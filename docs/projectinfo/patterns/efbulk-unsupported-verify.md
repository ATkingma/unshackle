# Verification: efbulk-unsupported

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/efbulk-unsupported-plan.md`](efbulk-unsupported-plan.md) |
| **Status**    | Verified                                                          |
| **Generated** | 2026-10-27 by KingmaDoc                                           |

> Verified by an AI agent (TAAK-342 step 8) against the code on branch `taak-342e`; REQ-7/REQ-8 (steps 8a-8b) on branch `taak-342f`.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/EfBulk/Unsupported/`. Unit tests: `src/Unshackle.Tests/Core/EfBulk/EfBulkFixTests.cs`, `src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs`, `src/Unshackle.Tests/DataAccess/Projects/XmlProjectPropertiesTests.cs`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | bulk-insert; `FixAsync_BulkInsert_IsManualAndNamesTheMitFork` |
| REQ-2 | Met | future, from-cache, audit |
| REQ-3 | Met | manual proofs (code unchanged); `FixAsync_FutureLeft_KeepsThePackageAndReportsTheCallWithItsLine` checks the quoted call |
| REQ-4 | Met | `FixAsync_NoZUsageLeft_RemovesBothZPackages`, `EditAsync_NoAddedPackage_RemovesTheRemovedAndAlsoRemovedPackages` |
| REQ-5 | Met | `FixAsync_FutureLeft_KeepsThePackageAndReportsTheCallWithItsLine` |
| REQ-7 | Met | `FixAsync_ExtensionsOnlyProject_KeptRowNamesExtensions`, `FixAsync_ProjectFileUnreadable_KeptRowNamesPlusOrExtensions`, `FixAsync_ReferencedProjectGetsZOnlyTransitively_NoKeptRow` (TAAK-356), `PackageReferences_ProjectAndBuildProps_EveryInclude`, `PackageReferences_NotXml_Null` |
| REQ-8 | Met | `FixAsync_OtherZUsagesNextToManualRow_OneRowWithCountAndEveryLine` |
| REQ-6 | Met | engine behaviour (`ProjectPackages.KeepForDependents`), not re-tested for efbulk |

## Deviations

- `IncludeFilter` and `Deferred*` have no stub and no proof; their reasons are covered only by the name rules.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed |
| Tests | `dotnet test` | all green |

## See also

- [efbulk-unsupported-plan.md](efbulk-unsupported-plan.md)
- [efbulk-unsupported-modellen.md](efbulk-unsupported-modellen.md)