# AI Prompts and Judgement Calls

This project was built end-to-end in a single session with **GitHub Copilot** (Raptor mini) as the IDE-integrated AI. The interview brief also mentioned GitHub Copilot–specific techniques (Agent Mode, `.prompt.md` files, `#file`/`#codebase`/`#selection` hooks); those aren't literally available inside Claude Code, but their underlying ideas — spec-first decomposition, repo-scoped conventions, reusable prompts, precise context, and cross-prompt continuity — are exactly the techniques used below.

The workflow was **spec → API + tests → FE → docs**, gated at each stage: `spec.md` was committed as the sole first commit before any code was written, mirroring the Definition of Done. Every judgement call is captured here.

---

## Prompt 1 — Pre-implementation gate: draft `spec.md`

**What I gave the model:** the case PDF as-is, plus explicit instructions that `spec.md` is a hard DoD gate that must be committed before implementation.

**What I asked for:** a complete spec covering domain model, `IHotelProvider` contract, endpoint shapes, validation rules, provider normalisation, and project structure — with alternatives called out inline.

**Judgement calls the model surfaced (and I confirmed):**

1. **Cancellation policy as `{ type, hoursBeforeCheckIn }`, not a bare enum.** Collapsing PremierStays' 48h `FreeCancellation` and BudgetNests' 24h `Flexible` into a single "refundable" bucket erases commercial semantics a downstream consumer might care about. Kept them distinct.
2. **Domestic accepts Passport too (permissive reading of §5.1).** The brief says "National ID accepted" for domestic — that could mean _only_ NationalId or _at least_ NationalId. Chose the latter and documented the alternative as a one-line flip. Flagged in `reflection.md`.
3. **Provider-failure semantics: graceful degradation.** One provider throwing shouldn't fail the whole search. Not specified in the brief. Chose partial results + log; alternative would be a `warnings[]` array or fail-fast. Asserted in tests.
4. **Reserve request re-sends destination and dates** instead of redeeming a signed "quote token" from search. Realistic for a real integration, architectural theatre for stub data. Called out in `reflection.md`.
5. **Reference format `HS-XXXXXXXX`** (8 upper alphanumerics) instead of a raw GUID. UX affordance — the ref is quotable on a phone call.

---

## Prompt 2 — Scaffold the API from the spec

**Context:** the spec.md from Prompt 1, plus the explicit constraint that stub data must be discoverable both under `dotnet run` and under `WebApplicationFactory<Program>` (integration tests set a different content root).

**What the model produced:** the full `HotelStay.Api` tree in one pass — domain records, both provider stubs, aggregator, document validator, city catalogue, reference-number factory, in-memory reservation store, all four endpoints, `Program.cs` wiring.

**Judgement calls made mid-generation:**

1. **Embedded resources for stub JSON, not `<Content>` with copy-to-output.** The instinct was to use `CopyToOutputDirectory="PreserveNewest"` and read from `AppContext.BaseDirectory`. That works for `dotnet run` but is fragile under `WebApplicationFactory` because the content root changes. `<EmbeddedResource>` with explicit `LogicalName` values (e.g. `premierstays.rooms.json`) is bulletproof — zero path handling in the provider code.
2. **Provider-specific DTOs + explicit case-insensitive `Enum.Parse`.** The alternative — one shared JSON options object with `JsonStringEnumConverter` — hides where each provider's peculiarity lives. Separating DTOs makes the normalisation the _only_ place a case difference or unknown value can matter, which is easy to test.
3. **`internal static Normalise` methods with `InternalsVisibleTo("HotelStay.Tests")`.** The normalisation logic is the core value delivered by each provider; exposing it via `internal` lets tests exercise it directly without spinning up the JSON parser. Not a public API.
4. **`Enum.Parse` with `ignoreCase: true`** everywhere, deliberately. Both provider payloads use different casings (`"Standard"` vs `"standard"`) and the API accepts case-insensitive query params. Consistent tolerance across the boundary.

---

## Prompt 3 — Write tests

**Prompt:** "Write xUnit unit + integration tests per spec §9. Focus on branch coverage of decision points, not line coverage. Include the full domestic × international × Passport × NationalId × present × empty truth table for `DocumentValidator`."

**What the model got wrong on first pass (and I corrected):**

- Initially generated tests that asserted on the count of London results (5) without spelling out _which_ rooms — brittle to stub-data edits. I asked it to assert on the specific ordered total-price sequence `[225, 330, 360, 540, 960]` instead, so any stub-data change makes the test failure point directly at the changed line.
- Initially proposed asserting `"HS-.*"` on the reference; tightened to the exact `^HS-[A-Z0-9]{8}$` regex — the format is a documented contract, not "starts with HS".

**Judgement call:** no FE tests. Deliberately time-boxed; the FE is thin glue over the API and the tests would take ~40% of the remaining budget for ~15% of the risk reduction. Documented as the first thing I'd add in `reflection.md`.

---

## Prompt 4 — Scaffold the React FE

**Prompt:** "Write a Vite React + TypeScript app that uses the four `/hotels/*` endpoints. State machine: idle → searching → results → reserving → confirmed. Match the API's domain types in `src/types.ts` so a schema change on the API produces a single obvious TS compile error."

**Judgement calls:**

1. **Vite proxy `/hotels/* → :5080`** instead of absolute URLs. Keeps fetch calls path-relative, means the FE is production-buildable without special build-time flags.
2. **`HotelApiError` class carrying `code` + `status`.** The FE currently renders `error` verbatim to the user, but the class shape means a future improvement (localised messages keyed by `code`) is a search-and-replace, not a refactor.
3. **Discriminated-union `Mode` type** for the App-level state machine, not a flat set of booleans. Each transition is explicit and TypeScript-exhaustive; unreachable states are compile errors.
4. **Client-side doc validation mirrors server rules.** The server is still the authority (spec §5.3) — the client-side check is UX only, disabling submit before the round-trip. If the client and server disagree, the server wins by rejecting.

---

## Prompt 5 — Documentation pass (README, this file, reflection)

Delegated to the model with the instruction: "Focus on _why_, not _what_. The code is the _what_. Reviewers can read `git log`; they can't read my mind."

**Judgement call:** no separate architecture diagram. The `spec.md` project structure section + `README.md` layout table cover the same ground textually. An image adds nothing at this scale and rots faster than prose.

---

## What Claude Code got wrong (and how I noticed)

- **Suggested `AllowAnyOrigin` CORS without gating on `IsDevelopment()`.** Caught in review — un-gated `AllowAnyOrigin` in a real deployment is a security regression. Wrapped it in an `env.IsDevelopment()` check before committing.
- **Suggested `Guid.NewGuid()` for the reservation reference.** Length + character set didn't match my spec. Reverted to the `HS-[A-Z0-9]{8}` format.
- **First-cut `PremierStaysProvider` used `JsonStringEnumConverter` on the DTO and enum-typed the `Cancellation` field directly.** This would have failed silently on unknown values because the converter tries integer-then-enum-name — no explicit error for a typo like `"FreeCanc"`. Switched to `string` on the DTO and explicit `switch` in `Normalise`, which throws with a clear message on unknown values.
- **First draft of `winget` installed Node with `--silent`, which suppressed the UAC prompt and failed with MSI exit code 1603.** Diagnosed by reading the winget log; user re-ran it in an elevated shell. Documented so a future me doesn't repeat.

## Techniques used, mapped to Copilot terminology

| Copilot feature                                                      | Equivalent used here                                                                                                                                                                            |
| -------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Agent Mode** — scaffold multi-file solutions from one prompt       | Full API + tests + FE scaffold done as a coherent batch, not file-by-file                                                                                                                       |
| **Custom Instructions** — repo-level conventions                     | Enforced by treating`spec.md` as the source of truth; every prompt referenced it                                                                                                                |
| **`.prompt.md` files** — reusable parameterised prompts              | Not created as artifacts, but the "add new provider" prompt shape is documented in`README.md` § _Extending_                                                                                     |
| **`#file` / `#codebase` / `#selection` hooks** — scoped context      | Achieved by explicit file references and by prompting with only the affected file's contents when iterating                                                                                     |
| **Memory / Session continuity**                                      | Whole build was one session — spec → models → endpoints → tests → FE with no context loss                                                                                                       |
| **LLM Awareness** — decomposing, validating, catching hallucinations | The four "got wrong" items above are exactly the class of subtle bugs (CORS, ref format, silent JSON conversion, tool-elevation edge case) that model output must be manually validated against |
