# Verification: fa-invoking

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-invoking-plan.md`](fa-invoking-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis d9d8f05).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` (claims the nodes), rewriting via `ThrowSyntax.Rewrite` in `ThrowPattern`, `ThrowAsyncPattern`, `NotThrowPattern` and `ThrowMessage`; registration in `FluentAssertionsFix.cs` (`s_assertions`, so also a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple, func` |
| REQ-2 | Met | `because-message` |
| REQ-3 | Met | `throw-exactly, awaiting-exactly` |
| REQ-4 | Met | `not-throw` |
| REQ-5 | Met | `awaiting` |
| REQ-6 | Met | `side-effect-receiver, value-type, method-group` |
| REQ-7 | Met (becauseArgs replaced) | `assertion-scope, aggregate` |
| Extension TAAK-097 | Met | `because-args.after.cs` |
| Extension TAAK-220 | Met | `aggregate-parameter-and`, `aggregate-parameter-message`, `awaiting-wait-async` (Manual): lambda parameter as receiver (`_ => _.Wait(..)`) with `Throw<Exception>` and a used exception, and `t => t.WaitAsync(..)` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (515 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Corpus (TAAK-170, TAAK-173)

- Polly 7.2.4 (`b1a5a17`, 2026-10-13, harness without clone changes): Fixed 4449 → 5255, Manual 964 → 171, build 0 errors, tests 5685/5685 → 5685/5685, 0 regressions.

## TAAK-220..222 (2026-10-13)

- `dotnet build` 0 warnings; `dotnet test` 1093/1093; sample 38/38.
- Corpus round 5: Akka 1 regression (known, TAAK-200), Steeltoe 0, LiteDB 0 (was 10/18/26).

## See also

- [fa-invoking-modellen.md](fa-invoking-modellen.md)
- `claude/docs/patroon-loop.md`
