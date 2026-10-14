# Verification: projectbestanden-migreren

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/features/projectbestanden-migreren-plan.md`](projectbestanden-migreren-plan.md) |
| **Status**    | Implemented                                                                                        |
| **Generated** | 2026-10-11, reviewed manually (TAAK-017)                                                           |

## Requirements

| REQ                                 | Outcome | Evidence                                                                                                                                                                |
| ----------------------------------- | ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 new package added             | met     | `XmlProjectFileStoreTests.EditAsync_PackageOnlyTransitive_AddsNewItemGroupToProject`, `..._CodeStillUsesPackage_KeepsItAndAddsTheNewOneNextToIt`                       |
| REQ-2 old package + `<Using>` gone  | met     | `..._CentralPackageManagement_SwapsReferenceUsingAndVersionInPlace`, `..._AddedPackageAlreadyThere_RemovesOldLinesAndEmptyGroup`, `FixerProjectFileTests.FixAsync_CodeNoLongerUsesPackage_*` |
| REQ-3 FA stays + Manual             | met     | `FixerProjectFileTests.FixAsync_CodeStillUsesPackage_KeepsItAndReportsManual`; integration test first run (Manual on `SampleShop.Tests.csproj`)                         |
| REQ-4 `Version` or CPM              | met     | `..._VersionOnReference_*`, `..._VersionAsChildElement_SwapsIt`, `..._CentralPackageManagement_*`                                                                       |
| REQ-5 shared props                  | met     | `..._SharedPropsAndOneProjectKeepsPackage_*`, `..._ReferenceAndUsingInDirectoryBuildProps_EditsThePropsOnly`                                                            |
| REQ-6 formatting preserved          | met     | `..._CrlfAndDeclaration_KeepsThem`, `SaveAsync_FileWithBom_WritesNewTextWithBom`                                                                                       |
| REQ-7 `--dry-run` diff              | met     | `CommandTests.FixExecuteAsync_DryRunWithProjectFileChanges_ShowsTheirDiffAndWritesNothing`                                                                              |
| REQ-8 Fixed/Manual rows (Line 0)    | met     | `FixerProjectFileTests`; integration test checks the CSV rows (`project-file`, Line 0)                                                                                  |
| REQ-9 invalid XML → Manual          | met     | `EditAsync_InvalidXml_Throws` + `FixerProjectFileTests.FixAsync_EditThrows_ReportsManualInsteadOfFailing`                                                               |
| REQ-10 fixed SampleShop builds      | met     | `SampleShopTests.FixTwice_SampleShopCopy_MigratesProjectFilesAndBuildsAndTests` (fix → Manual by hand → fix → `dotnet build` + `dotnet test`)                           |

Extra: `SaveAsync_FileChangedSinceEdit_ThrowsAndKeepsFile` and `CommandTests.FixExecuteAsync_ProjectFileChangedOnDisk_ReturnsFailure` (risk "file changed between reading and writing").

## Deviations from the plan

- Store tests are in `src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs` (plan: `DataAccess/`), in line with `DataAccess/Reports/`.
- Extra file `src/Unshackle.DataAccess/Projects/ProjectFile.cs` (loaded XML + original text).
- Integration test does not check `obj/` for FluentAssertions: it contains restore files (`*.nuget.dgspec.json`) from before the fix.

## See also

- [projectbestanden-migreren-plan.md](projectbestanden-migreren-plan.md)
- `claude/tasks/archief/TAAK-017-projectbestanden-migreren.md`
