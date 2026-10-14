# TAAK-184 — MediatR: request processor that returns another Task (`return Task.Run(...)`)

**Status**: ✅ Done — 2026-10-12
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`; action 7 from the run of 2026-10-11)
**Goal**: nadirbad VerticalSlice gets past this Manual (after that it rightly stays Manual on the generic `DomainEventNotification<T>`).

## Context for the next agent

- nadir (`feb3a15e7ba03dc12404127e0a1f8f33b214783d`, MediatR 12.4.1): `src/Application/Common/Behaviours/LoggingBehaviour.cs:21 public Task Process(TRequest request, CancellationToken ct)` with `return Task.Run(...)`. `RequestProcessorPattern` requires `Task.CompletedTask` or `async`.
- Same shape as TAAK-154 (handlers): `return Expr;` → `return new ValueTask(Expr);` (synchronous exception timing stays the same).

## Steps

1. [x] Proof `Proofs/MediatR/RequestProcessor/` with `return Task.Run(...)` and an expression body.
2. [x] Rewrite in `RequestProcessorPattern.cs` to `new ValueTask(Expr)`.
3. [x] (Corpus run 2026-10-12 on 5f9b366 (`CORPUSRUN-mediatr.md`, section Repetition after TAAK-180..184): confirmed, the only root Manual on nadir is the generic notification; 30/30, 0 regressions) Proof: harness on nadir → the only root Manual is `mediatr.unsupported` (generic notification).

## Result

- `MediatRSyntax.WithCompletedValueTask`: any other returned `Task` becomes `new ValueTask(expr)` (only used by `RequestProcessorPattern`); Manual remains for non-`Task` types (including `Task<T>`).
- Proofs: `RequestProcessor/other-return` (now Fixed, was Manual), `RequestProcessor/task-run`. Pattern docs (plan REQ-4, verify, models) and `proofs.md` updated.

## See also

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `claude/tasks/archief/TAAK-154-mediatr-handler-expression-body-task.md`
