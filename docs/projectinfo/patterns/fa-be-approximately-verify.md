# Verification: fa-be-approximately

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-approximately-plan.md`](fa-be-approximately-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (base c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs`; registration in `FluentAssertionsFix.cs` (also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeApproximately/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple` |
| REQ-2 | Met | `because`, `close-to` |
| REQ-3 | Met | `not` |
| REQ-4 | Met | `close-to` |
| REQ-5 | Met | `nullable` |
| REQ-6 | Met | `int-close-to`, `nullable-not`, `assertion-scope`; becauseArgs replaced by REQ-8 |
| REQ-7 | Met | `AndChain/equal-length-approximately` |
| REQ-8 | Met | `because-args` (Fixed with `string.Format`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (474 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## See also

- [fa-be-approximately-modellen.md](fa-be-approximately-modellen.md)
- `claude/docs/patroon-loop.md`
