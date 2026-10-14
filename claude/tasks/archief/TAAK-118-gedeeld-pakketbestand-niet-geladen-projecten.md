# TAAK-118 — Shared package file loses a package that a non-migrated project still needs

**Status**: done (2026-10-11)
**Created**: 2026-10-11

## Problem

A project that is skipped during loading (TAAK-090/112/135) is not in the migrations. If it gets
FluentAssertions/MediatR via a shared `Directory.Packages.props` (CPM) or `Directory.Build.props`, then
`XmlProjectFileStore` removed the `PackageVersion`/`PackageReference` there → that project no longer builds.

## Rule

A shared file only loses the old package if no `.csproj` on disk below it still uses it.
Otherwise the line stays, the new package is added next to it and the Fixer reports Manual on that file.

## What, where, how

- `XmlProjectFileStore.UnloadedUsers` (DataAccess, `IFileSystem`): per shared file all `*.csproj` below its folder
  that are not migrated and have this file as their nearest `Directory.*.props`; use = `PackageReference`
  of a removed package in its own `.csproj` or in its `Directory.Build.props`. Unreadable = user.
- `ProjectFileChange.KeptRemovedPackageFor`: the project for which the shared file keeps the package.
- `Fixer.MigrateProjectFilesAsync`: such a change → Manual instead of Fixed.
- Projects whose code stays Manual: already covered by `KeepRemovedPackage` (TAAK-017).

## Tests

- `XmlProjectFileStoreTests`: CPM with an unloaded FA project, without FA, via shared Build.props, closer Build.props.
- `FixerProjectFileTests.FixAsync_SharedFileKeptForUnloadedProject_ReportsManual` (NSubstitute).
- `Integration/SharedPackageFileTests`: real temporary folder, CPM, one skipped project that uses FA.

## See also

- `docs/projectinfo/architectuur/core.md` § Projectbestanden
- `claude/tasks/archief/TAAK-135-loader-razor-en-vreemde-projecten.md`
