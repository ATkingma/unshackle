# TAAK-181 — Project file `<Project Sdk="..." />` (self-closing) cannot be edited

**Status**: ✅ completed — 2026-10-12
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`)
**Goal**: a csproj that consists only of `<Project Sdk="Microsoft.NET.Sdk" />` no longer blocks the project-file step.

## Context for the next agent

- kgrzybek modular-monolith (`91c8ef24b4cb6ef558c95d8267fa07d68c7059f8`): `src/Modules/Meetings/Domain/CompanyName.MyMeetings.Modules.Meetings.Domain.csproj` is literally `<Project Sdk="Microsoft.NET.Sdk" />` + CRLF.
- Message "Could not edit the project files: ...Meetings.Domain.csproj cannot be edited without changing unrelated text" on **all** projects (210 cascade Manuals). TAAK-134 (splicing) therefore does not cover this case.
- The fix probably does not need to change anything in this file, but it is checked anyway; or the self-closing root element becomes `<Project ...></Project>` in the round trip.

## Outcome

- Cause: the file itself is not the problem (the round trip is equal). The error comes when a migration adds a reference to such an empty root element (the "came in transitively" branch of `EditReferences`, e.g. MediatR -> Mediator.Abstractions in a Domain project): `RenderElement` threw for a self-closing element with new children.
- Fix: `ProjectFile.RenderElement` turns `<Project Sdk="..." />` + children into `<Project Sdk="...">...</Project>`; new lines in an empty root follow the file's line ending (`ProjectFile.NewLine`, used in `AddNextTo`), so CRLF stays CRLF.
- Tests: `XmlProjectFileStoreTests` (`EditAsync_ProjectWithoutContent_...` with 4 shapes incl. CRLF, `EditAsync_SelfClosingProjectOfThisSolution_...`). Step 3 (harness on kgr) not run: the corpus project was not available in this run; the unit tests imitate the case.
- Note: `XmlProjectFileStoreTests` already has failing tests on Windows because of `/repo` paths versus `C:\repo` in the `Text` helper; my tests therefore use `FullPathText`.

## Steps

1. [x] DataAccess test: a round trip of `<Project Sdk="Microsoft.NET.Sdk" />` (CRLF, without a final newline) keeps the text equal.
2. [x] Fix in `ProjectFile.cs`: do not check/write unchanged files; splice a self-closing root element.
3. [x] Proof: corpus run 2026-10-12 on 5f9b366 (`CORPUSRUN-mediatr.md`, section Repeat after TAAK-180..184): kgr no longer has a "cannot be edited" Manual; remaining 32 generic decorators/`DomainNotificationBase<T>` + 30 `unmatched-use` (Autofac `MediatorModule` with `MediatR.Pipeline` types), rightly Manual. 246/287, 0 regressions.

## Changed files

- `src/Unshackle.DataAccess/Projects/ProjectFile.cs`, `src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs`
- `src/Unshackle.Tests/DataAccess/Projects/XmlProjectFileStoreTests.cs`

## See also

- `src/Unshackle.DataAccess/Projects/ProjectFile.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
