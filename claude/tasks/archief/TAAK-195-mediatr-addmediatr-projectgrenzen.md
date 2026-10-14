# TAAK-195 — MediatR: `AddMediatR` across project boundaries 100% automatic (internal handlers, `Assembly` parameter, test project)

**Status**: partly — internal handlers ✅ automatic; `Assembly` parameter (evo2) and test-project caller (evo1) ❌ failed (attempt 1, no exact translation)
**Created**: 2026-10-12 (100% round, `claude/tasks/mediatr-queue.md` row 25)
**Goal**: migrate the `mediatr.add-mediatr` Manuals of ardalis, evo1 and evo2 (TAAK-144, -148, -183) automatically and exactly, or measure per variant why that is not possible.

## Context for the next agent

- Variants (sources in `<probe-dir>/probe-195/` during this task, cleaned up afterwards; to clone again: `github.com/ardalis/CleanArchitecture` tag v9.1.0, `github.com/evolutionary-architecture/evolutionary-architecture-by-example` with `core.longpaths`):
    - ardalis `src/Clean.Architecture.Web/Program.cs:109` scans Core (internal `ContributorDeletedHandler`) and UseCases.
    - evo2 `Contracts.Infrastructure/Mediation/MediationModule.cs:12` scans `Contracts.Application` (internal handlers); `Common.Infrastructure/Mediator/MediatorModule.cs:8` is a public `AddMediator(services, Assembly)` that Offers, Passes and `Common.IntegrationTests.WithFakeConsumers(executingAssembly)` call.
    - evo1 `Fitnet/Common/Events/EventBus/InMemory/InMemoryEventBusModule.cs:7` internal `AddInMemoryEventBus(services, assembly)`; `Fitnet.IntegrationTests/.../ConfigurationExtensions.cs:43` calls it in `ConfigureTestServices` with the test assembly to register `TestEventConsumer : IIntegrationEventHandler<FakeEvent>` (both in the test project).

## Probe (Mediator 3.0.2, net10.0; `<probe-dir>/probe-195/p1`)

| Setup | Result |
| --- | --- |
| Host (generator, `AddMediator`) + Tst (without generator) that puts `INotificationHandler<FakeEvent>` into DI itself and does `Publish(new FakeEvent())` | no error, handler **not** called: the generated `Publish` switches on types known at compile time, unknown → silently ignored |
| Tst also with a generator and its own `AddMediator` | CS0436 (`Mediator.Mediator`, `MediatorOptions`, `AssemblyReference` twice), CS0121 (two `AddMediator` extensions), CS0122 (internal host handler) |
| Generated `AddMediator` | `IMediator`/`ISender`/`IPublisher` via `TryAdd`: the first call wins; handlers via `Add` (a second generator would register host handlers twice) |
| Internal handler in a referenced project + `[assembly: InternalsVisibleTo("Host")]` in the source | builds (TAAK-183) |

## Choices

1. **Internal handlers (ardalis, evo2 MediationModule) → automatic.** New pattern `mediatr.internal-handler-access` (`InternalHandlerAccessPattern`): the first file (by path) of the scanned project with an internal handler gets `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("<host>")]`. In the host project that is no behaviour difference (MediatR reached them via reflection). `AddMediatRPattern.RewriteAsync` is then no longer Manual (`CanGrantAccess`: a C# project of the solution, no strong name). Chosen over a csproj `<InternalsVisibleTo>` (requires a new `IProjectFileStore` contract in DataAccess) and over making handlers `public` (CS0050/CS0051 if the request type is internal).
2. **Contract `ISolutionFinder`** (`Core/Abstractions`, one line in `Fixer.ApplyPatternAsync`): `Find` without a solution does not know which file has a finding; without this every file with an internal handler got an empty Fixed row. Separate from `ISolutionPattern` so the four AutoMapper patterns (other agents) do not have to change.
3. **evo1 → failed.** The fake consumer and its event are in the test project. The host generator does not know `FakeEvent`, so `Publish` does nothing (measured); a generator in the test project does not build (CS0436/CS0121/CS0122) and through `TryAdd` would let the host `IMediator` win anyway. Exact is only possible if the test code replaces the host's DI registration and does not register the host handlers twice: no safe automatic translation.
4. **evo2 public `AddMediator(services, Assembly)` → failed.** Three modules each register their assembly in one container; Mediator has one generated mediator per host. The only exact form is one `AddMediator` in the composition root (`Fitnet`) with `InternalsVisibleTo` from every module, and the module calls removed. But the integration tests (`WithFakeConsumers(executingAssembly)`) run into the same problem as evo1.
5. **Best partial translation evo1/evo2**: stays Manual (all-or-nothing); the Manual text shows the way (one `AddMediator` in the host project, test handlers in the host or a host of their own per test project). With choice 1 the `MediationModule` Manual disappears for evo2; the `MediatorModule` Manual stays.

## Changed

- `src/Unshackle.Core/Abstractions/ISolutionFinder.cs` (new), `src/Unshackle.Core/Fixer.cs` (`FindAsync` if the pattern has it)
- `src/Unshackle.Core/Fixes/MediatR/Patterns/InternalHandlerAccessPattern.cs` (new), `AddMediatRPattern.cs` (`ScannedAssemblies`, `IsHiddenHandler`, `CanGrantAccess`), `MediatRFix.cs` (pattern at the end)
- `src/Unshackle.Tests/Helpers/TestProjects.cs` (multi-project proofs: `// @project Name -> References`), `CombinedProofTests.cs`, `Core/InternalHandlerAccessPatternTests.cs`
- Proof `src/Unshackle.Tests/Proofs/MediatR/_Combined/add-mediatr-internal-handlers-other-project.{before,after}.cs` (ardalis shape: Core/UseCases/Web)
- Docs: `docs/projectinfo/patterns/mediatr-internal-handler-access-{plan,modellen,verify}.md`, `mediatr-add-mediatr-{plan,verify}.md`, README row, `proofs.md`

## Steps

- [x] Probe: unknown notification, second generator, `TryAdd`
- [x] Pattern + contract + proof + unit tests
- [x] Docs (plan/models/verify, README, INDEX, proofs-doc)
- [x] ardalis clone: `fix --fix mediatr --force` → 31 Fixed, 0 Manual (with the vendored SharedKernel of a parallel task); Core/UseCases/Infrastructure/Web build. New gap: `tests/Clean.Architecture.UnitTests/NoOpMediator.cs` (own `IMediator` implementation, `Task` signatures) breaks the build (CS0535/CS0738) — TAAK-197.
- [!] evo1 and evo2 `MediatorModule`: no exact automatic translation (choices 3–4)

## See also

- `claude/tasks/archief/TAAK-183-mediatr-addmediatr-over-assemblies.md`
- `claude/tasks/archief/TAAK-144-mediatr-evo2-naamconflict-generator.md`, `claude/tasks/archief/TAAK-148-mediatr-friend-echte-aanroep.md`
- `docs/projectinfo/patterns/mediatr-internal-handler-access-plan.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
