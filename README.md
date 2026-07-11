# Hotel Stay Availability

A hotel availability + reservation feature for the SkyRoute platform. Aggregates two stub providers (PremierStays, BudgetNests), normalises their differing payload shapes into a unified list, and applies destination-aware document validation at reservation time.

Design intent, alternatives considered, and the reasoning behind every non-obvious choice live in [`spec.md`](spec.md) — read that first if you want to understand _why_ the code is shaped the way it is.

---

## Quick start

Two processes: the API on `:5080`, the React dev server on `:5173`. From a clean clone:

```bash
# 1. terminal A — API + tests
cd HotelStay.Api
dotnet run
# API is live at http://localhost:5080

# 2. terminal B — frontend
cd hotelstay-ui
npm install
npm run dev
# Open http://localhost:5173
```

Then in the browser: pick a destination, dates, click **Search**, click **Reserve** on a room, fill the form, confirm. The reference number persists in-process — a `GET /hotels/reservation/{reference}` request retrieves it until the API restarts.

## Prerequisites

- **.NET SDK 8.0 or later.** The project files target `net8.0`. If you need to update the target, change the `<TargetFramework>` in both `.csproj` files and delete `bin/` / `obj/` before rebuilding.
- **Node.js 20 LTS or later.** `hotelstay-ui` was scaffolded with Vite 8.
- No database, no message broker, no external hotel APIs — everything runs offline.

## Running the tests

```bash
dotnet test
```

43 tests across unit + integration surfaces. All must pass before merging. The integration suite spins the full API up via `WebApplicationFactory<Program>` and drives real HTTP against it — no mocking of the endpoint layer.

## API surface

Base URL `http://localhost:5080` in dev.

| Method | Path                                                   | Purpose                                                         |
| ------ | ------------------------------------------------------ | --------------------------------------------------------------- |
| `GET`  | `/hotels/search?destination&checkIn&checkOut&roomType` | Aggregate rooms from both providers. See[spec §4.1](spec.md).   |
| `POST` | `/hotels/reserve`                                      | Validate document, reserve a specific room, return a reference. |
| `GET`  | `/hotels/reservation/{reference}`                      | Fetch a prior reservation (in-memory store, process-scoped).    |
| `GET`  | `/hotels/cities`                                       | The five-city catalogue (2 domestic + 3 international).         |
| `GET`  | `/health`                                              | Liveness probe.                                                 |

All non-2xx responses use `{ "error": "human message", "code": "snake_case_code" }` — the code is the machine contract, the message is for humans.

## Repository layout

```
hotel-stay/
├── spec.md                  # design gate — committed before any implementation
├── HotelStay.Api/           # .NET minimal API
│   ├── Domain/              # Room, Reservation, CancellationPolicy, enums, ApiError
│   ├── Providers/           # IHotelProvider + PremierStays + BudgetNests (each with own DTOs + stub JSON)
│   ├── Services/            # Aggregator, DocumentValidator, CityCatalogue, ReferenceNumberFactory, ReservationStore
│   ├── Endpoints/           # Search, Reserve, Reservation, Cities — each in its own extension method
│   └── Data/cities.json     # embedded resource
├── HotelStay.Tests/         # xUnit — unit + WebApplicationFactory integration tests
├── hotelstay-ui/            # Vite + React + TypeScript
│   └── src/
│       ├── api/hotels.ts    # typed fetch wrappers
│       ├── types.ts         # mirror of API domain types
│       ├── components/      # SearchForm, ResultsList, RoomCard, ReserveForm, Confirmation
│       └── App.tsx          # state machine: idle → searching → results → reserving → confirmed
├── prompts.md               # AI prompts + judgement calls
└── reflection.md            # what I'd improve with more time
```

## Assumptions and known limitations

Called out in more depth in [`reflection.md`](reflection.md), but the load-bearing ones:

- **Reservation persistence.** Reservations are persisted to `reservations.json` in the API directory, so they survive process restarts on the same machine. Real deployments still need a proper database behind `IReservationStore`.
- **Single currency (USD).** Both stub providers report in USD. A real integration would need FX at aggregation time.
- **Domestic accepts Passport too.** The permissive reading of the brief (spec §5.1); the strict alternative is a one-line change in `DocumentValidator`.
- **No FE tests.** Deliberately out of scope for the time budget. A Playwright smoke test of the golden path is the first thing I'd add.
- **CORS is `AllowAnyOrigin` in dev.** Only enabled when `IHostEnvironment.IsDevelopment()` is true.

## Extending — adding a third provider

The core flow is provider-agnostic. To add `ThirdPartyStays`:

1. `HotelStay.Api/Providers/ThirdPartyStays/ThirdPartyStaysProvider.cs` implementing `IHotelProvider`.
2. `HotelStay.Api/Providers/ThirdPartyStays/stub-data/rooms.json`, registered as an embedded resource in the `.csproj`.
3. One line in `Program.cs`:
   ```csharp
   builder.Services.AddSingleton<IHotelProvider, ThirdPartyStaysProvider>();
   ```

No changes to `SearchEndpoint`, `HotelAggregator`, or the frontend. This is the extension point I'll demonstrate live if asked.
