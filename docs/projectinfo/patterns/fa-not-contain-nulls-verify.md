# Verification: fa-not-contain-nulls

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-contain-nulls-plan.md`](fa-not-contain-nulls-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 by hand                                             |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotContainNullsPattern.cs`, registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotContainNulls/`, run by `ProofTests`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `simple`, `array`, `nullable-int` |
| REQ-2 | Met | `all-be-string`, `all-be-member-expected`, `all-be-constant-expression` |
| REQ-3 | Met | `because`, `because-args`, `all-be-because`, `all-be-because-args` |
| REQ-4 | Met | `name-conflict`, `all-be-name-conflict` |
| REQ-5 | Met | `nullable`, `all-be-nullable-string` |
| REQ-6 | Met | `selector`, `value-type`, `all-be-computed`, `assertion-scope`, `all-be-assertion-scope`, `result-used`, `all-be-result-used` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | passed (651) |
| Full suite | `dotnet test --project src/Unshackle.Tests` | 35 known CRLF failures on a clean main, otherwise green |

## See also

- [fa-not-contain-nulls-modellen.md](fa-not-contain-nulls-modellen.md)
