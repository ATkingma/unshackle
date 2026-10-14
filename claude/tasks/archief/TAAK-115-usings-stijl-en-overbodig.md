# TAAK-115 — Added usings outside the namespace or redundant

**Status**: done (2026-10-11)
**Created**: 2026-10-11 (corpus run MediatR evo1, `claude/tasks/CORPUSRUN-mediatr.md`)

## Context

evo1 (evolutionary-architecture ch. 1) puts usings inside a file-scoped namespace and builds with TreatWarningsAsErrors. After the
MediatR fix: IDE0065 (`using Mediator;` at the top, outside the namespace) and IDE0005 (redundant) in e.g.
`PassExpiredEventHandler.cs` (handler via its own `IIntegrationEventHandler<T>`, does not name MediatR), `RegisterEndpoint.cs` and
`InMemoryEventBusModule.cs` (`AddMediator` lives in `Microsoft.Extensions.DependencyInjection`).

## Steps

- [x] `Fixer`: `AddedNamespaces` only for a file that names the old library by name (original semantics,
  `UsingDirectives.NamesRemovedNamespaceAsync`; inactive `#if` code counts). A file that only uses MediatR via its own base types
  or had an unused old using gets no new one.
- [x] `UsingDirectives.UpdateAsync`: no top-level usings but usings in the namespace → new usings go there, with the same
  indentation. Not if a name inside the namespace shadows the new one (own `*.Mediator`, see TAAK-131): then at the top.
- [x] Tests: `FixerTests.FixAsync_FileNamesNothingOfRemovedNamespace_AddsNoNamespace`, `…_UsingsInsideNamespace_AddsNewOneThere`,
  `…_UsingsInsideNamespaceThatShadowsAddedOne_AddsNewOneOnTop`. AddMediatR proofs without redundant `using Mediator;`.
- [x] Corpus: fresh copy of evo1 (`scratchpad/v110/work/evo1`): no more IDE0005/IDE0065. Build still red on IDE0060
  (`assembly` unused after `AddMediatR(... RegisterServicesFromAssembly(assembly))` → `AddMediator(...)`) and 2x MSG0005 (TAAK-136).

## Open

- IDE0060 after `mediatr.add-mediatr`: a parameter that only went to `RegisterServicesFromAssembly` becomes unused. Worth a new
  task (Manual message or `_ = assembly;`?), not in this task.

## See also

- `src/Unshackle.Core/UsingDirectives.cs`, `src/Unshackle.Core/Fixer.cs`
- `claude/tasks/TAAK-131-mediatr-namespace-schaduw.md`
