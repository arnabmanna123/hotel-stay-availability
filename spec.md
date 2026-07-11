# Hotel Stay Availability — Specification
> This document is the design gate. It is committed **before** any implementation file. Every decision below is a conscious choice made against the brief; alternatives considered are called out inline so the reasoning survives beyond the interview demo.
---
## 1. Scope and non-goals
**In scope**
- Search across two stub providers, normalise their responses, return a unified list.- Reserve a specific room with document validation applied at reservation time.- Retrieve a reservation by its reference number.- Minimal React SPA covering all four required states: results, empty, error, confirmation.
**Non-goals**
- No real hotel APIs, no auth, no persistence layer, no payments.- No i18n, no accessibility beyond semantic HTML defaults, no rate limiting.- No production concerns (retries, circuit breakers, caching) — this is a design demonstration, not a hardened service.
---
## 2. Domain model
The unified representation the API returns to the frontend. Providers must be normalised into this shape before leaving the API boundary.
### 2.1 `Room` (search result)
| Field                  | Type                   | Notes                                                                          || ---------------------- | ---------------------- | ------------------------------------------------------------------------------ || `id`                 | string                 | Provider-scoped room identifier, e.g.`PS-LON-STD-001`. Opaque to the client. || `providerId`         | enum`ProviderId`     | `PremierStays` \| `BudgetNests`.                                           || `roomType`           | enum`RoomType`       | `Standard` \| `Deluxe` \| `Suite`.                                       || `pricePerNight`      | decimal                | ≥ 0.                                                                          || `totalPrice`         | decimal                | `pricePerNight × nights`. Computed server-side so the FE doesn't drift.     || `currency`           | string                 | ISO 4217. Pinned to`USD` across both stubs for this exercise (see §7).      || `cancellationPolicy` | `CancellationPolicy` | See §2.2.                                                                     || `amenities`          | string[]               | Empty array for BudgetNests (minimal detail).                                  || `starRating`         | int?                   | `null` for BudgetNests.                                                      |
### 2.2 `CancellationPolicy`
Modelled as a small object rather than a bare enum so the two providers' different windows survive normalisation without losing information.
```ts{  type: "FreeCancellation" | "Flexible" | "NonRefundable",  hoursBeforeCheckIn: number | null   // null iff NonRefundable}```
| Provider raw                     | Normalised`type`   | `hoursBeforeCheckIn` || -------------------------------- | -------------------- | ---------------------- || PremierStays`FreeCancellation` | `FreeCancellation` | `48`                 || PremierStays`NonRefundable`    | `NonRefundable`    | `null`               || BudgetNests`Flexible`          | `Flexible`         | `24`                 || BudgetNests`NonRefundable`     | `NonRefundable`    | `null`               |
**Decision:** kept `FreeCancellation` and `Flexible` as distinct types even though both mean "refundable with a deadline". Collapsing them would erase the provider's own commercial semantics; a downstream consumer (or the traveller) may care that PremierStays' policy is stricter with itself.
### 2.3 `Reservation`
Returned by `POST /hotels/reserve` and `GET /hotels/reservation/{reference}`.
| Field                                             | Type                      | Notes                                               || ------------------------------------------------- | ------------------------- | --------------------------------------------------- || `reference`                                     | string                    | `HS-XXXXXXXX` (8 upper alphanumeric). See §6.3.  || `providerId`                                    | enum`ProviderId`        | Which provider fulfils the booking.                 || `roomId`                                        | string                    | The reserved room's id.                             || `roomType`                                      | enum`RoomType`          |                                                     || `checkIn` / `checkOut`                        | ISO date (`YYYY-MM-DD`) |                                                     || `nights`                                        | int                       | Convenience —`(checkOut - checkIn).Days`.        || `pricePerNight` / `totalPrice` / `currency` |                           | Snapshotted at reservation time.                    || `cancellationPolicy`                            | `CancellationPolicy`    |                                                     || `guestName`                                     | string                    | As supplied.                                        || `documentType`                                  | enum`DocumentType`      | `Passport` \| `NationalId`.                     || `documentNumber`                                | string                    | As supplied, not validated for format — see §5.2. |
---
## 3. Provider abstraction
```csharppublic interface IHotelProvider{    ProviderId Id { get; }    Task<IReadOnlyList<Room>> SearchAsync(SearchQuery query, CancellationToken ct);}```
- The API layer takes `IEnumerable<IHotelProvider>` (DI-injected). Adding a third provider is one class + one line in `Program.cs` — **no change to the search endpoint, aggregation, or normalisation**.- Each provider is responsible for its own JSON parsing and normalisation. The unified `Room` shape leaves the provider — the endpoint never touches raw provider payloads.- Aggregation runs providers in parallel with `Task.WhenAll`. A single failing provider must not fail the whole search (log + return empty list from the offender). This behaviour is asserted in tests.
### 3.1 Stubs
Both stubs read their payloads from JSON files under `HotelStay.Api/Providers/<Name>/stub-data/` — one file per (city × room-type) combination is overkill; instead each provider ships a single `rooms.json` and filters in memory by destination and (optionally) room type.
**Determinism:** the JSON files are checked in. Given the same query, both stubs return the same rooms in the same order. Tests can assert against fixed prices and ids.
**Representative scenarios covered by the stub data:**
- Every declared city has at least one Standard room from each provider.- At least one destination has a room from only *one* provider (proves aggregation works with an empty side).- BudgetNests has at least one `"available": false` record per city — filtered out before returning (proves the filter works end-to-end).- At least one Deluxe and one Suite exist across the catalogue.
---
## 4. API contracts
Base URL: `http://localhost:5080` (dev default).
### 4.1 `GET /hotels/search`
Query parameters:
| Name            | Required | Format                                  | Rule                                                           || --------------- | -------- | --------------------------------------- | -------------------------------------------------------------- || `destination` | ✓       | string                                  | Must match one of the declared cities (§7), case-insensitive. || `checkIn`     | ✓       | `YYYY-MM-DD`                          |                                                                || `checkOut`    | ✓       | `YYYY-MM-DD`                          | Must be**strictly after** `checkIn`.                   || `roomType`    | –       | `Standard` \| `Deluxe` \| `Suite` | Case-insensitive; if absent, all types returned.               |
**Responses**
- `200 OK`
  ```json  {    "nights": 3,    "currency": "USD",    "results": [ /* Room[] */ ]  }  ```
  Sort order: `(totalPrice ASC, providerId ASC)` — deterministic for tests and for the FE's default view.- `400 Bad Request` — missing required param, malformed date, `checkOut <= checkIn`, or unknown destination.
  ```json  { "error": "checkOut must be after checkIn", "code": "invalid_dates" }  ```
### 4.2 `POST /hotels/reserve`
Request body:
```json{  "roomId": "PS-LON-STD-001",  "providerId": "PremierStays",  "destination": "London",  "checkIn": "2026-08-01",  "checkOut": "2026-08-04",  "guestName": "Ada Lovelace",  "documentType": "NationalId",  "documentNumber": "AL-1815-XYZ"}```
**Responses**
- `200 OK` — full `Reservation` object (§2.3). The reference is guaranteed persisted (in-memory) before the response returns.- `400 Bad Request` — missing fields, malformed dates, unknown provider/room, or `checkOut <= checkIn`.- `422 Unprocessable Entity` — document mismatch. See §5.  ```json  { "error": "Passport required for international destinations", "code": "document_required_passport" }  ```
**Decision:** the reserve request re-sends destination and dates rather than a bare reservation-of-quote pattern. The alternative — issuing a signed "quote token" from search and redeeming it here — is realistic for a real integration but architectural theatre for a demo with stub data. The trade-off is called out in `reflection.md`.
### 4.3 `GET /hotels/reservation/{reference}`
- `200 OK` — full `Reservation` object.- `404 Not Found` — unknown reference.  ```json  { "error": "Reservation not found", "code": "reservation_not_found" }  ```
### 4.4 Standard error envelope
Every non-2xx response uses:
```json{ "error": "<human-readable message>", "code": "<snake_case_stable_code>" }```
Codes are the machine-readable contract; messages are for humans. FE decides which one to show.
---
## 5. Document validation
### 5.1 Rules
| Destination class | `Passport`         | `NationalId` || ----------------- | -------------------- | -------------- || Domestic          | ✓ accepted          | ✓ accepted    || International     | ✓**required** | ✗ rejected    |
**Reading of the brief:** "National ID accepted" for domestic is interpreted permissively — a passport is a superset of identity strength and is accepted domestically too. This is documented so the interviewer can push back if their reading is stricter; flipping to "domestic must be NationalId only" is a one-line change.
### 5.2 What is *not* validated
Document *format* (checksums, country prefixes, length) is out of scope. A passport number is any non-empty string. This is deliberate — the point of the exercise is destination×type coupling, not identity-document parsing. Called out in `reflection.md`.
### 5.3 Client-side vs server-side
- Client-side: the reserve form disables submit until a document type valid for the selected destination class is chosen, and shows the same 422 message inline if the server rejects.- Server-side: `DocumentValidationService` is the authority. The client-side check is UX, not enforcement.
---
## 6. Cross-cutting design
### 6.1 Dependency injection
Registered in `Program.cs`:
```csharpbuilder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();builder.Services.AddSingleton<IReservationStore, InMemoryReservationStore>();builder.Services.AddSingleton<IDocumentValidator, DocumentValidator>();builder.Services.AddSingleton<ICityCatalogue, CityCatalogue>();```
Singleton is correct here — every service is stateless except `InMemoryReservationStore`, which needs process-scoped state anyway.
### 6.2 In-memory reservation store
`ConcurrentDictionary<string, Reservation>` behind `IReservationStore`. Data is lost on process restart. Documented in the README as a known limitation for the demo.
### 6.3 Reference number generation
`HS-` + 8 upper-case alphanumerics from a cryptographically strong RNG. Collision probability is negligible at demo scale and doesn't warrant a retry loop. Chosen over `Guid.NewGuid().ToString()` because a shorter, human-quotable reference is the primary UX affordance on the confirmation screen.
### 6.4 Logging
Serilog to console at `Information` in dev. One log line per provider call (provider id, duration, result count). No PII in logs — no guest names or document numbers.
### 6.5 CORS
Dev-only: `AllowAnyOrigin` behind an `IHostEnvironment.IsDevelopment()` guard. The FE runs on `http://localhost:5173` (Vite default) and the API on `5080`.
---
## 7. Reference data
### 7.1 City catalogue
Committed as `HotelStay.Api/Data/cities.json`. Exposed via `GET /hotels/cities` so the FE can populate its destination dropdown from the API (single source of truth).
| City       | Class         || ---------- | ------------- || London     | Domestic      || Manchester | Domestic      || Paris      | International || New York   | International || Tokyo      | International |
Two domestic + three international — the brief's minimum, kept small on purpose to make stub data manageable.
### 7.2 Currency
Both providers report prices in USD for this exercise. Real providers would each return their own currency and the aggregation layer would need FX; that's out of scope and noted in `reflection.md`.
---
## 8. Project structure
```hotel-stay/├── README.md                # setup, run, assumptions, known limitations├── spec.md                  # this file├── prompts.md               # AI prompts + judgement calls├── reflection.md            # what I would improve with more time├── HotelStay.Api/│   ├── HotelStay.Api.csproj│   ├── Program.cs           # DI, endpoint routing│   ├── Endpoints/│   │   ├── SearchEndpoint.cs│   │   ├── ReserveEndpoint.cs│   │   ├── ReservationEndpoint.cs│   │   └── CitiesEndpoint.cs│   ├── Domain/│   │   ├── Room.cs│   │   ├── Reservation.cs│   │   ├── CancellationPolicy.cs│   │   ├── Enums.cs         # ProviderId, RoomType, DocumentType, CityClass, PolicyType│   │   └── SearchQuery.cs│   ├── Providers/│   │   ├── IHotelProvider.cs│   │   ├── PremierStays/│   │   │   ├── PremierStaysProvider.cs│   │   │   ├── PremierStaysDtos.cs│   │   │   └── stub-data/rooms.json│   │   └── BudgetNests/│   │       ├── BudgetNestsProvider.cs│   │       ├── BudgetNestsDtos.cs│   │       └── stub-data/rooms.json│   ├── Services/│   │   ├── HotelAggregator.cs        # fan-out, filter, sort│   │   ├── DocumentValidator.cs│   │   ├── CityCatalogue.cs│   │   ├── ReferenceNumberFactory.cs│   │   └── InMemoryReservationStore.cs│   └── Data/│       └── cities.json├── HotelStay.Tests/│   ├── HotelStay.Tests.csproj│   ├── Providers/│   │   ├── PremierStaysProviderTests.cs│   │   └── BudgetNestsProviderTests.cs│   ├── Services/│   │   ├── HotelAggregatorTests.cs│   │   ├── DocumentValidatorTests.cs│   │   └── ReferenceNumberFactoryTests.cs│   └── Endpoints/│       ├── SearchEndpointTests.cs│       ├── ReserveEndpointTests.cs│       └── ReservationEndpointTests.cs└── hotelstay-ui/            # Vite + React + TypeScript    ├── package.json    ├── vite.config.ts    ├── index.html    └── src/        ├── main.tsx        ├── App.tsx        ├── api/hotels.ts            # typed fetch wrappers        ├── types.ts                 # mirror of API domain types        ├── components/        │   ├── SearchForm.tsx        │   ├── ResultsList.tsx        │   ├── RoomCard.tsx        │   ├── ReserveForm.tsx        │   └── Confirmation.tsx        └── styles.css```
### 8.1 Solution wiring
- No `.sln` file required for `dotnet run` / `dotnet test`, but one is added for IDE ergonomics (`HotelStay.sln`) covering both `HotelStay.Api` and `HotelStay.Tests`.- `HotelStay.Tests` references `HotelStay.Api` as a `ProjectReference` and uses `Microsoft.AspNetCore.Mvc.Testing` for the endpoint-level integration tests.
### 8.2 Extension: adding a third provider
1. `Providers/ThirdParty/ThirdPartyProvider.cs` implementing `IHotelProvider`.2. `Providers/ThirdParty/stub-data/rooms.json`.3. One line in `Program.cs`: `builder.Services.AddSingleton<IHotelProvider, ThirdPartyProvider>();`
The aggregator, endpoints, tests for other providers, and the FE remain untouched. This will be demonstrated live during the interview if requested.
---
## 9. Testing strategy
**Unit** — one test class per business-logic file. Focus on branch coverage of decision points, not line coverage.
- `PremierStaysProviderTests` — PascalCase parsing, policy mapping (`FreeCancellation` → 48h; `NonRefundable` → null), amenity/star preservation, roomType filter, unknown destination returns empty.- `BudgetNestsProviderTests` — snake_case parsing, `"available": false` filtering, policy mapping (`Flexible` → 24h), no amenities/star present.- `HotelAggregatorTests` — merges both providers, honours roomType filter, one provider throwing does not fail the whole search (log + continue), sort order stable.- `DocumentValidatorTests` — full truth table: {domestic, international} × {Passport, NationalId} × {present, empty} — 8 cases, each with the expected error `code`.- `ReferenceNumberFactoryTests` — format matches `HS-[A-Z0-9]{8}`, 10 000 generations produce no duplicates.
**Integration** (WebApplicationFactory) — one class per endpoint.
- `SearchEndpointTests` — 200 happy path with fixed stub data (asserts specific prices and ordering); 400 for each of: missing destination, missing checkIn, missing checkOut, malformed date, checkOut ≤ checkIn, unknown destination.- `ReserveEndpointTests` — 200 happy path; 422 for {international destination + NationalId}; 400 for missing fields; 400 for unknown roomId.- `ReservationEndpointTests` — round-trip (reserve then GET); 404 for unknown reference.
**Not tested (deliberately):**
- The FE. Documented in `reflection.md` — a Playwright smoke test of the golden path is the first thing I'd add with more time.- JSON serialiser configuration in isolation — exercised end-to-end by the integration tests.
---
## 10. Open questions
Called out here so the interviewer can steer if the reading differs from mine.
1. **Domestic-Passport permissive interpretation** (§5.1) — flip if stricter reading is wanted.2. **Currency uniformity** (§7.2) — real providers would each have their own; single-currency simplification is deliberate.3. **Quote-token reservation pattern** (§4.2) — not implemented; called out in `reflection.md`.4. **Provider failure semantics** (§3) — one provider failing returns partial results, doesn't fail the request. Alternative (fail-fast, or return a `warnings` array) is a business choice not specified in the brief.