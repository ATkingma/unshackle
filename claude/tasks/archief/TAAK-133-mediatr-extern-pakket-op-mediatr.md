# TAAK-133 — MediatR: packages that themselves build on MediatR (Ardalis.SharedKernel) are not detected

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` (ardalis CleanArchitecture 9.1)

## Problem

`Ardalis.SharedKernel` 1.4.0 depends on MediatR 12.1.1 and provides `DomainEventBase : INotification`,
`ICommand<T>`/`IQuery<T>`/`ICommandHandler`, `MediatRDomainEventDispatcher`. The fix reported 15 Fixed / 0 Manual,
wrote 23 files, and the build breaks: CS0311 `ContributorDeletedEvent` (inherits `DomainEventBase`) is not a
`Mediator.INotification`. All-or-nothing promised exactly the opposite. MediatR also stays transitively in the graph.

## What, where, how

- **Where**: `MediatRFix` (before the patterns) or a new check in `UnsupportedPattern`.
- **How**: types in own code of which a base type/interface from an **assembly other than MediatR itself** implements MediatR
  (`AllInterfaces` with `ContainingAssembly` ≠ MediatR/MediatR.Contracts and ≠ own compilation) → Manual
  "<package> depends on MediatR; replace it or migrate it first". Possibly also: report a PackageReference with MediatR as
  a dependency.
- A proof is not possible (requires a second assembly): unit test with a `MetadataReference` from a second compilation.

## Steps

- [x] Red test with an external base class
- [x] Detection + Manual
- [x] `dotnet build` + `dotnet test` green

## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
