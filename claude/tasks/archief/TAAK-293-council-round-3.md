# TAAK-293 — Council round 3: typed kept-lookup reason, stricter registration rules, fullcart text

**Status**: ✅ done
**Created**: 2026-10-19
**Goal**: implement the round-3 council decisions in `claude/docs/council.md` with proof.

## Context for the next agent

- Builds on TAAK-292 (`claude/tasks/archief/TAAK-292-council-follow-up.md`): `MapperRegistration.cs`, `MapperCleanup.cs`, `FixedResult.Info`.
- Q1: make sure `Info` also appears in the JSON/markdown report output.
- Q2: replace the `Stays on AutoMapper:` string prefix as mechanism by a typed reason code on the finding; the renderer prints the prefix; a test pins the rendered text.
- Q3: target-typed `new()` recognised as container via converted type; `AddAutoMapper` inside a lambda only unconditional when the lambda goes directly to an unconditional registration/builder call, else keep with reason `ConditionalOrDeferredRegistration` + file:line.
- fullcart: the MediatR reason names Scrutor's `Decorate(typeof(INotificationHandler<>), …)` instead of Autofac.

## Steps

- [x] 1. KingmaDoc plan updates (unused-imapper REQ-13..15; generic-decorator REQ-8)
- [x] 2. Implementation + unit tests + proofs
- [x] 3. Corpus `after` on pi22317, assistiva, culinary, msb, encryptednotes, estore (+ `scan` on fullcart for the text): 0 regressions, numbers here and in CORPUSRUN-blind.md
- [x] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## Choices

- `KeptReason` (enum in `Core/Models`) on `ManualResult.Kept`; only the kept `GetService<IMapper>()` lookup uses it. The other "Stays on AutoMapper:" findings (kept maps, calls, AddAutoMapper, configuration tests) keep their string prefix: the council decision was about the kept lookup; moving them is a separate change. `AutoMapperFix.AllowsPartial` accepts either.
- The prefix lives in the renderer: `ReportRow.ReasonOf` (CSV, JSON, markdown) and the console blocker list use it.
- A local function around `AddAutoMapper` counts as deferred (lookup kept), like a lambda that is not passed directly to a registration call (conservative choice, not in the council text).
- "Registration or builder call": a call whose receiver, or the type it extends, is (or implements) `IServiceCollection`, `IHostBuilder`, `IWebHostBuilder`, `IHostApplicationBuilder`, `WebApplicationBuilder` or `IFunctionsHostBuilder`, matched by name.
- Scrutor is recognised syntactically (`.Decorate(...)` with `typeof(D<>)` as an argument), wherever it is among the decorator's uses (fullcart's first use is a `HashSet<Type>` exclusion list).

## Result

- Corpus: 0 regressions, counts unchanged (`CORPUSRUN-blind.md`, section TAAK-293); fullcart's reason names `Decorate(...) at DependencyInjection.cs:38`.
- Open: figure 4 of `automapper-unused-imapper-modellen.md` updated in D2 only (no `d2` here).

## See also

- `claude/docs/council.md`, `claude/tasks/archief/TAAK-292-council-follow-up.md`, `claude/tasks/CORPUSRUN-blind.md`
