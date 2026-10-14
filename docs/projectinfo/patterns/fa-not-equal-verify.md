# Verification: fa-not-equal

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-equal-plan.md`](fa-not-equal-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 by hand                                             |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs`, registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotEqual/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `same-type`, `interface-type`, `string-collection` |
| REQ-2 | Met | `different-type`, `array-literal` |
| REQ-3 | Met | `because`, `because-args` |
| REQ-4 | Met | `nullable`, `same-type` (guard), `non-null` (no guard) |
| REQ-5 | Met | `dictionary`, `assertion-scope`, `result-used` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | passed (582) |

## See also

- [fa-not-equal-modellen.md](fa-not-equal-modellen.md)
