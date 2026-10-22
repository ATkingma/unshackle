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
| REQ-8 | Met     | proof `Package/plain-local` (harness equal) |
| REQ-9 | Met     | proofs `Package/worksheet-local`, `Package/worksheet-name` (harness equal), `Package/used-worksheet` |
| REQ-10 | Met    | proofs `Package/name-missing`, `Package/conditional-add`, `Package/rename-to-existing` |
| REQ-11 | Met    | proof `Package/worksheet-index`; `WorksheetIndexTests` (EPPlus 5 stub, assembly version 5.8.0.0; unknown version); `Harness_EPPlus4OnNetStandard_IsZeroBased` |
| REQ-12 | Partly met | `FontsLicenceTests` (Warning; 1.x Fixed; 2.x Manual `LicensedDependency`). The `PackageAddEdit` pin is not done: there is no assets-file reader behind a Core interface (engine work). |
| REQ-13 | Met    | proofs `Package/cells-values`, `Package/used-worksheet` (harness equal), `Package/cell-range` |
| REQ-14 | Met    | `CellValueHarnessTests` (covered types equal; `Value_UncoveredType_Differs` pins float 0.1f, 0.1 + 0.2, decimal.MaxValue and DateTime); proofs `uncovered-datetime`, `uncovered-double` |
| REQ-15 | Met    | `EPPlusFixTests.Fix_CellValuesAndStyles_RewrittenTogetherAndSameWorkbook` |
| REQ-16 | Met    | `WorksheetIndexTests`: `RewriteAsync_EPPlus4OneFamily_ByFamily` (net4x, netcore), `RewriteAsync_EPPlus4NotProvable_Manual` (netstandard pinned, unknown, packable, mixed, dependent of another family), `Harness_EPPlus4OnNetCore_IsZeroBased` |
| REQ-17 | Met    | `WorksheetIndexTests`: `RewriteAsync_KeyInConfig_WorksheetIndexConfigured`, `RewriteAsync_KeyInCode_WorksheetIndexConfigured`, `RewriteAsync_NoConfigSearch_WorksheetIndexConfigured`, Info on `RewriteAsync_EPPlus5Index_ZeroBasedToPosition`; proof `Package/worksheet-index` |
| REQ-7 | Met     | `EPPlusFixTests.Fix_LicenseLineAndPackage_BothRewrittenAndAfterCompiles` |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 1707 passed, 0 failed (rows 1-2); 1862 passed, 0 failed (row 3, after merging main); 1865 passed, 0 failed (E7); 2020 passed, 0 failed (rows 4-5); 2030 passed, 0 failed (E10/E11).
- No corpus run yet (TAAK-309 step 5).

## Deviations

- The harness reads both files with ClosedXML (one reader, so a difference is in the file). A built-in number format written as id only
  (EPPlus) or as id + code (ClosedXML) is normalised to the id for the 15 culture-invariant built-ins; the formatted text is compared separately.
- Found by the harness: `Column(i).Width = 20` is not the same column in both libraries (EPPlus 4.5.3.3 stores 20, ClosedXML adds the
  padding and reads EPPlus's back as 19.289); pinned by `Differences_SameColumnWidthNumber_IsReported`, input for queue row 9.
- Deviation from council E3 (needs the council): E3 says 4.x is 1-based, but the harness shows EPPlus 4.5.3.3's .NET Standard build (the
  one .NET Core and .NET 5+ load) is 0-based by default. Its .NET Framework builds default to 1-based, and both read an override from
  configuration. So 4.x integer indexes are Manual `WorksheetIndexBaseNotProvable`. 5+ is rewritten as decided, although 5+ also reads
  that configuration key.
- Row 3 widens this pattern instead of being a pattern of its own: a worksheet only exists through its package local, so one rewrite of the
  scope covers both (the queue said rows 3-9 widen the use set).
- Harness findings for E4: ClosedXML 0.105.1 writes a number with 15 significant digits and EPPlus 4.5.3.3 with 17, so 0.1f, 0.1 + 0.2 and
  decimal.MaxValue differ in the file. EPPlus writes a DateTime as a serial number with General format, where ClosedXML writes a date format.
- `RemovedNamespaces` also lists the common EPPlus sub-namespaces (`OfficeOpenXml.Style`, `.Drawing`, `.Drawing.Chart`, `.Table`, `.DataValidation`,
  `.ConditionalFormatting`); each is removed only when nothing binds to it.
- E11 "projects that can see it" is approximated by the whole solution: an override anywhere blocks every integer index (the cautious side).
- `IConfigKeySearch` is a Core interface in `Fixes/EPPlus/`. Until its DataAccess implementation is wired in `Cli.cs`, every integer index is Manual
  `WorksheetIndexConfigured`.
- Diagrams are inline Mermaid instead of D2/PNG under `img/`.

## See also

- [plan](epplus-package-plan.md), [modellen](epplus-package-modellen.md)
