# TAAK-048 — fa.throw/fa.throw-async: FA unwraps AggregateException, Shouldly does not

**Status**: completed (2026-10-11)
**Created**: 2026-10-11
**Found in**: TAAK-046 (semantic audit)

## Problem

FA 7 `Throw<T>()` and `ThrowAsync<T>()` also pass when the action throws an `AggregateException` with a `T` in it (FA unwraps it). `Should.Throw<T>` / `Should.ThrowAsync<T>` in Shouldly 4.3.0 do not.

Measured (`scratchpad/semprobe`):

- `Action` throws `AggregateException(new Derived())`: `act.Should().Throw<Derived>()` PASS, `Should.Throw<Derived>(act)` FAIL.
- `Func<Task>` likewise: `ThrowAsync<Derived>()` PASS, `Should.ThrowAsync<Derived>(f)` FAIL.

Typical in code that uses `Task.Wait()`/`.Result` or `Parallel.ForEach`. Consequence: a green test turns red. Which type is actually thrown cannot be seen statically.

## What, where, how (options)

- **Where**: `ThrowPattern.cs`, `ThrowAsyncPattern.cs` (`src/Unshackle.Core/Fixes/FluentAssertions/Patterns/`).
- **Option A (recommended)**: document as a known deviation in the KingmaDoc plan/verify + a report line (Fixed stays), because turning red is visible and the pattern is rare. No code change except docs.
- **Option B**: Manual when the action body syntactically contains `.Wait(`, `.Result`, `WaitAll`, `Parallel.` (heuristic, misses indirect cases).
- **Why not always Manual**: then the biggest pattern disappears for an edge case.
- The decision lies with the user.

## Steps

- [x] Record choice A/B (B)
- [x] With B: proofs `aggregate-*` in `Throw/` and `ThrowAsync/` (manual + fixed)
- [x] Update docs `docs/projectinfo/patterns/` fa.throw + fa.throw-async (Deviations)
- [x] `dotnet build` + `dotnet test` green

## Result

`ThrowMessage.MayThrowAggregate` (shared by both patterns) follows a lambda, a local delegate variable (also via a method group) or a method group and looks via symbols for `Task.Wait`/`WaitAll`/`Task<T>.Result`/`Parallel.For/ForEach/Invoke`; if `T` does not accept an `AggregateException` → Manual. Probe (`scratchpad/aggprobe`, FA 7.2.2/Shouldly 4.3.0): Wait/Result/WaitAll/Parallel.* FA PASS, Shouldly FAIL; `GetAwaiter().GetResult()` both PASS (unwraps itself, hence deliberately not Manual); `WaitAny` does not throw; `Throw<AggregateException>`/`Throw<Exception>` Shouldly PASS.

## See also

- `claude/tasks/TAAK-046-semantische-audit.md`

## User decision (2026-10-11)

Option B: heuristic. If the action can produce an `AggregateException` (`Task.Wait`, `.Result`, `.GetAwaiter().GetResult()`, `Parallel.*`, `Task.WaitAll`) → Manual with an explanation; otherwise simply convert. Start after TAAK-047 (same patterns).
