# Verification: fa-contain-single-which

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-single-which-plan.md`](fa-contain-single-which-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis beaa9c6).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs`; registration in `FluentAssertionsFix.cs` (also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple` |
| REQ-2 | Met | `predicate`, `enumerable` |
| REQ-3 | Met | `because` |
| REQ-4 | Met | `which` |
| REQ-5 | Met | `nullable` |
| REQ-6 | Partly replaced | `expression-variable`, `assertion-scope` (Manual); `which-chain` and `because-args` are now Fixed |
| Extension TAAK-096 | Met | `which-chain.after.cs` |
| Extension TAAK-097 | Met | `because-args.after.cs` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (432 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## See also

- [fa-contain-single-which-modellen.md](fa-contain-single-which-modellen.md)
- `claude/docs/patroon-loop.md`
