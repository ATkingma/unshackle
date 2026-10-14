# TAAK-130 — MediatR: handler via marker interface in another file is missed (file order)

**Status**: done
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` (evolutionary-architecture ch. 1 and 2)

## Problem

`Fixer` runs the patterns file by file on the **already updated** solution. If the marker
(`interface IIntegrationEventHandler<in T> : INotificationHandler<T>`) is in a file that comes earlier
(`Common/...` before `Offers/...`), its `using MediatR` has already become `using Mediator`. The semantic model of the
handler file then sees `Mediator.INotificationHandler`, `MediatRSyntax.FindHandleMethods` finds nothing, and there is
**no Fixed and no Manual**. Consequence: all-or-nothing lets it through, the build breaks (CS0738 `Task Handle`).

Reproduced: two files `A.cs` (marker) + `B.cs` (handler) → handler missed; `A.cs` renamed to `Z.cs` → found.
The existing proof `MediatR/NotificationHandler/edge-marker-interface` is in one file and does not see this.

## What, where, how

- **Where**: `src/Unshackle.Core/Fixer.cs` (loop over `files`, lines ~64–80).
- **How**: run `Find` and the semantic questions on the model of the **original** solution
  (`original`), only rewrite on the running root; or first collect all findings, then apply them.
  Affects every cross-file symbol check (marker interfaces, behavior constraints, base classes), also for FA.
- A proof is not possible (proofs are a single file): unit test in `src/Unshackle.Tests/Core/` with two documents, marker
  in the file that comes first alphabetically.

## Steps

- [ ] Red test: two documents, handler file after marker file
- [ ] Fixer on original semantics
- [ ] `dotnet build` + `dotnet test` green

## Outcome (2026-10-11)

**Status**: done. `Fixer` fixes each file in a working copy of the **original** solution (only that file changes)
and then puts the final text into the collected solution. Test `FixerTests.FixAsync_MarkerInterfaceInOtherFile_FindsHandlerWhateverTheFileOrder`
(marker before/after the handler file, real `MediatRFix`); was red with marker first. The proof runner did not need changes.

Evidence: evo1 CS0738 gone (build still red on MSG0005 = TAAK-136 and IDE0005/IDE0065/IDE0060 due to using placement in
`InMemoryEventBusModule.cs`, `PassExpiredEventHandler.cs`, `RegisterEndpoint.cs` - new, no task yet). evo2: 25 Fixed (sim: 22,
the missed handlers are now found); build only still red on TAAK-131 (CS0246/CS1961 due to `*.Mediator` shadowing).

## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/archief/TAAK-080-mediatr-marker-interface-handler.md`
