---
kingmadoc: 1
feature: "fa-throw"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/Throw/
---
# Feature: fa.throw: act.Should().Throw<T>() (+ WithMessage) becomes Should.Throw<T>(act) (+ Message check)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-008. Everything marked _(inferred)_ comes from the codebase analysis.

## One-sentence summary

fa.throw: `act.Should().Throw<T>()` becomes `Should.Throw<T>(act)`; a directly following `.WithMessage(pattern)` becomes a `.Message.ShouldContain/StartWith/EndWith/Be(...)` on the same statement.

## Scope (in / out)

**In scope**

- Standalone `Throw<T>()` on an `Action` or `Func<T>` (T a reference type), with optional `because` without `becauseArgs`.
- A directly following `.WithMessage(p[, because])` with a constant string `p` without wildcards other than a leading and/or trailing `*`.
- `Manual(reason)` for `ThrowExactly`, chains, used result, `Invoking(...)`, other subject types and untranslatable patterns.

**Out of scope**

- `ThrowAsync` (row 7), `NotThrow` (row 8), `.WithInnerException`, `.Where`, `.WithParameterName` (stay Manual).
- Management of `using` directives: the `Fixer` does that via `IFix`.

## Requirements

- **REQ-1**: WHEN `act.Should().Throw<T>()` is a statement on an `Action` THE SYSTEM SHALL replace it with `Should.Throw<T>(act)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/simple.*.cs`).
- **REQ-2**: WHEN the subject is a `Func<T>` with T a reference type THE SYSTEM SHALL produce `Should.Throw<T>(func)` (proof `.../Throw/func.*.cs`).
- **REQ-3**: WHEN a `because` without `becauseArgs` is present THE SYSTEM SHALL carry it over as the second argument (`customMessage`) of `Should.Throw` (proof `.../Throw/because.*.cs`).
- **REQ-4**: WHEN the assertion is the body of a void lambda THE SYSTEM SHALL rewrite it (proof `.../Throw/lambda.*.cs`).
- **REQ-5**: WHEN `.WithMessage("*x*")` directly follows `Throw<T>()` THE SYSTEM SHALL produce `Should.Throw<T>(act).Message.ShouldContain("x", Shouldly.Case.Insensitive)` (proof `.../Throw/message-contains.*.cs`).
- **REQ-6**: WHEN the pattern is `x*` or `*x` THE SYSTEM SHALL use `ShouldStartWith` or `ShouldEndWith` respectively with `Case.Insensitive` (proofs `message-starts.*`, `message-ends.*`).
- **REQ-7**: WHEN the pattern contains no wildcards THE SYSTEM SHALL produce `ShouldBe(x, Shouldly.StringCompareShould.IgnoreCase)` (proof `message-exact.*`).
- **REQ-8**: WHEN `WithMessage` has a `because` THE SYSTEM SHALL carry it over as the `customMessage` of the message check; for `ShouldBe` before the `StringCompareShould` (proofs `message-because.*`, `message-exact-because.*`).
- **REQ-9**: IF `ThrowExactly<T>` is used THEN THE SYSTEM SHALL produce Manual, because Shouldly's `Throw<T>` also accepts derived types (proof `throw-exactly.*`).
- **REQ-10**: IF `becauseArgs` are used (on `Throw` or `WithMessage`) THEN THE SYSTEM SHALL produce Manual (proofs `because-args.*`, `message-because-args.*`).
- **REQ-11**: IF the pattern contains a `*`/`?` in the middle of the text or a `?`, or is not a string constant, THEN THE SYSTEM SHALL produce Manual (proofs `message-wildcard.*`, `message-variable.*`).
- **REQ-12**: IF the result is used or chained further (`.Which`, `.WithInnerException`, variable) THEN THE SYSTEM SHALL produce Manual (proofs `chain.*`, `value-used.*`); also if the assertion itself is a continuation (`x.Should().A().And.Throw<T>()`, no proof: this does not compile with FA 7.2.2).
- **REQ-13**: IF the subject is built with `Invoking(...)`, or is a `Func<ValueType>` THEN THE SYSTEM SHALL produce Manual (proofs `invoking.*`, `value-func.*`).
- **Extension ThrowExactly**: `ThrowExactly<T>` no longer becomes Manual: `ThrowExactlyPattern` (`fa.throw-exactly`) finds it and has this `Rewrite` build `Should.Throw<T>(act).ShouldBeOfType<T>()` (proofs in `ThrowExactly/`). Replaces REQ-9; there is no `Throw/throw-exactly.*` proof.
- **Extension TAAK-097**: WHEN `becauseArgs` accompany a constant, formattable `because` (on `Throw` or `WithMessage`) THE SYSTEM SHALL produce `string.Format(CultureInfo.InvariantCulture, because, args)` as `customMessage` (proofs `because-args.*`, `message-because-args.*`, now `.after.cs`); named arguments or a non-constant `because` stay Manual. Replaces REQ-10.
- **Extension TAAK-096**: WHEN `.Which` directly follows `Throw<T>()` THE SYSTEM SHALL drop `.Which`, since `Should.Throw<T>` returns the exception (proofs `value-used.*`, `edge-which-message.*`, now Fixed); `.WithInnerException`, `.And`, `.Where` stay Manual (proofs `chain.*`, `edge-inner-exception.*`). Replaces the `.Which` part of REQ-12.
- **Extension Invoking**: `x.Invoking(s => ...)` subjects are found by `fa.invoking` (`InvokingPattern`), which calls this `Rewrite`: a stable reference receiver plus lambda becomes `Should.Throw<T>(() => ...)` (proofs `Invoking/simple`, `func`, `throw-exactly`, ...); otherwise Manual (`Invoking/side-effect-receiver`, `value-type`, `method-group`). Any other FA-built subject stays Manual. Replaces the Invoking part of REQ-13; there is no `Throw/invoking.*` proof.
- **Extension TAAK-287**: a subject that is a method group or variable of `Func<V>` (V a value type or an unconstrained `T`), or `Func<Task<V>>` for ThrowAsync, is no `Func<object?>`: it is wrapped as `() => f()` (the result is boxed and dropped, as FluentAssertions drops it; probed against FA 7.2.2). A call as subject goes into a local first, so it runs once; a maybe-null subject stays Manual (proofs `Throw/value-func*`, `ThrowAsync/value-func*`).
- **Extension AggregateException**: IF the action visibly blocks on a task or uses `Parallel.*` (`Wait`, `Result`, `WaitAll`, ...) and T does not accept an `AggregateException` THEN THE SYSTEM SHALL produce Manual, because FA unwraps `AggregateException` and Shouldly does not (proofs `aggregate-wait.*`, `aggregate-method-group.*`); `GetAwaiter().GetResult()` or an expected `AggregateException` stays Fixed (proofs `aggregate-get-result.*`, `aggregate-expected.*`). Does not apply to ThrowExactly.
- **Extension line endings**: WHEN the `WithMessage` pattern without wildcards contains a line break THE SYSTEM SHALL produce `ShouldBe(lit, StringCompareShould.IgnoreCase | StringCompareShould.IgnoreLineEndings)` (proofs `message-line-break.*`, `edge-multiline-message.*`); with a `*` as well, Manual (proof `message-contains-line-break.*`).
- **Extension TAAK-100**: IF the assertion is inside an unconverted `AssertionScope` THEN THE SYSTEM SHALL produce Manual "Inside an AssertionScope" (proofs `ThrowExactly/assertion-scope.*`, `Invoking/assertion-scope.*`).
- **Extension TAAK-170**: the whole `Throw<T>()` chain is converted via `ThrowChains`: `.And`/`.Which` → the returned exception, `.WithMessage`/`.WithParameterName` → checks on `Message`/`ParamName`, `.Where(x => p)` → `(p[x := e]).ShouldBeTrue()`, `.WithInnerException<TI>()` → `e.InnerException.ShouldNotBeNull().ShouldBeAssignableTo<TI>()`, `.WithInnerExceptionExactly<TI>()` → `ShouldBeOfType<TI>()`. A check that is not the last step splits the statement (`var exception = …;`); `var ex = <keten>;` stays if every use of `ex` is itself a chain. Proofs `Throw/result-*`, `chain`, `edge-inner-exception`; a passed-on or returned result stays Manual (`result-passed`, `result-local-passed`). Replaces REQ-12.
- **Extension TAAK-220**: IF the action blocks on a task (`Wait`/`.Result`/`WaitAll`/`Parallel.*`), also in a method from the own solution that it calls (two levels deep), and `T` is not an `AggregateException` (or derived) THEN THE SYSTEM SHALL produce Manual; if `T` is a base of `AggregateException` (`Exception`), only when the chain uses the exception (`.And`/`.Which`/step/local): FA gives the unwrapped inner exception, Shouldly the `AggregateException` (probe). Proofs `Throw/aggregate-helper-method`, `aggregate-exception-and` (Manual), `aggregate-exception-plain` (Fixed); `Invoking/aggregate-parameter-and`, `aggregate-parameter-message` (lambda parameter as receiver).
- **Extension TAAK-222**: IF `T` is an assertion exception (`XunitException`, `AssertionFailedException`, `AssertionException`, `AssertFailedException` or derived) and the action calls a method from the own solution containing an FA assertion THEN THE SYSTEM SHALL make the `Throw` Manual, and also the FA assertions in that helper (`IsInsideExpectedFailure`, all patterns), so that test and helper stay FA together. Proofs `Throw/assertion-helper`, `BeTrueFalse/assertion-helper`; unit tests `FluentAssertionsSyntaxTests` (helper in another file).

## Assumptions

- Shouldly 4.3.0 `Should.Throw<T>(Action|Func<object?>, string? customMessage)` accepts derived exceptions, just like FA's `Throw<T>` (established with a test program).
- FA's `WithMessage` compares the whole message against a case-insensitive wildcard pattern; the translations with `Case.Insensitive` do the same.
- The `Fixer` manages `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- The error message gets "Additional Info:" instead of FA's "because" prefix: documented difference.
- This pattern replaces a whole statement expression (the subject becomes an argument); with output in unusual formatting (multi-line) the spacing may change.
- `Func<int>` falls under Manual because Shouldly expects `Func<object?>`.

## Models

Class diagram, sequence diagram and decision tree: [fa-throw-modellen.md](fa-throw-modellen.md).

## Open questions

- None.
