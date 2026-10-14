# Verification: fa-contain-equivalent-of

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-equivalent-of-plan.md`](fa-contain-equivalent-of-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 by hand                                             |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs`, registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple`, `strings`, `nullable-value` |
| REQ-2 | Met | `sealed-record`, `record-struct` |
| REQ-3 | Met | `because` |
| REQ-4 | Met | `nullable-subject` |
| REQ-5 | Met | `class`, `record-unsealed`, `record-list-member`, `dictionary`, `different-type` |
| REQ-6 | Met | `options`, `assertion-scope`, `which` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | passed (668) |
| Full suite | `dotnet test --project src/Unshackle.Tests` | 35 known CRLF failures on clean main, otherwise green |

## See also

- [fa-contain-equivalent-of-modellen.md](fa-contain-equivalent-of-modellen.md)
