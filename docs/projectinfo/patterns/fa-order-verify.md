# Verification: fa-order

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-order-plan.md`](fa-order-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt)                      |

> Verified against uncommitted changes (basis 0cb4140).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs`; registratie in `FluentAssertionsFix.cs` (ook als schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Order/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `selector-ascending`, `selector-descending` |
| REQ-2 | Gehaald | `no-selector` |
| REQ-3 | Gehaald | `because`, `because-args` |
| REQ-4 | Gehaald | `string-key`, `string-elements`, `string-because` |
| REQ-5 | Gehaald | `nullable` |
| REQ-6 | Gehaald | `comparer`, `comparison`, `selector-comparer`, `expression-variable` |
| REQ-7 | Gehaald | `assertion-scope`, `result-used` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | alle Order-proofs groen; 37 falen: bekende CRLF-tests (XmlProjectFileStore, FixerTests, CommandTests, 2 SampleShopTests) en 2 proofs van het lopende `fa.equivalent-options` van een andere agent |

## Zie ook

- [fa-order-modellen.md](fa-order-modellen.md)
- `claude/docs/patroon-loop.md`
