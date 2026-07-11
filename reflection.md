# Reflection — what I would improve with more time

Ranked roughly by ROI (impact per hour of effort). Everything above the line is what I would ship in a real second pass; everything below is what I'd write down in an ADR and defer.

---

## Would ship in a second iteration

### 1. Playwright smoke test of the golden path (highest ROI)

**Why:** the interview brief scores "Frontend reflects all defined states — results, empty, error, and confirmation" as part of Definition of Done. Right now the _only_ verification of those states is that I clicked through them manually during development. A single ~50-line Playwright spec that walks search → reserve → confirmation and asserts on the reference number's format would be the difference between "works on my machine" and "provably works from a clean clone."

**Why I didn't:** the time budget bought either FE tests _or_ the full state machine + styling. Chose the state machine — a broken UX is a demo-time show-stopper, a missing Playwright spec is not.

### 2. Reservation persistence

**Why:** the reference number is the primary UX affordance on the confirmation screen. Losing it when the API restarts means a demo failure is one `Ctrl+C` away. A tiny Sqlite file via EF Core (or even a JSON file write behind `IReservationStore`) removes that whole failure mode with ~20 lines of code.

**Why I didn't:** the brief says "no persistence," and I read that literally. In a real project I'd challenge that reading — the requirement is more likely "no _server-side database_", not "state must vaporise on restart."

### 3. Structured logging (Serilog + request-id middleware)

**Why:** the endpoints already log at `Information` for provider timings and outcomes, but plain `ILogger` output is noisy in a terminal and grep-hostile in a real deployment. Adding Serilog with a JSON console sink and a request-id middleware (`Activity.Current?.TraceId`) would make the log line for a failing provider actually diagnosable when a reviewer sees it. Cheap: two NuGet packages, one middleware, one `.WriteTo.Console(new JsonFormatter())`.

**Why I didn't:** deferred to keep the dependency graph minimal. Anything I add makes `dotnet run` slower and adds a package that could have a CVE at demo time.

### 4. FE error surface differentiates 4xx from 5xx from network failures

Right now `HotelApiError.message` gets rendered verbatim in a red banner. A user seeing "Passport required for international destinations" and a user seeing "Failed to fetch" are having very different experiences — the first is user-recoverable, the second is a system problem. `code` is already on the error class; the banner just needs to switch on it.

### 5. Quote-token pattern for the reserve request

Right now `/hotels/reserve` re-sends `destination`, `checkIn`, `checkOut`, `roomId`, `providerId` — five fields the FE already learned from `/hotels/search`. A signed quote token issued by search and redeemed by reserve would: (a) prevent tampering (price fixing), (b) let providers guarantee availability and rate for a window, (c) collapse the reserve payload to `{ quoteToken, guestName, documentType, documentNumber }`.

**Why I didn't:** it's architectural theatre against stub data that always agrees with itself. Documented so the reviewer sees I know the pattern exists.

### 6. `IReservationStore` behind a stricter interface

Currently: `void Save(Reservation)`, `Reservation? Find(string)`. That's fine for reads but silently allows double-booking of the same room + dates. A `TrySave(Reservation) : bool` (with room-date uniqueness check) plus a `Delete(string reference)` for cancellations would round out the shape. Tests would need to grow to cover the uniqueness constraint.

---

## Would document in an ADR and defer

These are correct calls for a demo but wrong calls for production.

### 7. Multi-currency support

Both providers return USD. Real providers would each report their own currency; the aggregator would need FX rates, a display-currency parameter, and a policy for whether `totalPrice` is in the room's currency or the traveller's. Non-trivial (FX source, staleness, rounding), and irrelevant to the graded surface.

### 8. Retry + circuit breaker on provider calls

The aggregator already tolerates a failing provider — but it treats "failed once" the same as "consistently down." Polly with a per-provider circuit breaker is standard-issue for real integrations. Not modelled because stub providers cannot fail.

### 9. Rate limiting

`.AddRateLimiter` on the endpoint pipeline. Trivial to add, entirely unhelpful for a demo — but I'd want it in front of any real integration to protect provider quotas.

### 10. Authentication + per-traveller reservations

Right now anyone can GET any reference number. In production, a reservation is bound to a traveller (or to a session); anonymous lookup would be a data-leak issue. Out of scope for the brief.

### 11. Accessibility pass

Semantic HTML is there (`<button>`, `<form>`, `<fieldset>`, `<dl>`), but I haven't checked keyboard traps, screen-reader flow, or colour-contrast on the light palette. Real product work would need an a11y audit — probably an hour with axe DevTools would find and fix most of it.

### 12. Component tests

If a Playwright smoke test is #1, then component-level tests (Vitest + Testing Library on `SearchForm`, `ReserveForm`) would be #6 or #7. The state-machine logic in `App.tsx` is the interesting bit; each component is thin.

### 13. OpenAPI spec

`Microsoft.AspNetCore.OpenApi` was in the .NET 10 webapi template — I removed it because the default package version (2.0.0) had a known CVE and the spec is not deep enough to warrant Swagger. In a real integration, `Scalar` or `Swashbuckle` on a pinned safe version would be the right move.

---

## Lessons

- **Committing `spec.md` first was the highest-leverage decision.** It forced me to name every judgement call before writing code — the alternative is discovering them during implementation, which is where drift happens.
- **Provider-specific DTOs paid off almost immediately.** The temptation was one shared `Room` DTO and let JSON options figure it out. Separating them made the normalisation the _only_ place casing/enum quirks live, which made the tests obvious.
- **The AI was faster than me at code generation but wrong on judgement calls three times.** The value wasn't "AI writes the code" — it was "AI produces a plausible first draft that I can review against the spec." The spec was the load-bearing artifact; the AI was the muscle. This is the correct relationship even without deadline pressure.
- **`AllowAnyOrigin` under `IsDevelopment()` guard** — the model generated the un-gated version first. If I hadn't been reading every diff, that would have landed. Every AI-produced security-adjacent line has to be manually inspected; the "looks reasonable" bar the model clears is not the "safe in production" bar.
