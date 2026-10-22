# Verification: epplus-package

|               |                                                                                    |
| ------------- | ---------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/epplus-package-plan.md`](epplus-package-plan.md)       |
| **Status**    | Verified                                                                           |
| **Generated** | 2026-10-22 by KingmaDoc skill (by hand, `kingmadoc` not available)                 |

> Verified by an AI agent against the code of branch `worktree-agent-adf8e0efedecc7b94` (TAAK-309 step 3).

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met     | proof `Package/using-statement`; `EPPlusHarnessProofTests.Proof_AfterWritesSameWorkbook(Package/using-statement)` |
| REQ-2 | Met     | proof `Package/using-declaration`; harness row `Package/using-declaration` |
| REQ-3 | Met     | proofs `Package/used-worksheet`, `Package/byte-array`, `Package/invalid-sheet-name`; `EPPlusFixTests.Fix_SaveAsStream_ManualAndSolutionUntouched` |
| REQ-4 | Met     | proof `Package/existing-file` |
| REQ-5 | Met     | proof `Package/field`; `EPPlusFixTests.Fix_TwoPackagesInOneBlock_Manual` |
| REQ-6 | Met     | `CellCompareTests.Differences_SameWorkbook_IsEmpty`; `Differences_DeliberateDifference_IsReported` (10 rows: value, type, text, number format, font, fill, borders, merges, widths, sheet name); `HarnessProofs_CoverEveryFixedPackageProof`; `Proof_SaveAsOverExistingFile_BothOverwrite` |
| REQ-7 | Met     | `EPPlusFixTests.Fix_LicenseLineAndPackage_BothRewrittenAndAfterCompiles` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 1707 passed, 0 failed.
- No corpus run yet (TAAK-309 step 5).

## Deviations

- The harness reads both files with ClosedXML (one reader, so a difference is in the file). A built-in number format written as id only
  (EPPlus) or as id + code (ClosedXML) is normalised to the id for the 15 culture-invariant built-ins; the formatted text is compared separately.
- Found by the harness: `Column(i).Width = 20` is not the same column in both libraries (EPPlus 4.5.3.3 stores 20, ClosedXML adds the
  padding and reads EPPlus's back as 19.289); pinned by `Differences_SameColumnWidthNumber_IsReported`, input for queue row 9.
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-package-plan.md), [modellen](epplus-package-modellen.md)
