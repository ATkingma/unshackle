# Verification: epplus-address

|               |                                                                            |
| ------------- | -------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-address-plan.md`](epplus-address-plan.md) |
| **Status**    | Verified                                                                   |
| **Generated** | 2026-10-27 by KingmaDoc skill (by hand, `kingmadoc` not available)         |

> Verified by an AI agent against the code of the TAAK-327 worktree branch (taak-327b).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | `Address/_Combined/rows-columns` (`StylesCombinedProofTests`) |
| REQ-2 | Met     | `Address/_Combined/rows-columns` |
| REQ-3 | Met     | `Address/_Combined/rows-columns` (`EPPlusHarnessProofTests`) |
| REQ-4 | Met     | proofs `Address/single-cell`, `Address/reversed-literal` (manual) |
| REQ-5 | Met     | proof `Address/start-cell` (manual) |
| REQ-6 | Met     | `Address/_Combined/range-local` (`StylesCombinedProofTests`, `EPPlusHarnessProofTests`) (TAAK-337) |
| REQ-7 | Met     | proof `Address/range-local-reassigned` (manual) (TAAK-337) |
| REQ-8 | Met     | `EPPlusFixTests.Fix_AddressReadInMemberReportedByUnsupported_OneRowWithAddressReason`; proof `Unsupported/address-folded` (manual) (TAAK-346 step 2a) |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test --project src/Unshackle.Tests`: all passed (counts in TAAK-327).
- No corpus run (disk space); re-measure is open in TAAK-327.

## Deviations

- Harness finding: a reversed literal address (`"C5:A2"`) throws `ArgumentOutOfRangeException` in EPPlus 4.5.3.3 (ClosedXML accepts it), so it is Manual; reversed int coordinates are sorted by both and stay Fixed.
- TAAK-337: range locals (E25) and range parameters (via `epplus.flow-types`) are mapped; range fields are not. Uses of a range local other than its address (values, styles, merge) are Manual for now.
- E26: non-positive int coordinates are accepted (documented limitation in the plan).
- A Manual address is also reported by `epplus.unsupported`; both Manual, so AllOrNothing is unchanged.
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-address-plan.md), [modellen](epplus-address-modellen.md)
