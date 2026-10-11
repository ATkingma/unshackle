# Verification: fa-direct-mapping

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-direct-mapping-plan.md`](fa-direct-mapping-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis 0079d44).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DirectMappingPattern.cs`; registratie in `FluentAssertionsFix.cs` (`s_assertions`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/DirectMapping/`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `be-one-of, be-one-of-convert` |
| REQ-2 | Gehaald | `be-subset-of, unique` |
| REQ-3 | Gehaald | `have-value, not-have-value` |
| REQ-4 | Gehaald | `be-one-of-enumerable, unique-key, contain-in-order` |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (621 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-direct-mapping-plan.md](fa-direct-mapping-plan.md)
- [fa-direct-mapping-modellen.md](fa-direct-mapping-modellen.md)
