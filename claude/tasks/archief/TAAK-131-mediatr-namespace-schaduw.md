# TAAK-131 — MediatR: `using Mediator;` inside a namespace is shadowed by an own `*.Mediator` namespace

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` (evolutionary-architecture ch. 2)

## Problem

Fitnet has `namespace EvolutionaryArchitecture.Fitnet.Common.Infrastructure.Mediator` and puts usings inside the
(file-scoped) namespace. After the swap there is `using Mediator;` in `...Common.Infrastructure.Events`; C# looks that up
relatively first and finds its own namespace. Consequence: CS0246 `INotification`/`IPublisher` not found.
A project name or folder "Mediator" is common in MediatR projects (`MediatorModule`, `Mediator/Behaviors`).

## What, where, how

- **Where**: `UsingDirectives` (swap from `RemovedNamespaces` → `AddedNamespaces`).
- **How**: if `Mediator` binds to something other than the global namespace from the position of the using
  (`model.LookupNamespacesAndTypes` / does a `*.Mediator` namespace exist in the compilation), write `using global::Mediator;`.
- Proof (pending): `src/Unshackle.Tests/Proofs/_Pending/MediatR/Usings/edge-namespace-shadow.*`.

## Steps

- [x] Detection + `global::`
- [x] Move proof back out of `_Pending` (the proof runner must accept a folder for the usings swap), green
- [x] `dotnet build` + `dotnet test` green

## Result

- `UsingDirectives.IsShadowed`: on the in-place swap (`using MediatR;` → `Mediator`) inside a namespace declaration it
  checks whether an enclosing namespace (not the global one) has a member with the first name part (`A.Infrastructure.Mediator`).
  Then it becomes `using global::Mediator;`. New usings (no swap) fall under TAAK-115 (then at the top of the file).
- Proof moved from `_Pending/MediatR/Usings/` to `Proofs/MediatR/NotificationHandler/edge-namespace-shadow.*`
  (with a handler added, so the pattern produces a finding; the proof runner does not know a separate Usings folder).
- Corpus: fresh copy of evo2 (`scratchpad/v131/evo2`): 25 Fixed / 0 Manual, the CS0246s are gone. Build still fails on one
  IDE0060 (`MediatorModule.AddMediator(…, Assembly assembly)`: the parameter is unused after the AddMediatR rewrite) — separate.


## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
