# TAAK-094 — `await act.Should().NotThrowAsync()` → `await Should.NotThrowAsync(act)`

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (top-10 actions 1–4)

## Choices

New pattern `NotThrowAsyncPattern` (IPattern, like ThrowAsync: node = the await, `ConfigureAwait` stays; `ThrowSyntax.Follow` made public). No aggregate heuristic: NotThrow fails on any exception, including AggregateException (probed). Generic, `NotThrowAfterAsync`, non-awaited/used result, `Func<Task<valuetype>>`, `FluentActions.Awaiting` and becauseArgs → Manual.

## Evidence

Probe: ok, synchronous throw, async throw, AggregateException, `Func<Task<int>>`: same verdict (on a synchronous throw Shouldly throws the original exception; both red). Overloads: `Should.NotThrowAsync(Func<Task>, string)` and `(Task, string)`.

Probe code: scratchpad `p092/Program.cs` (FA 7.2.2 + Shouldly 4.3.0). Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/` (see `docs/projectinfo/architectuur/proofs.md`).

## Result

`dotnet build`, `dotnet test` (579) and the SampleShop tests (38) green. Scan: see `ANALYSE-fa-100.md` section "Na acties 1–4".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/README.md` (pattern table)
