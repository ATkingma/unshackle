# Verification: fa-throw-async

|               |                                                                  |
| ------------- | ---------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-throw-async-plan.md`](fa-throw-async-plan.md) |
| **Status**    | Verified                                                         |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                              |

> Verified by an AI agent against the code at uncommitted changes (basis 41ff4a3).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs` (+ shared `ThrowMessage.cs`); registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/`, run by `ProofTests`.

| REQ    | Verdict | Evidence |
| ------ | ------- | -------- |
| REQ-1  | Met | `simple.*.cs` |
| REQ-2  | Met | `func-task-reference.*.cs` |
| REQ-3  | Met | `because.*.cs` |
| REQ-4  | Met | `message-contains.*.cs` |
| REQ-5  | Met | `message-starts.*.cs`, `message-ends.*.cs` |
| REQ-6  | Met | `message-exact.*.cs` |
| REQ-7  | Met | `message-because.*.cs`, `message-exact-because.*.cs` |
| REQ-8  | Replaced | by Extension ThrowExactly (`fa.throw-exactly`) |
| REQ-9  | Replaced | by Extension TAAK-097 |
| REQ-10 | Met | `message-wildcard.*`, `message-variable.*` |
| REQ-11 | Met | `no-await.*`, `where.*` |
| REQ-12 | Met | `value-used.*` |
| REQ-13 | Met (Awaiting part replaced) | `value-func.*` |
| ThrowExactly | Met | `ThrowExactly/async.*`, `ThrowExactly/async-message.*` |
| TAAK-097 | Met | `because-args.after.cs`, `message-because-args.after.cs` |
| Awaiting | Met | `Invoking/awaiting.*`, `Invoking/awaiting-exactly.*` |
| ConfigureAwait | Met | `edge-configure-await.*` |
| AggregateException | Met | `aggregate-result` (Manual), `aggregate-awaited` (Fixed) |
| Line endings | Met | `message-line-break` (Fixed), `message-contains-line-break` (Manual) |
| TAAK-100 | Met | `IsInsideAssertionScope` as the first check; proof `ThrowAsync/assertion-scope` |
| TAAK-221 | Met | `cancelled-inner`, `ThrowExactly/async-cancelled` (Manual); `ThrowExactly/async-task-canceled`, `result-local-and` (Fixed) |
| TAAK-220 (WaitAsync) | Met | `Invoking/awaiting-wait-async` (Manual) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Shared code | Own pattern file | Wildcard translation moved to `ThrowMessage` (shared with `ThrowPattern`, behaviour unchanged; Throw proofs green); `FluentAssertionsSyntax.TryGetArguments` skips the receiver of extension methods | Low |
| 2 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because` | Low |
| 3 | Scope | `.Where`/`.WithInnerException` | Manual with reason "not directly awaited" | Low |
| 4 | Scope | _(edge case)_ | `await ...ThrowAsync<T>()[.WithMessage(p)].ConfigureAwait(x)`: the ConfigureAwait stays after `Should.ThrowAsync<T>(act)` (TAAK-042, `edge-configure-await.*`) | Low |
| 5 | Behaviour | `WithMessage` equal to FA | FA ignores `\r\n` vs `\n`. Exact message with a line break → `ShouldBe(.., IgnoreCase \| IgnoreLineEndings)`; `*` form with a line break → Manual (Shouldly's ShouldContain/StartWith/EndWith have no line-ending option). Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-047, `message-line-break.*`, `message-contains-line-break.*`) | Low |
| 6 | Behaviour | `ThrowAsync<T>` equal to FA | FA unwraps `AggregateException`, Shouldly does not. Heuristic: if the traceable action (lambda, local delegate variable, method group) blocks on `Task.Wait`/`WaitAll`/`.Result` or calls `Parallel.For/ForEach/Invoke`, and `T` does not accept an `AggregateException` → Manual. Untraceable actions (parameter, field) and indirect calls are converted as usual. `GetAwaiter().GetResult()` and `WaitAny` do not wrap and stay Fixed. Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-048, `aggregate-*.*`) | Medium |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed |
| Test  | `dotnet test`  | passed (180 tests, incl. 18 ThrowAsync proofs) |

## TAAK-220..222 (2026-10-13)

- `dotnet build` 0 warnings; `dotnet test` 1093/1093; sample 38/38.
- Corpus round 5: Akka 1 regression (known, TAAK-200), Steeltoe 0, LiteDB 0 (was 10/18/26).

## See also

- `claude/docs/patroon-loop.md`
- [fa-throw-async-modellen.md](fa-throw-async-modellen.md)
