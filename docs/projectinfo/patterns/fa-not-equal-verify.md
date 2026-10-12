# Verification: fa-not-equal

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-equal-plan.md`](fa-not-equal-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 met de hand                                         |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs`, registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotEqual/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `same-type`, `interface-type`, `string-collection` |
| REQ-2 | Gehaald | `different-type`, `array-literal` |
| REQ-3 | Gehaald | `because`, `because-args` |
| REQ-4 | Gehaald | `nullable`, `same-type` (guard), `non-null` (geen guard) |
| REQ-5 | Gehaald | `dictionary`, `assertion-scope`, `result-used` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | passed (582) |

## Zie ook

- [fa-not-equal-modellen.md](fa-not-equal-modellen.md)
