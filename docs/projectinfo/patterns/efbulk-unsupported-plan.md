---
kingmadoc: 1
feature: "efbulk-unsupported"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/EfBulk/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/EfBulk/EfBulkFix.cs
    - src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs
    - src/Unshackle.Tests/Proofs/EfBulk/Unsupported/
---

# Feature: efbulk.unsupported: Bulk*/Future/FromCache/IncludeFilter/Audit → Manual, and the Z package removal rule

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-342 step 8 and council round 17 B5/B8. Undecided choices took the conservative option; see "Council questions" in `claude/tasks/TAAK-342-ef-bulk.md`.

## One-sentence summary

efbulk.unsupported: every other call or constructor of Z.EntityFramework.Plus / Z.EntityFramework.Extensions becomes a Manual `Unsupported` row that quotes the call, and both Z packages leave a project only when no Z usage is left in it or in a project that references it.

## Scope (in / out)

**In scope**: invocations and object creations that bind to a method or constructor of the Z assemblies, except the batch extensions of `efbulk.delete`/`efbulk.update`; the `PackageMigration` of the fix (remove `Z.EntityFramework.Plus.EFCore` and `Z.EntityFramework.Extensions.EFCore`, add nothing).

**Out of scope**: any rewrite of these calls; EFCore.BulkExtensions (council round 17 B2); member accesses or type names without a call (they still keep the package through the engine's usage check).

## Requirements

- **REQ-1**: WHEN a `Bulk*` method of Z is called THE SYSTEM SHALL report Manual `Unsupported` and name `EFCore.BulkExtensions.MIT` as an alternative that is not migrated (bulk-insert; B8).
- **REQ-2**: WHEN `Future`/`Deferred*`, `FromCache`, `IncludeFilter`/`IncludeOptimized` or the `Audit` type is used through a call or constructor THE SYSTEM SHALL report Manual `Unsupported` with the family's reason (future, from-cache, audit).
- **REQ-3**: THE SYSTEM SHALL quote the exact call in the reason (B4 style) and leave the code unchanged.
- **REQ-4**: WHEN no Z symbol is bound anymore in a project and in no project that references it THE SYSTEM SHALL remove both Z package references (and their central `PackageVersion`) and add nothing (unit tests `FixAsync_NoZUsageLeft_RemovesBothZPackages`, `EditAsync_NoAddedPackage_RemovesTheRemovedAndAlsoRemovedPackages`).
- **REQ-5**: WHEN a Z usage remains THE SYSTEM SHALL keep both packages and report the remaining usages with file:line: the `Unsupported`/Manual rows themselves, or an unmatched-use row for a file without a Manual row (unit test `FixAsync_FutureLeft_KeepsThePackageAndReportsTheCallWithItsLine`).
- **REQ-6**: WHEN a dependent project still uses Z THE SYSTEM SHALL keep the package in the projects it references (engine `KeepForDependents`, already covered by engine tests).

## Assumptions

- Package id = assembly name for both Z packages (true for the `.EFCore` packages; EF6 packages `Z.EntityFramework.Plus.EF6` are not matched and stay untouched).
- `XmlProjectFileStore`: a `PackageMigration` with no added package removes `RemovedPackage` like an `AlsoRemovedPackages` entry; no other fix has an empty `AddedPackages`.
- Kept-package rows name `Z.EntityFramework.Plus.EFCore` (the `RemovedPackage`) also for a project that references only the Extensions package.
- B8 check (2026-10-27, nuget.org): `EFCore.BulkExtensions.MIT` is MIT, latest 10.22.0 (2025-12-09), targets EF Core 10 (repo `videokojot/EFCore.BulkExtensions.MIT`).
- Z stubs (`EfBulkReferences.cs`) gained `FromCache`, `Audit` and `BulkInsert(Async)` with the shapes of the real API; not re-checked against the nupkg.

## Models

[efbulk-unsupported-modellen.md](efbulk-unsupported-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-342-ef-bulk.md` (13, 14).