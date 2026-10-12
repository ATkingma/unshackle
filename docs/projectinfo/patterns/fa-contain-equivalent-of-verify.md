# Verification: fa-contain-equivalent-of

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-equivalent-of-plan.md`](fa-contain-equivalent-of-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 met de hand                                         |

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs`, registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple`, `strings`, `nullable-value` |
| REQ-2 | Gehaald | `sealed-record`, `record-struct` |
| REQ-3 | Gehaald | `because` |
| REQ-4 | Gehaald | `nullable-subject` |
| REQ-5 | Gehaald | `class`, `record-unsealed`, `record-list-member`, `dictionary`, `different-type` |
| REQ-6 | Gehaald | `options`, `assertion-scope`, `which` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | passed (668) |
| Volledige suite | `dotnet test --project src/Unshackle.Tests` | 35 bekende CRLF-failures op schone main, verder groen |

## Zie ook

- [fa-contain-equivalent-of-modellen.md](fa-contain-equivalent-of-modellen.md)
