# Verification: fa-throw-exactly

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-throw-exactly-plan.md`](fa-throw-exactly-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis d9d8f05).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs` (claims the nodes), rewriting in `ThrowPattern`, `ThrowAsyncPattern`, `NotThrowPattern` and `ThrowMessage`; registration in `FluentAssertionsFix.cs` (`s_assertions`, so also a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowExactly/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple` |
| REQ-2 | Met | `because` |
| REQ-3 | Met | `message, async-message` |
| REQ-4 | Met | `async` |
| REQ-5 | Met | `aggregate-wait` |
| REQ-6 | Met (partly replaced) | `assertion-scope` |
| TAAK-097 | Met | `because-args.after.cs` |
| TAAK-096 | Met | `value-used.after.cs` |

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

## See also

- [fa-throw-exactly-modellen.md](fa-throw-exactly-modellen.md)
- `claude/docs/patroon-loop.md`
