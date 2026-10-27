# Verification: epplus-flow-types

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-flow-types-plan.md`](epplus-flow-types-plan.md) |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-27 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of the TAAK-327 worktree branch; REQ-7 to REQ-12 against the TAAK-336 branch.

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `FlowTypes/parameter` (after) |
| REQ-2 | Met     | `FlowTypes/_Combined/parameter` (`StylesCombinedProofTests`, `EPPlusHarnessProofTests`) |
| REQ-3 | Met     | proof `FlowTypes/public-member` (manual) |
| REQ-4 | Met     | proof `FlowTypes/unfixed-use` (manual, `CellReadDiffers`) |
| REQ-5 | Met     | proof `FlowTypes/unfixed-caller` (manual) |
| REQ-6 | Met     | proof `FlowTypes/method-group` (manual) |
| REQ-7 | Met     | proof `FlowTypes/field-ctor-parameter` (manual, `FlowFieldNotMapped`) |
| REQ-8 | Met     | proof `FlowTypes/return-nonliteral-name` (manual, `FlowReturnNotMapped`) |
| REQ-9 | Met     | `FlowTypes/_Combined/field` (`StylesCombinedProofTests`, `EPPlusHarnessProofTests`) |
| REQ-10 | Met    | proof `FlowTypes/return` (after); `FlowTypes/_Combined/return` (combined + harness) |
| REQ-11 | Met    | proofs `FlowTypes/chain` (after), `chain-recursive`, `chain-too-deep` (manual); `FlowTypes/_Combined/chain` (combined + harness) |
| REQ-12 | Met    | `EPPlusFixTests.Fix_NodeLeftManualByFlowTypesOrAddress_OneRowPerNode` |
| REQ-13 | Met    | proof `FlowTypes/range-parameter` (after); `FlowTypes/_Combined/range-parameter` (combined + harness) (TAAK-337) |
| REQ-14 | Met    | proof `FlowTypes/range-parameter-unfixed-use` (manual) (TAAK-337) |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: all passed (counts in TAAK-327 and TAAK-336).
- No corpus run (disk space); re-measure is open in TAAK-327.

## Deviations

- TAAK-336: a Fixed field needs a write of a migrated package, which a single-pattern proof cannot show (its before compiles against EPPlus only), so the `field` after proof is `FlowTypes/_Combined/field`; the single-pattern `field` proof was renamed `field-ctor-parameter` (its shape).
- `unfixed-caller` now passes `package.Workbook.Worksheets[1]` instead of a field, and `method-group` returns `Delegate`: the field and the `Action<ExcelWorksheet>` return type are now Manual for their own reason.
- E24 (TAAK-336): a Manual helper is no longer reported by `epplus.unsupported` too. The `Unsupported/cells`, `chart`, `internal-api` proofs and two `*UnsupportedTests` now use a constructor or a void method, so they still show an `epplus.unsupported` row.
- E22 maps only `Add("literal")`: a lookup by name or index in a helper cannot prove that the caller's package has that sheet (EPPlus returns null, ClosedXML throws).
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-flow-types-plan.md), [modellen](epplus-flow-types-modellen.md)
