# Verification: fa-order

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-order-plan.md`](fa-order-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing)                      |

> Verified against uncommitted changes (basis 0cb4140).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs`; registration in `FluentAssertionsFix.cs` (also as a link of `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Order/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `selector-ascending`, `selector-descending` |
| REQ-2 | Met | `no-selector` |
| REQ-3 | Met | `because`, `because-args` |
| REQ-4 | Met | `string-key`, `string-elements`, `string-because` |
| REQ-5 | Met | `nullable` |
| REQ-6 | Met | `comparer`, `comparison`, `selector-comparer`, `expression-variable` |
| REQ-7 | Met | `assertion-scope`, `result-used` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | all Order proofs green; 37 fail: known CRLF tests (XmlProjectFileStore, FixerTests, CommandTests, 2 SampleShopTests) and 2 proofs of the ongoing `fa.equivalent-options` of another agent |

## See also

- [fa-order-modellen.md](fa-order-modellen.md)
- `claude/docs/patroon-loop.md`
