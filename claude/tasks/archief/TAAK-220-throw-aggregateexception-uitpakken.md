# TAAK-220 — `Throw<T>()` on a delegate that throws an `AggregateException` via `Wait()`/`WaitAsync` → test regression

**Status**: ✅ completed (2026-10-13)
**Created**: 2026-10-13 (FA corpus run round 4)
**Goal**: no more test regressions with `Invoking(..).Should().Throw<T>()` where the delegate throws an `AggregateException` that FA unwraps and Shouldly does not.

## Context for the next agent

- Akka.NET `23a8566`, `Akka.Streams.Tests`: 9 regressions (round 4, `<corpus>/corpus-fa/_results/r4/akka/compare.json`).
- FA 7 `Throw<T>()` unwraps an `AggregateException` and looks for `T` in the inner exceptions; Shouldly `Should.Throw<T>` does not. With `T = Exception` Shouldly does pass, but returns the `AggregateException`, so `.Should().Be(Ex)` or `.Message` fails afterwards.
- The existing Manual rule in `fa.invoking` ("The action blocks on a task … Wait/Result/WaitAll") does not see it when:
  - the receiver is the lambda parameter: `task.Invoking(_ => _.Wait(TimeSpan.FromSeconds(3))).Should().Throw<Exception>().And.Should().Be(Ex)` (QueueSourceSpec ×5), `future.Invoking(f => f.Wait()).Should().Throw<Exception>().WithMessage(..)` (FlowWatchTerminationSpec);
  - the delegate is a local variable: `fail.Should().Throw<TestException>()` (FlowScanSpec), `action.Should().Throw<Exception>()` (FlowSupervisionSpec);
  - it is an own blocking method: `p.Awaiting(t => t.WaitAsync(..)).Should().ThrowAsync<TestException>()` (SinkForeachAsyncSpec; Akka's `WaitAsync` throws an `AggregateException`).

## Steps

- [x] 1. Extend detection: `Wait`/`Result`/`GetAwaiter().GetResult()` also on lambda parameters; for a delegate in a local, follow the initializer (like TAAK-070).
- [x] 2. A delegate that cannot be analysed (own method, parameter, field): Manual or Fixed with the warning "FluentAssertions unwraps AggregateException". Agree with the user which of the two.
- [x] 3. Unit tests + proofs (`Proofs/FluentAssertions/Throw/aggregate-*`).
- [x] 4. Akka repeat: Akka.Streams.Tests 0 regressions.

## Result

Corpus round 5 (`CORPUSRUN-fa.md`): Akka 1 regression (only TAAK-200), Steeltoe 0, LiteDB 0. Build, `dotnet test` (1093) and sample tests (38) green.

## Choices

- **Probe** (FA 7.2.2 / Shouldly 4.3.0, net10.0, `<probe-dir>/probe220`): `Should.Throw<Exception>(() => t.Wait())` returns the `AggregateException`, FA `Throw<Exception>()` the inner `TestEx`. FA also unwraps with `Throw<SystemException>`; only `T` = `AggregateException` (or derived) keeps the aggregate. Shouldly `ThrowAsync<TestEx>` on a task with a nested `AggregateException(TestEx)` fails, FA flattens and passes.
- **Lambda parameter as receiver** (`_ => _.Wait(..)`) was already seen; the real cause in QueueSourceSpec/FlowWatchTermination was `T = Exception`: that accepts `AggregateException`, so the old rule let it through. Now: if `T` is a base of `AggregateException`, Manual as soon as the chain uses the exception (`.And`, `.Which`, a step, `var ex = …`); a bare `Throw<Exception>()` stays Fixed (both pass).
- **Delegate in a local** was already followed; FlowScan/FlowSupervision block in an own method (`Scan(..)`, `Run(..)`). `MayThrowAggregate` now follows methods with source code in the compilation, two levels deep.
- **`WaitAsync`**: cannot be proven statically (the nested aggregate originates in Akka). Chosen: Manual for a lambda `x => x.WaitAsync(..)` with `T` not `AggregateException`: the task comes from elsewhere. Manual instead of Fixed+warning, because Fixed makes the test fail.
- Delegates that cannot be followed (parameter, field) stay Fixed as before (no proof of blocking; otherwise too many Manuals).
- Proofs: `Throw/aggregate-helper-method`, `aggregate-exception-and`, `aggregate-exception-plain`; `Invoking/aggregate-parameter-and`, `aggregate-parameter-message`, `awaiting-wait-async`.

## See also

- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md`
- `claude/tasks/CORPUSRUN-fa.md` (round 4)
- `claude/tasks/patroon-queue.md`
