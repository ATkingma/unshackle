# Verification: epplus-flow-types

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-flow-types-plan.md`](epplus-flow-types-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-27 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of the TAAK-327 worktree branch.

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `FlowTypes/parameter` (after) |
| REQ-2 | Met     | `FlowTypes/_Combined/parameter` (`StylesCombinedProofTests`, `EPPlusHarnessProofTests`) |
| REQ-3 | Met     | proof `FlowTypes/public-member` (manual) |
| REQ-4 | Met     | proof `FlowTypes/unfixed-use` (manual, `CellReadDiffers`) |
| REQ-5 | Met     | proof `FlowTypes/unfixed-caller` (manual) |
| REQ-6 | Met     | proof `FlowTypes/method-group` (manual) |
| REQ-7 | Met     | proof `FlowTypes/field` (manual, `FlowFieldNotMapped`) |
| REQ-8 | Met     | proof `FlowTypes/return` (manual, `FlowReturnNotMapped`) |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test --project src/Unshackle.Tests`: all passed (counts in TAAK-327).
- No corpus run (disk space); re-measure is open in TAAK-327.

## Deviations

- Only parameters are Fixed. Fields and return types (E18 step 1 asks for after proofs) are Manual with their own codes until the council answers questions 2 and 3 in TAAK-327 (conservative choice).
- `unfixed-caller` now passes `package.Workbook.Worksheets[1]` instead of a field, and `method-group` returns `Delegate`: the field and the `Action<ExcelWorksheet>` return type are now Manual for their own reason.
- A Manual helper is also reported by `epplus.unsupported` (two rows for one method); both Manual, so AllOrNothing is unchanged.
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-flow-types-plan.md), [modellen](epplus-flow-types-modellen.md)
