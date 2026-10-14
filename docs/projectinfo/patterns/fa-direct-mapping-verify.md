# Verification: fa-direct-mapping

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-direct-mapping-plan.md`](fa-direct-mapping-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis 0079d44).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DirectMappingPattern.cs`; registration in `FluentAssertionsFix.cs` (`s_assertions`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/DirectMapping/`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | `be-one-of, be-one-of-convert` |
| REQ-2 | Met | `be-subset-of, unique` |
| REQ-3 | Met | `have-value, not-have-value` |
| REQ-4 | Met | `be-one-of-enumerable, unique-key, contain-in-order` |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (621 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## See also

- [fa-direct-mapping-plan.md](fa-direct-mapping-plan.md)
- [fa-direct-mapping-modellen.md](fa-direct-mapping-modellen.md)
