# Verification: fa-discard-assignment

|               |                                                                                      |
| ------------- | ------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-discard-assignment-plan.md`](fa-discard-assignment-plan.md) |
| **Status**    | Verified                                                                             |
| **Generated** | 2026-10-18 by KingmaDoc skill                                                        |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/FluentAssertions/DiscardAssignment/simple.*`, unit test `Rewrite_DiscardedAssertion_KeepsOnlyTheAssertion` |
| REQ-2 | Met | `DiscardAssignment/await.*` |
| REQ-3 | Met | `_Combined/discard-assignment.*`; buildenator corpus scan: Manual 318 → 46 together with TAAK-275 |

## Deviations

None.

## Validation results

| Check  | Command | Result |
| ------ | ------- | ------ |
| Build  | `dotnet build` | passed (0 warnings) |
| Test   | `dotnet test` | passed |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed |

## See also

- [fa-discard-assignment-modellen.md](fa-discard-assignment-modellen.md)
