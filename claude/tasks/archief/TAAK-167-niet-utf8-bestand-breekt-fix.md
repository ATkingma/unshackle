# TAAK-167 — A non-UTF-8 file breaks off the whole fix

**Status**: ✅ completed — 2026-10-12 (Linux follow-up 2026-10-14, `91f85de`)
**Created**: 2026-10-12 (corpus run, `claude/tasks/CORPUSRUN-fa.md`)
**Goal**: one Latin-1 file yields a Manual for that file, and the rest of the solution is migrated.

## Context for the next agent

- EventFlow `v0.83.4713` (`e544b76`): `Source/EventFlow.EntityFramework.Tests/MsSql/EfMsSqlReadStoreIncludeTests.cs` is ISO-8859-1.
- `fix` reported 483 Fixed / 59 Manual and then ended with `Unhandled exception: InvalidDataException: … is not UTF-8` (exit 1). Nothing was written.
- After converting the file to UTF-8 by hand: fix and build green, 970 tests, 0 regressions.

## Analysis

### Current situation

`XmlProjectFileStore.Decode` (`src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs:396-405`) throws `InvalidDataException`. `SaveAsync` (:143) checks all files before writing, and `FixCommand.ExecuteAsync` (:93) does not catch the exception.

### Problem

An error in one file blocks the whole migration, and the user gets a stack trace instead of a line in the report.

### New situation

The file is skipped with a Manual ("not UTF-8; convert it or migrate it by hand"). The FA package stays in that project (project-file cascade); the rest of the solution is written.

### Improvement

The run no longer breaks, and the user sees one line in the report.

## Approach

- Check the encoding before the rewrite (when loading in DataAccess) and return the result to Core as "file not editable" → Manual finding.
- Alternative: fall back to Latin-1 and keep that encoding when writing. Rejected: the real code page is not known.

## Steps

- [x] 1. DataAccess test: a Latin-1 file in a solution gives no exception, but a Manual.
- [x] 2. Implementation in `XmlProjectFileStore`/`FixCommand`, without a general `catch (Exception)`.
- [x] 3. Proof: the EventFlow harness without manual conversion gives `ok`.

## Choices and outcome

- The crash was not in `Decode` for project files but in `SaveAsync` for the changed `.cs` document; `FixCommand` did not catch `InvalidDataException`.
- Detection in Core (`Fixer.NotUtf8FilesAsync`), without I/O: Roslyn reads UTF-8 and BOM files with `UTF8Encoding` and everything else with
  its own encoding. That keeps Core free of DataAccess and there is no contract change.
- The file is skipped before all patterns, the global-using swap and the using update, so it is never changed and never sent to `SaveAsync`.
  It still names the old library, so the existing cascade keeps the package in that project.
- Only a file that names the old library gets a Manual (`encoding`); otherwise noise.
- Latin-1 as a fallback while keeping the encoding: rejected (code page unknown).
- `FixCommand` now also catches `InvalidDataException` from `SaveAsync` (race: file changed after loading) as a clean error message, no general `catch`.
- No proofs folder: a proof is a `.cs` in UTF-8; the encoding lives in the tests (Fixer unit test with `SourceText.From(code, Encoding.Latin1)` and an
  integration test on disk with a real Latin-1 file).
- **Follow-up 2026-10-14 (`91f85de`)**: the integration test failed only in CI (Linux). Roslyn decodes a non-UTF-8 file with the system ANSI
  code page, which on Linux/macOS is UTF-8 itself (replacement characters), so `Encoding` was a `UTF8Encoding` and the file was not detected;
  the strict `SaveAsync` check then refused (exit 1). `MsBuildSolutionStore` now gives such a file (text contains U+FFFD and the bytes are not
  valid UTF-8) its text as Latin-1, so every platform reports it as not UTF-8.

## Changed files

- `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Interface/Commands/FixCommand.cs`
- `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs` (follow-up)
- `src/Unshackle.Tests/Core/FixerProjectFileTests.cs`, `src/Unshackle.Tests/Integration/SampleShopTests.cs`
- `docs/projectinfo/architectuur/rapportage.md`

## Risks & edge cases

| Risk | Mitigation |
| --- | --- |
| The project loses the FA package while one file still uses FA | The Manual counts for the project-file cascade |

## See also

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/docs/INDEX.md`
