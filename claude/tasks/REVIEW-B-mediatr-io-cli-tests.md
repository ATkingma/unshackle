# REVIEW-B: MediatR, I/O, CLI and tests

Read-only review on `main` 3bd36cf. Scope: `Core/Fixes/MediatR/`, `DataAccess/`, `Interface/`, `Tests/` (Proofs runner, Helpers, Integration).
Out of scope: what TAAK-060/063/080/081 already touch (notification publisher config, source generator only in host, marker-interface handlers / `symbol.Interfaces` vs `AllInterfaces` in `FindHandleMethods`, behavior request constraint).

## High

### H1. `fix` writes halfway: source code is already on disk when a project file fails afterwards

- **Where:** `src/Unshackle.Interface/Commands/FixCommand.cs:80-88`, `src/Unshackle.DataAccess/Projects/XmlProjectFileStore.cs:85-99`
- **Problem:** first `_store.SaveAsync` (all `.cs`), then `_projectFiles.SaveAsync`. The staleness check (`text != change.OldText`) sits per file inside the write loop. If the second project file changed in the meantime (or is unreadable), the `.cs` files and the first project file have already been written.
- **Why:** the code is migrated to `Mediator`/Shouldly but the csproj still refers to the old package → build broken, half-applied migration. With `--force` (dirty tree) this cannot be undone via git. The test `FixExecuteAsync_ProjectFileChangedOnDisk_ReturnsFailure` only proves the exit code, not that nothing was written.
- **Fix:** separate a check phase in `IProjectFileStore` (e.g. `SaveAsync` first reads and compares all files, only then writes), and do that check in `FixCommand` before `_store.SaveAsync`. Test: two changes, the second stale → first file and solution untouched.

### H2. Source code is overwritten without a staleness check

- **Where:** `src/Unshackle.DataAccess/Solutions/MsBuildSolutionStore.cs:40-52`
- **Problem:** `TryApplyChanges` writes the documents based on the in-memory solution; changes the user makes between loading (the MSBuild load takes long) and writing are silently lost. For project files that check does exist.
- **Why:** data loss in user code, inconsistent with `XmlProjectFileStore`.
- **Fix:** before `TryApplyChanges`, compare `File.ReadAllText(document.FilePath)` per changed document with the loaded `SourceText` of `Original`; on a difference `InvalidOperationException` (already translated neatly in `FixCommand:85`). Combine with H1 so all checks happen before the first write.

### H3. A git error is read as a "clean working tree"

- **Where:** `src/Unshackle.DataAccess/SourceControl/GitSourceControl.cs:29-31, 40-41`
- **Problem:** every exit code ≠ 0 and a missing `git` give `false` = "no changes". So `fatal: detected dubious ownership` (exit 128, common on shared drives/WSL/containers), a broken index or a lock also give the green light to write.
- **Why:** the only protection against irreversible overwriting silently disappears, without a message.
- **Fix:** distinguish "no repository" (`git rev-parse --is-inside-work-tree` fails, or stderr contains `not a git repository`) from other errors; on other errors and on missing git: return `true` or an explicit error/warning in the view ("cannot check working tree, use --force"). Test with a fake git error (e.g. a directory without read permission).

### H4. No integration test for `--fix mediatr`

- **Where:** `src/Unshackle.Tests/Integration/SampleShopTests.cs` (all three tests use `fluentassertions`); `CliTests.cs:42` only tests "missing solution".
- **Problem:** `samples/SampleShop` contains MediatR code (handlers, behavior, `ServiceCollectionExtensions`), but the real chain MSBuild load → patterns → `Directory.Packages.props` edit (two added packages + `MediatR.Contracts` removed) → build + test with the source generator runs nowhere.
- **Why:** precisely the riskiest part (package swap with two new packages, a generator that writes `AddMediator`) is only covered in memory by unit proofs.
- **Fix:** parametrise `FixTwice_SampleShopCopy_...` over fixId, or a separate `FixMediatR_SampleShopCopy_BuildsAndTests` with `dotnet build`/`dotnet test` after the fix.

## Medium

### M1. The report can overwrite any file with `.md/.csv/.json`, also in the target repo

- **Where:** `src/Unshackle.DataAccess/Reports/*ReportWriter.cs` (`File.Create`/`WriteAllLinesAsync`), `ScanCommand.cs:47`
- **Problem:** `--report README.md` or `--report Directory.Packages.json`… overwrites without asking; with `fix` this happens after the git check, so the report itself also makes the tree dirty for a next run. No path traversal risk in the classic sense (a CLI argument from the user themselves), but silent data loss.
- **Fix:** refuse if the target already exists and was not written by Unshackle before, or at least: refuse if the path lies inside the solution folder and exists, without `--force`. Cheapest variant: `FileMode.CreateNew` unless `--force`.

### M2. Markdown report only escapes `|`

- **Where:** `src/Unshackle.DataAccess/Reports/MarkdownReportWriter.cs:34`
- **Problem:** `Reason` and `File` come from user code (e.g. `'{setting}'` in `AddMediatRPattern.cs:200` puts the source code literally into the reason). `<script>`, `<img onerror>`, backticks and a trailing `\` (which cancels the `\|` of the next cell) go into the table unescaped. Rendered in a PR comment or wiki that is HTML injection / a broken table.
- **Fix:** in `Cell` first `\` → `\\`, then `|` → `\|`, and HTML-encode `<`, `>`, `&` (`System.Net.WebUtility.HtmlEncode`). Add a test with `<script>` and a trailing backslash.

### M3. `AddMediatRPattern` duplicates/loses comments in the lambda block

- **Where:** `src/Unshackle.Core/Fixes/MediatR/Patterns/AddMediatRPattern.cs:211-214`
- **Problem:** every new statement gets the leading and trailing trivia of `body.Statements[0]`. If there is a comment above the first line (`// handlers`), it appears above `ServiceLifetime =` and above `PipelineBehaviors =`; comments on later statements disappear.
- **Fix:** copy only the whitespace indentation of `Statements[0]` (`.Where(t => t.IsKind(WhitespaceTrivia)).TakeLast(1)`, as `VoidRequestHandlerPattern.cs:521` already does) and put the comments of all original statements before the first new one. Add a proof.

### M4. Only `Statements` with expressions; `if (env.IsDev) cfg.AddBehavior...` → Manual, but `settings.Count == 0` also covers an empty block

- **Where:** `AddMediatRPattern.cs:158-168`
- **Problem:** `services.AddMediatR(cfg => { })` (empty) becomes Manual with a misleading reason ("not a list of cfg calls"), while `AddMediator(cfg => { cfg.ServiceLifetime = ServiceLifetime.Transient; })` is simply possible.
- **Fix:** treat an empty block as `settings = []` with a valid rewrite; only non-expression statements Manual.

### M5. `UnsupportedPattern` only looks at directly implemented interfaces

- **Where:** `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs:439`
- **Problem:** `class AuditPre : BasePre<Foo>` with `abstract class BasePre<T> : IRequestPreProcessor<T>` only reports the base class; derived generic messages (`class Query<T> : BaseQuery`, `BaseQuery : IRequest`) are not reported at all, while the generator breaks on them.
- **Why:** a generic message that is not reported as Manual gives a build error in `Mediator.g.cs` after the fix, without explanation. (If TAAK-080 solves this for `FindHandleMethods`: include the same fix here.)
- **Fix:** use `symbol.AllInterfaces` for the `s_noGeneric` check; the advice check may stay on `AllInterfaces` but only report on the type that declares the interface itself. Proof with a derived generic query.

### M6. Case-sensitive path comparison in `XmlProjectFileStore`

- **Where:** `XmlProjectFileStore.cs:38-39, 41`
- **Problem:** `files` and `Keeps` use `Ordinal`. On Windows, `ProjectPath` (from MSBuild) and the `FindUp` result can differ in casing (`src\App` vs `SRC\app`): the same file is loaded twice, two changes with the same `OldText`; the second `SaveAsync` sees the first write and throws "changed after it was read" — after a partial write (see H1). `Keeps` can moreover miss a shared props file and remove the package while a project still needs it.
- **Fix:** normalise with `Path.GetFullPath` and `StringComparer.OrdinalIgnoreCase` on Windows (or `OperatingSystem.IsWindows() ? OrdinalIgnoreCase : Ordinal`) in a helper.

### M7. `<Using Include="MediatR" Alias="..."/>` and `Static` stay while the package goes

- **Where:** `XmlProjectFileStore.cs:10` (shortcut), `:160`
- **Problem:** the shortcut is documented, but the consequence is not "left alone" but a build error: the package **is** removed. No Manual finding is produced.
- **Fix:** now: when such a `<Using>` is found, let `EditAsync` signal it (exception → already becomes Manual in `Fixer.cs:149`) or treat the project as `KeepRemovedPackage`. Adjust the shortcut comment.

### M8. Git process keeps running on cancellation

- **Where:** `GitSourceControl.cs:33-38`; same pattern in `SampleShopTests.cs:194-198`
- **Problem:** `WaitForExitAsync(ct)` throws on cancel, but the process is not killed; `using` only disposes the handle. With `dotnet restore` in the integration test, child processes (and MSBuild nodes) keep hanging on timeout → flaky follow-up runs.
- **Fix:** `catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw; }`. Process starting is now duplicated three times (GitSourceControl, `DotnetAsync`, `GitInitAsync`); a test helper `ProcessRunner` in `Tests/Helpers` for the two test copies.

## Low

### L1. `MsBuildSolutionStore` uses `System.IO.File` directly

- **Where:** `MsBuildSolutionStore.cs:16`
- **Problem:** all other DataAccess classes get `IFileSystem` injected; this one does not. Inconsistent, and not testable without a real file.
- **Fix:** `IFileSystem` via the constructor (classic constructor, like the report writers).

### L2. `Cli.RunAsync` always gets `CancellationToken.None`

- **Where:** `src/Unshackle.Interface/Program.cs:3`
- **Problem:** works because System.CommandLine wires Ctrl+C itself, but that is implicit. Fine, only worth a comment. No action needed except a comment.

### L3. Explicit-interface check and `CommaList` duplicated across MediatR patterns

- **Where:** `PipelineBehaviorPattern.cs:275, 336`, `VoidRequestHandlerPattern.cs:476`, (Request/NotificationHandler likewise)
- **Fix:** `CommaList` to `MediatRSyntax`; the `ExplicitInterfaceSpecifier` Manual message as a shared helper. Only pick up after TAAK-060/063/080/081 to avoid merge conflicts.

### L4. `SendAsTaskPattern` misses `_mediator?.Send(x)`

- **Where:** `SendAsTaskPattern.cs:565`
- **Problem:** with null-conditional, `i.Expression` is a `MemberBindingExpressionSyntax`; no finding, while the type becomes `ValueTask?`-like and `Task` usage breaks without a message.
- **Fix:** recognise `MemberBindingExpressionSyntax` and report Manual (`.AsTask()` on a nullable ValueTask is not trivial).

### L5. `ProjectFile` searches the closing tag textually

- **Where:** `src/Unshackle.DataAccess/Projects/ProjectFile.cs:29-30`
- **Problem:** `LastIndexOf("</Project")` also matches a comment after the root (`<!-- </Project> -->`); then `_suffix` is cut wrongly and the file gets corrupted on write. `\r`-only line endings also break the line calculation.
- **Fix:** determine the closing position via `XmlReader` (last `EndElement` at depth 0) or a round-trip check: `_prefix + root + _suffix` with an unchanged document must yield `OldText`, otherwise throw (becomes Manual).

### L6. A Manual proof with an empty `.manual.txt` proves nothing

- **Where:** `src/Unshackle.Tests/ProofTests.cs:77-78`
- **Problem:** `Reason.Contains("")` is always true; an empty or accidentally emptied file then only tests "is Manual".
- **Fix:** `reason.ShouldNotBeNullOrWhiteSpace("manual.txt must name the reason")` before the assert.

### L7. `ProofTests` fails unclearly on a typo in the folder name

- **Where:** `ProofTests.cs:53-54`
- **Problem:** `s_fixes[parts[0]]` → `KeyNotFoundException`, `.Single(...)` → "Sequence contains no matching element"; no hint which folder.
- **Fix:** `ShouldContainKey` / `SingleOrDefault(...).ShouldNotBeNull($"no pattern {parts[1]}Pattern for proof {proof}")`.

### L8. `ConsoleText.Sanitize` lets bidi and zero-width characters through

- **Where:** `src/Unshackle.Interface/Output/ConsoleText.cs:12`
- **Problem:** `char.IsControl` does not cover U+202E (RLO), U+2066-2069 and U+200B (category `Format`). A diff can thus show different code than what is written ("Trojan Source").
- **Fix:** also replace `char.GetUnicodeCategory(c) == UnicodeCategory.Format`. Add a test next to `Sanitize_EscapeSequence_...`.

## Fix first

1. **H1 + H2** together: all staleness checks (source and project files) before the first write; test that nothing is written for a stale file.
2. **H3**: do not interpret git errors as "clean".
3. **M1**: the report must not silently overwrite an existing file.
4. **H4**: integration test for `--fix mediatr` on SampleShop with build + test.
5. **M2**: escape Markdown cells correctly (backslash + HTML).

## Status (handled 2026-10-11)

| Finding | Status |
| ------- | ------ |
| H1 + H2 | **Fixed.** `ISolutionStore.SaveAsync` and `MsBuildSolutionStore.SaveAsync` removed (no more `TryApplyChanges`). `FixCommand` turns every changed document into a `ProjectFileChange` and passes source + project files in one `IProjectFileStore.SaveAsync`: first check all files (old text, strict UTF-8), then write via temp + move, on an error restore the files already written. Tests: `SaveAsync_SecondFileChanged_WritesNothing`, `SaveAsync_SecondWriteFails_RestoresTheFirst`, `SaveAsync_NotUtf8_ThrowsAndKeepsFile`, `FixExecuteAsync_ProjectFileChanges_SavesThemWithTheSourceInOneCall`. `Fixer.cs` not touched; but `Core/Abstractions/ISolutionStore.cs` (method removed), `IProjectFileStore.cs` and `ISourceControl.cs` (doc only). |
| H3 | **Fixed.** `GitSourceControl` throws `InvalidOperationException`: "not in a git repository" / "git could not check…" / git missing; `FixCommand` refuses with the advice `--force`. Tests: `NotARepository_ThrowsNotARepository`, `BrokenIndex_ThrowsGitError`, `FixExecuteAsync_SourceControlCannotTell_RefusesWithoutLoading`. Integration tests in temp folders now use `--force`. |
| H4 | **Fixed.** `SampleShopTests.FixMediatRTwice_SampleShopCopy_FirstChangesNothingThenMigratesAndBuildsAndTests`: the first fix leaves everything alone (generic `CountQuery` Manual, all-or-nothing), build + test green; after fixing `CountQuery` by hand the second fix migrates everything, no `MediatR` left in project/props, build + 38 tests green. |
| M1 | **Fixed.** `--report` refuses an existing file without `--force`; `scan` also got `--force`. Tests: `ScanExecuteAsync_ReportExists_RefusesWithoutForce`, `ScanExecuteAsync_ReportExistsWithForce_Overwrites`. |
| M2 | **Fixed.** HTML encode + escaping of `\`, `\|`, backtick, line endings. Test: `MarkdownWriteAsync_HtmlBackticksAndTrailingBackslash_EscapesEveryCell`. |
| M3, M4, M5 | **Fixed.** M3/M4 in `AddMediatRPattern` (proofs `processors`, `empty`); M5 in `UnsupportedPattern` (proof `derived-generic-message`). |
| M6 | **Fixed.** `GetFullPath` + `OrdinalIgnoreCase` on Windows/macOS in `XmlProjectFileStore`. No test (platform-dependent; CI is Linux). |
| M7 | **Fixed.** `XmlProjectFileStore` throws `InvalidDataException` on `<Using Include=…>` with `Alias`/`Static` of a removed namespace (→ Manual, and through all-or-nothing no half work). Test: `EditAsync_AliasOrStaticUsingOfRemovedNamespace_Throws`. |
| M8 | **Fixed.** `DataAccess/Processes/ProcessRunner` kills the process tree on cancellation; used by `GitSourceControl` and the two test copies. |
| L1 | **Fixed.** `MsBuildSolutionStore(IFileSystem)`. |
| L2 | **Fixed** (comment in `Program.cs`). |
| L3, L4 | **Fixed.** L3: `CommaList`, `RejectExplicit`, `WithConstraint` in `MediatRSyntax`. L4: `?.Send` → Manual (proof `null-conditional`). |
| L5 | **Fixed.** Round-trip check in `ProjectFile`; otherwise `InvalidDataException` (→ Manual). Test: `EditAsync_ClosingTagInTrailingComment_ThrowsInsteadOfCorrupting`. |
| L6, L7 | **Fixed** in `ProofTests`. |
| L8 | **Fixed.** Also `UnicodeCategory.Format`. Test: `Sanitize_BidiAndZeroWidth_ReplacesFormatCharacters`. |
| B-extra (not restored) | **Fixed.** An unrestored solution gives no WorkspaceFailed error (verified). `MsBuildSolutionStore.LoadAsync` therefore compiles every project and throws `SolutionLoadException` on CS0246/CS0234 with the advice `dotnet restore "<path>"`. Chosen over restoring automatically: no network/side effects without asking, and it also catches other non-binding code. Test: `SampleShopTests.Scan_NotRestored_FailsWithRestoreAdvice`. |
