# Verification: projectbestanden-migreren

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/features/projectbestanden-migreren-plan.md`](projectbestanden-migreren-plan.md) |
| **Status**    | Implemented                                                                                        |
| **Generated** | 2026-10-11, handmatig beoordeeld (TAAK-017)                                                        |

## Requirements

| REQ                                 | Uitkomst | Bewijs                                                                                                                                                                  |
| ----------------------------------- | -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 nieuw pakket erbij            | gehaald  | `XmlProjectFileStoreTests.EditAsync_PackageOnlyTransitive_AddsNewItemGroupToProject`, `..._CodeStillUsesPackage_KeepsItAndAddsTheNewOneNextToIt`                       |
| REQ-2 oud pakket + `<Using>` weg    | gehaald  | `..._CentralPackageManagement_SwapsReferenceUsingAndVersionInPlace`, `..._AddedPackageAlreadyThere_RemovesOldLinesAndEmptyGroup`, `FixerProjectFileTests.FixAsync_CodeNoLongerUsesPackage_*` |
| REQ-3 FA blijft + Manual            | gehaald  | `FixerProjectFileTests.FixAsync_CodeStillUsesPackage_KeepsItAndReportsManual`; integratietest eerste run (Manual op `SampleShop.Tests.csproj`)                           |
| REQ-4 `Version` of CPM              | gehaald  | `..._VersionOnReference_*`, `..._VersionAsChildElement_SwapsIt`, `..._CentralPackageManagement_*`                                                                       |
| REQ-5 gedeelde props                | gehaald  | `..._SharedPropsAndOneProjectKeepsPackage_*`, `..._ReferenceAndUsingInDirectoryBuildProps_EditsThePropsOnly`                                                            |
| REQ-6 opmaak behouden               | gehaald  | `..._CrlfAndDeclaration_KeepsThem`, `SaveAsync_FileWithBom_WritesNewTextWithBom`                                                                                       |
| REQ-7 `--dry-run` diff              | gehaald  | `CommandTests.FixExecuteAsync_DryRunWithProjectFileChanges_ShowsTheirDiffAndWritesNothing`                                                                              |
| REQ-8 Fixed/Manual-rijen (Line 0)   | gehaald  | `FixerProjectFileTests`; integratietest controleert de CSV-rijen (`project-file`, Line 0)                                                                               |
| REQ-9 ongeldige XML → Manual        | gehaald  | `EditAsync_InvalidXml_Throws` + `FixerProjectFileTests.FixAsync_EditThrows_ReportsManualInsteadOfFailing`                                                               |
| REQ-10 gefixte SampleShop bouwt     | gehaald  | `SampleShopTests.FixTwice_SampleShopCopy_MigratesProjectFilesAndBuildsAndTests` (fix → Manual met de hand → fix → `dotnet build` + `dotnet test`)                       |

Extra: `SaveAsync_FileChangedSinceEdit_ThrowsAndKeepsFile` en `CommandTests.FixExecuteAsync_ProjectFileChangedOnDisk_ReturnsFailure` (risico "bestand gewijzigd tussen lezen en schrijven").

## Afwijkingen van het plan

- Store-tests staan in `src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs` (plan: `DataAccess/`), in lijn met `DataAccess/Reports/`.
- Extra bestand `src/Unshackle.DataAccess/Projects/ProjectFile.cs` (geladen XML + oorspronkelijke tekst).
- Integratietest controleert `obj/` niet op FluentAssertions: daar staan restore-bestanden (`*.nuget.dgspec.json`) van vóór de fix.

## Zie ook

- [projectbestanden-migreren-plan.md](projectbestanden-migreren-plan.md)
- `claude/tasks/TAAK-017-projectbestanden-migreren.md`
