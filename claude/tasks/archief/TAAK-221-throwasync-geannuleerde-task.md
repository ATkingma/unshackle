# TAAK-221 — `ThrowAsync`/`ThrowExactlyAsync` on a cancelled task → Shouldly returns its own `TaskCanceledException`

**Status**: ✅ completed (2026-10-13)
**Created**: 2026-10-13 (FA corpus run round 4)
**Goal**: no test regressions with async throw asserts on `OperationCanceledException`/`TaskCanceledException`.

## Context for the next agent

- Steeltoe `f13e514`: 18 regressions (6 tests × 3 TFMs), `<corpus>/corpus-fa/_results/r4/steeltoe/compare.json`.
- `await action.Should().ThrowExactlyAsync<OperationCanceledException>()` becomes `(await Should.ThrowAsync<OperationCanceledException>(action)).ShouldBeOfType<OperationCanceledException>()`. That fails with "should be of type OperationCanceledException but was TaskCanceledException" in MongoDb/RabbitMQ/Redis/RelationalDatabase `HealthContributorTest.Canceled_Throws` and `DiscoveryHttpDelegatingHandlerTest.DoesNotTrackStatistics_WhenRequestIsCanceled`.
- `ConfigServerConfigurationProviderTest.RemoteLoadAsync_HostTimesOut`: `ThrowAsync<TaskCanceledException>().WithInnerException<TimeoutException>()` → the inner exception is `null`.
- Presumed cause (step 1 confirms it): if the task ends in the status *Canceled* (and not *Faulted*), Shouldly 4.3 `Should.ThrowAsync` returns a self-created `TaskCanceledException` instead of the real exception. Type and inner exception are then lost. FA awaits the task and catches the real exception.

## Steps

- [x] 1. Repro in a unit test: a `Func<Task>` that does `token.ThrowIfCancellationRequested()`, with FA and with Shouldly.
- [x] 2. Choose the rewrite: Manual if `T` is `OperationCanceledException` or derived from it (or if `WithInnerException` follows), or a form that keeps the real exception (e.g. `Should.Throw<T>(() => action().GetAwaiter().GetResult())`). Agree with the user.
- [x] 3. Unit tests + proofs.
- [x] 4. Steeltoe repeat: 0 regressions.

## Result

Corpus round 5 (`CORPUSRUN-fa.md`): Akka 1 regression (only TAAK-200), Steeltoe 0, LiteDB 0. Build, `dotnet test` (1093) and sample tests (38) green.

## Choices

- **The probe confirms** (`<probe-dir>/probe220`): if the task ends *Canceled* (`ThrowIfCancellationRequested`, or an async method that throws an OCE/TCE), `Should.ThrowAsync<T>` returns a new `TaskCanceledException` without an inner exception; FA returns the real one (`OperationCanceledException`, or a TCE with inner `TimeoutException` respectively). `Should.Throw<T>(() => act().GetAwaiter().GetResult())` keeps the real exception.
- **Chosen: Manual** if `T` is (derived from) `OperationCanceledException` and a step follows (`WithMessage`/`WithInnerException*`/`Where`) or it is `ThrowExactlyAsync`. Exception: `ThrowExactlyAsync<TaskCanceledException>()` without a step stays Fixed (that is exactly the type Shouldly creates). The `GetAwaiter().GetResult()` form was rejected: it blocks synchronously, removes the `await` (CS1998 in async methods without another `await`, a build error with warnings-as-errors) and deviates from what the user wrote.
- `.And.CancellationToken`/`var ex = …` without a step stays Fixed: the new TCE gets the task's token (proof `ThrowAsync/result-local-and` stays green).
- Proofs: `ThrowAsync/cancelled-inner`, `ThrowExactly/async-cancelled` (Manual), `ThrowExactly/async-task-canceled` (Fixed).

## See also

- `claude/tasks/CORPUSRUN-fa.md` (round 4)
- `claude/tasks/patroon-queue.md`
