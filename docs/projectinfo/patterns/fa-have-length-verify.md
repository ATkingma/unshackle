# Verification: fa-have-length

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-have-length-plan.md`](fa-have-length-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs`; registration in `FluentAssertionsFix.cs` (also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple` |
| REQ-2 | Met | `because` |
| REQ-3 | Met | `nullable` |
| REQ-4 | Met | `receiver-parens` |
| REQ-5 | Met (becauseArgs replaced) | `stream`, `assertion-scope` |
| REQ-6 | Met | `AndChain/equal-length-approximately` |
| Extension TAAK-097 | Met | `because-args.after.cs` (`string.Format(CultureInfo.InvariantCulture, ...)`) |

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

- [fa-have-length-modellen.md](fa-have-length-modellen.md)
- `claude/docs/patroon-loop.md`
