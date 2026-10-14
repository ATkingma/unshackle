# TAAK-183 — MediatR: `AddMediatR` across project boundaries (investigate the Mediator 3 `Assemblies` option)

**Status**: done (2026-10-12)
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)
**Goal**: the `mediatr.add-mediatr` Manuals from the corpus migrate automatically, or deliberately stay Manual with a justification in the pattern doc.

## Context for the next agent

Variants, currently Manual in `AddMediatRPattern.cs`:

- ardalis v9.1.0 `src/Clean.Architecture.Web/Program.cs:109`: scans `Clean.Architecture.Core`, handler `ContributorDeletedHandler` is `internal` (CS0122 from Web). ardalis also rightly stays Manual because of Ardalis.SharedKernel.
- evo2 `Contracts/Fitnet.Contracts.Infrastructure/Mediation/MediationModule.cs:12`: scans `Contracts.Application`, handler `internal`; and `Common/Fitnet.Common.Infrastructure/Mediator/MediatorModule.cs:10`: `public` method with an `Assembly assembly` parameter (handlers in the caller).
- evo1 `Fitnet/Common/Events/EventBus/InMemory/InMemoryEventBusModule.cs:10`: `internal` method, also called by `Fitnet.IntegrationTests` (InternalsVisibleTo) with `Assembly.GetExecutingAssembly()` to register fake consumers.

## Analysis

Mediator 3.x can generate handlers from referenced assemblies via `AddMediator(o => o.Assemblies = [...])`; internal handlers remain unreachable unless there is InternalsVisibleTo or generation in the declaring project. First measure with a probe project what 3.0.2 can do (as in TAAK-132), then choose per variant.

## Result (2026-10-12)

Probe (host + library, Mediator 3.0.2, net10.0):

| Setup | Result |
| --- | --- |
| internal handler, no InternalsVisibleTo, `o.Assemblies = [typeof(Marker)]` | CS0122 in `Mediator.g.cs` |
| same without `Assemblies` | CS0122 (the generator scans referenced assemblies anyway) |
| `InternalsVisibleTo("Host")`, with or without `Assemblies` | builds, both handlers run |

`Assemblies` only limits the scan; it does not help against internal handlers. Decision per variant:

- Scanned assembly with an internal handler that has this project as a friend → now **Fixed** (`AddMediatRPattern.HiddenHandler` checks `GivesAccessTo`).
- ardalis (`ContributorDeletedHandler` internal, no friend; plus Ardalis.SharedKernel) → stays Manual; the message now also mentions InternalsVisibleTo as a way out.
- evo2 public method with an `Assembly` parameter → stays Manual (handlers at the caller; multiple `AddMediator` calls give one `IMediator`, TAAK-144).
- evo2 `MediationModule` (internal handlers, no friend) → stays Manual.
- evo1 internal method that the test project actually calls → stays Manual (TAAK-148).

Changed: `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`, `src/Unshackle.Tests/Core/AddMediatRPatternTests.cs`
(theory `..._ManualWhenHandlerNotPublicAndNoFriend`: friend `Host` → Fixed, `Other` → Manual), `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`, `-verify.md`.
No proof: a proof is a single file and cannot mimic a second assembly with InternalsVisibleTo; the unit test does that with two compilations.

## Steps

1. [x] Probe: Mediator 3.0.2 `Assemblies` with public and internal handlers, and with InternalsVisibleTo.
2. [x] Decision per variant in `docs/projectinfo/patterns/` (add-mediatr).
3. [x] Proofs + implementation for the variants that are possible.
4. [x] Proof: corpus run 2026-10-12 on 5f9b366 (`CORPUSRUN-mediatr.md`, section Repetition after TAAK-180..184): ardalis (1 add-mediatr + 11 SharedKernel), evo1 (1) and evo2 (2) keep exactly the Manuals decided above; tests equal (9/9, 45/45, 26/52), 0 regressions.

## See also

- `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
