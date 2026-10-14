# Verification: fa-throw

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-throw-plan.md`](fa-throw-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 135f8e5).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `simple.*.cs` |
| REQ-2  | Met | `func.*.cs` |
| REQ-3  | Met | `because.*.cs` |
| REQ-4  | Met | `lambda.*.cs` |
| REQ-5  | Met | `message-contains.*.cs` |
| REQ-6  | Met | `message-starts.*.cs`, `message-ends.*.cs` |
| REQ-7  | Met | `message-exact.*.cs` |
| REQ-8  | Met | `message-because.*.cs`, `message-exact-because.*.cs` |
| REQ-9  | Replaced | by Extension ThrowExactly (`fa.throw-exactly`) |
| REQ-10 | Replaced | by Extension TAAK-097 |
| REQ-11 | Met | `message-wildcard.*`, `message-variable.*` |
| REQ-12 | Partly   | `chain.*`, `edge-inner-exception.*` (`.Which` part replaced by TAAK-096); the `.And.Throw` form has no proof (does not compile with FA 7.2.2), the code branch (`GetSubject` is null) is exercised via the shared helper |
| REQ-13 | Met (Invoking part replaced) | `value-func.*` |
| ThrowExactly | Met | `ThrowExactly/*.after.cs` |
| TAAK-097 | Met | `because-args.after.cs`, `message-because-args.after.cs` |
| TAAK-096 | Met | `value-used.after.cs`, `edge-which-message.after.cs`; Manual: `chain`, `edge-inner-exception` |
| Invoking | Met | `Invoking/simple`, `func`, `throw-exactly` (Fixed); `Invoking/side-effect-receiver`, `value-type`, `method-group` (Manual) |
| AggregateException | Met | `aggregate-wait`, `aggregate-method-group` (Manual); `aggregate-expected`, `aggregate-get-result` (Fixed) |
| Line endings | Met | `message-line-break`, `edge-multiline-message` (Fixed); `message-contains-line-break` (Manual) |
| TAAK-100 | Met | `ThrowExactly/assertion-scope`, `Invoking/assertion-scope` |
| TAAK-070 | Met | `edge-assertion-in-throw-lambda` (`.manual.txt`): `Throw<T>`/`ThrowAsync<T>` with T ≠ `Exception`/`ShouldAssertException` on an action with an FA assertion → Manual (`ExpectsAssertionFailure`) |
| TAAK-220 | Met | `aggregate-helper-method`, `aggregate-exception-and` (Manual), `aggregate-exception-plain` (Fixed); `ThrowMessage.MayThrowAggregate` now follows own methods (2 levels) and, for `T = Exception`, checks whether the chain uses the exception |
| TAAK-222 | Met | `Throw/assertion-helper`, `BeTrueFalse/assertion-helper` (Manual); `FluentAssertionsSyntaxTests.IsInsideExpectedFailure_HelperInOtherFile_*`, `ExpectsAssertionFailure_FollowsHelperInOtherFile` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; documented difference | Low |
| 2 | Behaviour | `WithMessage` equal to FA | The pattern is rebuilt as a string literal (verbatim/raw formatting is lost) | Low |
| 3 | Behaviour | `WithMessage` equal to FA | FA ignores `\r\n` vs `\n`. Exact message with a line break → `ShouldBe(.., IgnoreCase \| IgnoreLineEndings)`; `*` form with a line break → Manual (Shouldly's ShouldContain/StartWith/EndWith have no line-ending option). Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-047, `message-line-break.*`, `message-contains-line-break.*`) | Low |
| 4 | Behaviour | `Throw<T>` equal to FA | FA unwraps `AggregateException`, Shouldly does not. Heuristic: if the traceable action (lambda, local delegate variable, method group) blocks on `Task.Wait`/`WaitAll`/`.Result` or calls `Parallel.For/ForEach/Invoke`, and `T` does not accept an `AggregateException` → Manual. Untraceable actions (parameter, field) and indirect calls are converted as usual. `GetAwaiter().GetResult()` and `WaitAny` do not wrap and stay Fixed. Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-048, `aggregate-*.*`) | Medium |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Test  | `dotnet test`  | passed (98 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (15 tests) |

## Corpus (TAAK-170, TAAK-173)

- Polly 7.2.4 (`b1a5a17`, 2026-10-13, harness without clone changes): Fixed 4449 → 5255, Manual 964 → 171, build 0 errors, tests 5685/5685 → 5685/5685, 0 regressions.

## TAAK-220..222 (2026-10-13)

- `dotnet build` 0 warnings; `dotnet test` 1093/1093; sample 38/38.
- Corpus round 5: Akka 1 regression (known, TAAK-200), Steeltoe 0, LiteDB 0 (was 10/18/26).

## See also

- `claude/docs/patroon-loop.md`
