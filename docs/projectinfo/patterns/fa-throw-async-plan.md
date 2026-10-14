---
kingmadoc: 1
feature: "fa-throw-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/
---
# Feature: fa.throw-async: await act.Should().ThrowAsync<T>() (+ WithMessage) becomes await Should.ThrowAsync<T>(act) (+ Message check)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-012, building on `fa.throw` (TAAK-008).

## One-sentence summary

fa.throw-async: `await act.Should().ThrowAsync<T>()` becomes `await Should.ThrowAsync<T>(act)`; a directly following `.WithMessage(pattern)` becomes `(await Should.ThrowAsync<T>(act)).Message.ShouldContain/StartWith/EndWith/Be(...)`.

## Scope (in / out)

**In scope**

- A `ThrowAsync<T>()` awaited as a statement on a `Func<Task>` or `Func<Task<T>>` (T a reference type), with an optional `because` without `becauseArgs`.
- A directly following `.WithMessage(p[, because])` with a constant string `p` without wildcards other than a leading and/or trailing `*`.
- `Manual(reason)` for `ThrowExactlyAsync`, calls that are not awaited directly, a used result, `Awaiting(...)`/`Invoking(...)`, other subject types and untranslatable patterns.

**Out of scope**

- `Func<ValueTask>` (FA 7.2.2 has no `Should()` for it), `NotThrowAsync`, `ThrowWithinAsync`, `.WithInnerException`, `.Where` (stay Manual).
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `await act.Should().ThrowAsync<T>()` is a statement on a `Func<Task>` THE SYSTEM SHALL replace it with `await Should.ThrowAsync<T>(act)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/simple.*.cs`).
- **REQ-2**: WHEN the subject is a `Func<Task<T>>` with T a reference type THE SYSTEM SHALL give `await Should.ThrowAsync<T>(func)` (proof `.../ThrowAsync/func-task-reference.*.cs`).
- **REQ-3**: WHEN there is a `because` without `becauseArgs` THE SYSTEM SHALL take it over as the second argument of `Should.ThrowAsync` (proof `.../because.*.cs`).
- **REQ-4**: WHEN `.WithMessage("*x*")` directly follows `ThrowAsync<T>()` THE SYSTEM SHALL give `(await Should.ThrowAsync<T>(act)).Message.ShouldContain("x", Shouldly.Case.Insensitive)` (proof `.../message-contains.*.cs`).
- **REQ-5**: WHEN the pattern is `x*` or `*x` THE SYSTEM SHALL use `ShouldStartWith` or `ShouldEndWith` respectively with `Case.Insensitive` (proofs `message-starts.*`, `message-ends.*`).
- **REQ-6**: WHEN the pattern contains no wildcards THE SYSTEM SHALL give `ShouldBe(x, Shouldly.StringCompareShould.IgnoreCase)` (proof `message-exact.*`).
- **REQ-7**: WHEN `WithMessage` has a `because` THE SYSTEM SHALL take it over as the `customMessage` of the message check; for `ShouldBe` before the `StringCompareShould` (proofs `message-because.*`, `message-exact-because.*`).
- **REQ-8**: IF `ThrowExactlyAsync<T>` is used THEN THE SYSTEM SHALL give Manual (proof `throw-exactly.*`).
- **REQ-9**: IF `becauseArgs` are used (on `ThrowAsync` or `WithMessage`) THEN THE SYSTEM SHALL give Manual (proofs `because-args.*`, `message-because-args.*`).
- **REQ-10**: IF the pattern contains a `*`/`?` in the middle of the text, or is not a string constant, THEN THE SYSTEM SHALL give Manual (proofs `message-wildcard.*`, `message-variable.*`).
- **REQ-11**: IF the call is not awaited directly (no `await`, or followed by `.Where`/…) THEN THE SYSTEM SHALL give Manual (proofs `no-await.*`, `where.*`).
- **REQ-12**: IF the outcome of the await is used (variable) THEN THE SYSTEM SHALL give Manual (proof `value-used.*`).
- **REQ-13**: IF the subject is built with `Awaiting(...)`, or is a `Func<Task<ValueType>>` THEN THE SYSTEM SHALL give Manual (proofs `awaiting.*`, `value-func.*`).
- **Extension ThrowExactly**: `ThrowExactlyAsync<T>` is no longer Manual: `fa.throw-exactly` finds it and has this `Rewrite` build `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()` (proofs `ThrowExactly/async.*`, `async-message.*`). Replaces REQ-8; there is no `ThrowAsync/throw-exactly.*` proof.
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` (on `ThrowAsync` or `WithMessage`) THE SYSTEM SHALL give `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` (proofs `because-args.*`, `message-because-args.*`, now `.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-9.
- **Extension Awaiting**: `x.Awaiting(s => ...)` subjects are found by `fa.invoking` (`InvokingPattern`), which calls this `Rewrite`: a stable reference receiver plus lambda becomes `await Should.ThrowAsync<T>(async () => ...)` (proofs `Invoking/awaiting.*`, `awaiting-exactly.*`); otherwise Manual. Replaces the Awaiting part of REQ-13; there is no `ThrowAsync/awaiting.*` proof.
- **Extension ConfigureAwait**: WHEN `.ConfigureAwait(...)` sits between the call and the `await` THE SYSTEM SHALL keep it on `Should.ThrowAsync` (proof `edge-configure-await.*`).
- **Extension AggregateException**: IF the action visibly blocks (`.Result`, `Wait`, ...) and T does not accept an `AggregateException` THEN THE SYSTEM SHALL give Manual (proof `aggregate-result.*`); an awaited task stays Fixed (proof `aggregate-awaited.*`). Not for ThrowExactlyAsync.
- **Extension line endings**: WHEN the `WithMessage` pattern without wildcards contains a line break THE SYSTEM SHALL give `ShouldBe(lit, StringCompareShould.IgnoreCase | StringCompareShould.IgnoreLineEndings)` (proof `message-line-break.*`); with a `*` as well, Manual (proof `message-contains-line-break.*`).
- **Extension TAAK-100**: IF the assertion is inside an `AssertionScope` that is not converted THEN THE SYSTEM SHALL give Manual "Inside an AssertionScope" (no own proof; see `ThrowExactly/assertion-scope`).
- **Extension TAAK-170**: the same chain conversion as `fa.throw` (`ThrowChains`) on `await Should.ThrowAsync<T>(…)`; proofs `ThrowAsync/result-*`, `where`, `value-used`.
- **Extension TAAK-221**: IF `T` is `OperationCanceledException` or derived from it, and a step follows (`WithMessage`/`WithInnerException`/`Where`) or it is `ThrowExactlyAsync` (except exactly `TaskCanceledException`) THEN THE SYSTEM SHALL give Manual: if the task ends cancelled, Shouldly's `ThrowAsync` returns a new `TaskCanceledException` (without inner exception) (probe Shouldly 4.3.0). `.And.CancellationToken` stays Fixed (the new exception gets the task's token). Proofs `ThrowAsync/cancelled-inner`, `ThrowExactly/async-cancelled` (Manual), `ThrowExactly/async-task-canceled`, `ThrowAsync/result-local-and` (Fixed).
- **Extension TAAK-220 (async)**: IF the action is a lambda `x => x.WaitAsync(..)` (waits for a task created elsewhere) and `T` is not `AggregateException` THEN THE SYSTEM SHALL give Manual: if that task faulted with a nested `AggregateException`, FA flattens it and Shouldly does not (probe). Proof `Invoking/awaiting-wait-async`.

## Assumptions

- Shouldly 4.3.0 has `Should.ThrowAsync<T>(Func<Task>, string? customMessage)`; `Func<Task<T>>` with T a reference type converts via covariance. Like `Throw<T>` it accepts derived exceptions (established with a test program).
- FA's `WithMessage` on the task is an extension method (`ExceptionAssertionsExtensions`) that compares the whole message case-insensitively with a wildcard.
- The `Fixer` manages `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- The error message gets "Additional Info:" instead of FA's "because" prefix: a documented difference.
- The statement is replaced as a whole (await + chain); deviating formatting can change.
- `Func<Task<int>>` falls under Manual because there is no implicit conversion to `Func<Task>`.

## Models

Class, sequence and decision diagram: [fa-throw-async-modellen.md](fa-throw-async-modellen.md).

## Open questions

- None.

## See also

- `claude/docs/INDEX.md` — index for agents
- `claude/docs/patroon-loop.md` — how patterns are made and documented
- `docs/projectinfo/patterns/fa-throw-plan.md` — the synchronous pattern
