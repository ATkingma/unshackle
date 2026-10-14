# TAAK-132 — MediatR: abstract generic base class wrongly Manual ("generic messages")

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` (kgrzybek modular-monolith: 10 of 12 `mediatr.unsupported`)

## Problem

`UnsupportedPattern` (line ~54) gives Manual for every generic type that implements `IRequest<T>`/`INotification`,
including `public abstract class CommandBase<TResult> : ICommand<TResult>`. The Mediator generator only registers
concrete types: verified with Mediator 3.0.2 (abstract `CommandBase<TResult> : IRequest<TResult>` +
`sealed Ping : CommandBase<int>` builds and `Send` returns the right answer). Via all-or-nothing the Manual blocks
the whole solution.

## What, where, how

- **Where**: `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`.
- **How**: exempt `symbol.IsAbstract` (and interfaces); concrete generic messages and generic handlers stay Manual.
  `derived-generic-message` (generic concrete type under a non-generic base) stays Manual.
- Proof: `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/edge-abstract-generic-base.*` (swap-only, builds with the generator).

## Steps

- [x] `IsAbstract` exemption
- [x] Move proof back out of `_Pending`, green
- [x] `dotnet build` + `dotnet test` green

## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
