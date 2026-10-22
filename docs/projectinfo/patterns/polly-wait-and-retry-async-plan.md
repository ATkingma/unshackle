---
kingmadoc: 1
feature: "polly-wait-and-retry-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-15]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/WaitAndRetryAsyncPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Core/Fixes/Polly/PollyPredicate.cs
    - src/Unshackle.Core/Fixes/Polly/PollyTargetFramework.cs
    - src/Unshackle.Core/Fixes/Polly/PollyOnRetry.cs
    - src/Unshackle.Core/Fixes/Polly/PollyFix.cs
    - src/Unshackle.Tests/Proofs/Polly/WaitAndRetryAsync/
---

# Feature: polly.wait-and-retry-async: WaitAndRetryAsync + ExecuteAsync → ResiliencePipeline with AddRetry

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308 step 4, queue row 1 (`claude/tasks/polly-queue.md`), council round 8 (general rule, P1, P2).

## One-sentence summary

polly.wait-and-retry-async: `Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay)` held in a local (or inline) and run only
through awaited `ExecuteAsync(() => ...)` calls becomes a `new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions { ... }).Build()`
pipeline, with the delay lambda kept as the `DelayGenerator`.

## Scope (in / out)

**In scope**

- `WaitAndRetryAsync(int, Func<int, TimeSpan>)` from assembly `Polly` (Polly 7, and Polly 8 still on the v7 API: council P5) on a
  receiver `Policy.Handle<E>()` without a predicate.
- The policy inline (`await Policy...WaitAndRetryAsync(...).ExecuteAsync(...)`) or in a local (`var` or an explicit type).
- `ExecuteAsync(Func<Task>)` and `ExecuteAsync(Func<Task<T>>)` with a parameterless lambda, directly awaited (also via `.ConfigureAwait(...)`).

**Out of scope (Manual, typed reason code, later queue rows)**

- `HandleInner`/`OrInner` and `HandleResult(value)`/`OrResult(value)` (row 2 keeps them Manual), `onRetry` and other overloads (row 3), fields,
  properties, return values and arguments holding the policy (row 5), sync `WaitAndRetry`/`Execute` (row 4).

## Requirements

- **REQ-1**: WHEN the shape is in scope THE SYSTEM SHALL emit `ShouldHandle = new PredicateBuilder().Handle<E>()`,
  `MaxRetryAttempts = n` and `.Build()` (proofs `local-block-delay`, `inline-result`, `typed-local-not-handled`).
- **REQ-2**: THE SYSTEM SHALL keep the delay lambda body verbatim in a local function `TimeSpan SleepDuration(int attempt)` called with
  `args.AttemptNumber + 1`, because v7 passes the 1-based retry number and v8 the 0-based attempt (council P2). The behaviour proof
  fails when the `+ 1` is dropped (`PollyBehaviourProofTests.Proof_WithoutAttemptOffset_DifferentTrace`).
- **REQ-3**: WHEN the callback is `async () => ...` THE SYSTEM SHALL write `async _ => ...`; WHEN it is `() => expr` with `expr` a
  `Task`/`Task<T>` THE SYSTEM SHALL write `_ => new ValueTask(expr)` / `_ => new ValueTask<T>(expr)` (no added await, so no captured context).
- **REQ-4**: WHEN the local has an explicit type THE SYSTEM SHALL change it to `ResiliencePipeline` (proof `typed-local-not-handled`).
- **REQ-5**: THE SYSTEM SHALL produce the same run-time trace before (Polly 7.2.4) and after (Polly.Core via Microsoft.Extensions.Resilience
  10.11.0): attempts, delay arguments, final exception or result, and an unhandled exception passing through on the first call
  (`PollyBehaviourProofTests`).
- **REQ-6**: WHEN the retry count is not an integer literal of at least 1 THE SYSTEM SHALL report Manual `RetryCountNotLiteral`
  (v8 rejects `MaxRetryAttempts = 0`; proof `retry-count-variable`).
- **REQ-7**: WHEN any use of the policy local is not an awaited, rewritable `ExecuteAsync` THE SYSTEM SHALL report the declaration and
  every call Manual with the same reason (all or none per local; proof `result-not-awaited`, unit tests).
- **REQ-8**: WHEN the policy is stored or passed on outside a local THE SYSTEM SHALL report Manual `PolicyNotLocal` (proof `policy-returned`).

- **REQ-9** (queue row 2): WHEN the receiver is a chain of `Policy.Handle<E>(pred?)`, `Policy.HandleResult<T>(pred)`, `Policy<T>.Handle<E>(pred?)`,
  `.Or<E>(pred?)` and `.OrResult(pred)` THE SYSTEM SHALL emit the same clauses in order on `new PredicateBuilder(<T>)()`
  (`.Handle<E>(pred?)`, `.HandleResult(pred)`); `HandleInner`, `OrInner` and a value instead of a predicate are Manual
  `ExceptionFilterNotSupported` (proofs `filter-or-predicate`, `filter-handle-result`, `filter-policy-of-t`, `filter-handle-inner`).
- **REQ-10** (queue row 2): WHEN the v7 builder is `PolicyBuilder<T>` THE SYSTEM SHALL emit `ResiliencePipelineBuilder<T>`,
  `RetryStrategyOptions<T>` and, for an explicitly typed local, `ResiliencePipeline<T>`; when retries run out on a handled result,
  both return that last result (behaviour proof `filter-handle-result`).
- **REQ-11** (council P9): WHEN the file's preprocessor symbols show a target framework Microsoft.Extensions.Resilience 10.11.0
  does not support (.NET Core before net8.0, .NET Framework before net462) THE SYSTEM SHALL report Manual `TargetFrameworkUnsupported`
  on each rewrite, and `PollyFix.ProjectBlockers` reports the project-level row with `ProjectBlockingReason.TargetFrameworkUnsupported`
  that blocks the project's file edits (council round 10, G4). Both stay: the project blocker alone would leave rewritten v8 code
  in a project that keeps Polly v7 only.
- **REQ-12** (council P10): WHEN `args`, `SleepDuration` or `_` is taken (a symbol in scope at the call, or an identifier inside the
  rewritten span) THE SYSTEM SHALL use `args1`, `args2`, ... (first free of 10), else Manual `NameClash`.

- **REQ-13** (queue row 3, council P3): WHEN `onRetry`/`onRetryAsync` (positional or named) is a lambda whose parameters are
  `Exception`, `DelegateResult<T>` (only `.Result`/`.Exception` used), `TimeSpan`, the retry count and an unused `Context`
  THE SYSTEM SHALL emit `OnRetry = args => { <ret> OnRetryCallback(...) <body>; ... }` called with `args.Outcome.Exception!`,
  `args.Outcome`, `args.RetryDelay`, `args.AttemptNumber + 1`; an async callback returns `new ValueTask(...)`
  (proofs `on-retry-attempt`, `on-retry-async`, `on-retry-result` + behaviour traces).
- **REQ-14** (queue row 3): WHEN the body reads the `Context` THE SYSTEM SHALL report Manual `OnRetryUsesContext` (proof
  `on-retry-context`); any other parameter use or type is Manual `OnRetryNotMappable`.

- **REQ-15** (queue row 5): WHEN the policy is held in a private field initialised at its declaration, in a type declared in one
  place, and every use (`_f` or `this._f`) is an awaited rewritable `ExecuteAsync` THE SYSTEM SHALL rewrite the field (type →
  `ResiliencePipeline(<T>)`) and every call (proof `field-static` + behaviour trace); a non-private field or a partial type is Manual
  `PolicyMemberNotPrivate` (proof `field-public`), any other use Manual `PolicyUsedBeyondExecute` (proof `field-passed-on`).
  Properties, return values, parameters and fields assigned elsewhere stay Manual `PolicyNotLocal`.

## Assumptions

- v8 `RetryStrategyOptions`: a non-null `DelayGenerator` result replaces `Delay`/`BackoffType`/`UseJitter`; `PredicateBuilder.Handle<E>()`
  matches `E` and derived types like v7 `Handle<E>()`. Pinned by the behaviour proofs, not only by reading.
- v7 and v8 both run the callback without the captured context and rethrow the last exception when retries run out.

## Risks

- `args` and `SleepDuration` are new names in the user's scope: a body already using either is Manual `NameClash`.

## Models

Class diagram, sequence and decision tree: [polly-wait-and-retry-async-modellen.md](polly-wait-and-retry-async-modellen.md).

## Open questions

- None for this row; package-level questions are in TAAK-308.
