# Core: Fixer → Fix → Pattern

```
Fixer                       the single entry point: FixAsync(solution, request)
 └─ IFix                    one per library (FluentAssertions, MediatR, AutoMapper)
     └─ IPattern            find one kind of code + rewrite it
          ├─ Find(...)      → locations in the code
          └─ Rewrite(...)   → Fixed(new node) or Manual(reason)
```

```
src/Unshackle.Core/
├─ Fixer.cs            FixAsync, RunAsync (the pass loop over files), project files
├─ PartialRun.cs       RunPartialAsync: the partial run of an all-or-nothing fix (TAAK-274)
├─ FileFixer.cs        one file in, one FilePass out: patterns, usings, targeted imports, replace
├─ SolutionChecks.cs   SwapGlobalUsingsAsync, UnreachableFilesAsync, NotUtf8FilesAsync
├─ FindingFactory.cs   CreateFinding, NotApplied, InternalError
├─ Abstractions/   IFix, IPattern, IFixer, ISolutionStore, IReportWriter
├─ Models/         Finding, PatternResult, FixRequest, FixReport
└─ Fixes/
   ├─ FluentAssertions/   FluentAssertionsFix.cs + Patterns/
   ├─ MediatR/            MediatRFix.cs + Patterns/
   └─ AutoMapper/         AutoMapperFix.cs + Patterns/
```

Every fix has the same layout. Know one and you know them all.

**Deviating patterns** (for example `HaveCount`, where the type decides the right replacement) are handled inside that
pattern. If it grows too large, split it into several patterns, such as `HaveCountOnListPattern` and
`HaveCountOnEnumerablePattern`. The Fixer does not notice.

**A pattern never crashes.** If a rewrite does not work, it returns `Manual(reason)` and that ends up in the report.

**Dry-run costs nothing extra.** Roslyn never modifies a `Solution`. The Fixer computes a new `Solution` and the report.
With `--dry-run` we only show the diff; otherwise `ISolutionStore` writes the changes.

## As built (TAAK-001)

- `IPattern`: `Id`, `Description`, `Find(SyntaxNode root, SemanticModel model)` → nodes,
  `Rewrite(SyntaxNode node, SemanticModel model)` → `PatternResult`. The pattern itself chooses which node it replaces
  (an expression, the outermost call of a chain, or a whole statement).
- `PatternResult.Fixed(node(s), params requiredNamespaces)` or `PatternResult.Manual(reason)`.
- `IFix`: `Id`, `Patterns`, `RemovedNamespaces`, `AddedNamespaces`.
- `Fixer` (per file via `FileFixer.FixFileAsync`): per document the patterns one after another (fresh `SemanticModel`), replaced with `TrackNodes`/`ReplaceNode`
  (outermost first, overlap → Manual), then usings via `UsingDirectives`. Exceptions per pattern → Manual.
  Every file is fixed against the **original** solution (only that file changes in its working copy): the semantics
  of other files therefore do not depend on the order (TAAK-130: a marker interface in an earlier file had already been
  swapped, so a handler in a later file was silently missed). The final text then goes into the collected solution.
  Once per file path: a multi-target project (one Roslyn project per TFM) is first fixed in the first TFM;
  if the file has `#if` code that is active in another TFM (`#if !NETFRAMEWORK`), then for each differing set of
  inactive code one extra pass follows on the text so far, which only fixes nodes that were inactive in **all** earlier
  passes (`FileFixer.NewCode`), so shared code is fixed and reported once. The usings row "inactive #if branch" of an
  earlier pass is dropped when a later pass updates the usings (TAAK-223). All copies get the final text. Project
  migrations also count per project file (the package stays when one TFM still needs it); `fix` counts written files
  uniquely (TAAK-022).
- Details and reasons: `claude/tasks/archief/TAAK-001-fundament.md`.

## FluentAssertions patterns: shared base (REVIEW-A)

- `FluentAssertionPattern` (Template Method): `Rewrite` does the checks every `x.Should().Method(...)` shares
  (AssertionScope, subject shape, used result, becauseArgs) with one reason text, then `RewriteAssertion(AssertionContext)`.
  Hooks: `GetAssertion` (statement or `.Which` as node), `IsResultDiscarded`/`ResultUsedReason`, `RewriteBeforeArguments`
  (params overloads that the argument split would otherwise see as becauseArgs). The Throw family (Throw, ThrowAsync,
  ThrowExactly, NotThrow, Invoking) has a different node shape and shares `ThrowSyntax` and `ThrowMessage` instead of the base.
- Shared helpers in `FluentAssertionsSyntax`: `ConvertExpected`, `IsSafeReceiver`/`NotNullReceiver`, `IsSafeToEvaluateTwice`
  (one symbol-based rule), `TryGetSplittableStatement`/`NotSplittableReason`, `Arguments`/`Invoke`, `WithWhich`/`WithStatement`.
  Patterns no longer call statics of other patterns.
- `.And` chains belong only to `AndChainPattern`; it passes on warnings of links and accepts links spanning several statements.
- `UsingDirectives` also swaps usings inside a namespace declaration (block or file-scoped). New usings go inside the
  namespace if the file has them there (IDE0065), unless an own namespace shadows the new one; and only in a file
  that names the old library (IDE0005, TAAK-115).
- `IFix.RemovedExtensionMethods` (FA: `Should`): an `x.Should()` that binds to nothing after the fix (unknown receiver, e.g.
  after a rewritten lambda argument) keeps the old using and the package and gives a Manual finding `usings` (TAAK-110).
- `IFix.AddedReferences` (FA: Shouldly, netstandard2.0 build from `Interface/References/`, supplied by the composition root):
  the `Fixer` adds them, plus its own document `Unshackle.AddedUsings.g.cs` with `global using` of the `AddedNamespaces`, to
  the working copy the patterns run on, not to the result. That way an already rewritten `ctx.ShouldBe(..)` in a
  lambda argument binds, overload resolution of the surrounding call succeeds, and a later pattern recognises the outer
  assertion in the same run (TAAK-116). `UsingDirectives` does not count that document as an import of the file.

## Outward contracts (TAAK-005)

- `ISolutionStore`: `LoadAsync(path, ct)` → `Solution` (throws `FileNotFoundException` or `SolutionLoadException` with
  the MSBuild errors, or when types do not resolve (CS0246/CS0234: not restored → advice `dotnet restore`). Load only:
  `FixCommand` writes the changed `.cs` files together with the project files via `IProjectFileStore.SaveAsync`
  (REVIEW-B H1/H2): first **all** files are checked (unchanged since loading, UTF-8), only then written
  (temp file + move), and on an error halfway the files already written are restored. Implementation: `MsBuildSolutionStore`.
  A file that is not valid UTF-8 gets its text as Latin-1 on every platform (Roslyn falls back to UTF-8 on Linux/macOS),
  so the store marks it as Latin-1 on every platform and `SaveAsync` writes it back byte-exact (TAAK-167, TAAK-285).
- `IReportWriter`: `Extension` + `WriteAsync(path, findings, ct)`. See `rapportage.md`.
- `ISourceControl`: `HasUncommittedChangesAsync(directory, ct)`; `fix` refuses on a dirty working tree unless `--force`.
  No git repository, no git, or a git error gives `InvalidOperationException` (never "clean"); `fix` then also refuses
  without `--force`. Processes start via `DataAccess/Processes/ProcessRunner` (kills the process tree on cancellation).
- `--report` refuses to overwrite an existing file without `--force` (`scan` and `fix`).
- The Fixer itself did not change: Interface loads, calls `FixAsync` and lets DataAccess write.

## Loading despite NuGet warnings and missing projects (TAAK-090)

- `MsBuildSolutionStore` passes global properties to MSBuild for the design-time build only: `NuGetAudit=false` and
  `WarningsNotAsErrors=NU1701;NU1900–NU1905`. User files stay unchanged.
- Roslyn reports every MSBuild message of a project as `Failure`, also an ordinary warning (NU1903, NU1701). Such a failure
  is only a warning (`ISolutionStore.Warnings`, shown by `scan`/`fix` as `Warning: …`) if the project was loaded anyway,
  or if the named project file does not exist. Any other failure stays fatal.
- Why a missing project is safe: nothing of it is on disk to rewrite. If a loaded project depends on it, its types do
  not resolve and the existing CS0246/CS0234 check still fails (with the warnings included in the error message).
  A project that does exist but does not load stays fatal: the fix would skip its code and still change shared package files.
- Corpus (TAAK-090): Polly, EventFlow, SmartEnum and Bogus now load. Specification does not: the core project lies outside the
  tarball and the other projects refer to it (CS0246). EventFlow's own `dotnet restore` fails on NU1902/NU1903 because of
  TreatWarningsAsErrors; that is a user build (`-p:NuGetAudit=false`), not something of the loader.

## Own assertion extensions (TAAK-091)

- `fa.custom-assertion` (`Patterns/CustomAssertionPattern.cs`, new file; one line in `FluentAssertionsFix.Patterns`)
  recognises a non-FA method on an FA assertions type (receiver or base type in a `FluentAssertions` namespace,
  name ends in `Assertions`) and gives Manual with one reason per extension. See `rapportage.md` for the grouping.

## Project files (TAAK-017)

- `IFix.Package` (default `null`): `PackageMigration` (old package = assembly name, `AddedPackages`: name + version; the first replaces the old one in place, the rest go next to it). FA → Shouldly 4.3.0; MediatR → Mediator.Abstractions + Mediator.SourceGenerator 3.0.2.
- `Fixer` creates a `ProjectMigration` per project that references the old assembly; `KeepRemovedPackage` if a `.cs` file
  still uses the package semantically after the fix (`ProjectPackages`). All projects go into one `IProjectFileStore.EditAsync`,
  because `Directory.*.props` is shared.
- Report: Fixed per changed project file, Manual per project that keeps the package or on a read/XML error (Line 0,
  pattern `project-file`). `FixReport.ProjectFileChanges` holds the new text; `FixCommand` shows the diff or passes them with the source code to `SaveAsync`.
- Implementation: `XmlProjectFileStore` (DataAccess, `XDocument` + `IFileSystem`), keeps whitespace, CRLF and BOM and refuses to
  write if the file changed in the meantime. See `docs/projectinfo/features/projectbestanden-migreren-plan.md`.
- Textual splice (TAAK-134/111): on load, `ProjectFile` walks the original text alongside the `XDocument` and remembers per
  node where it is. On write, every unchanged node comes literally from the original text; only changed
  attribute values are replaced in place, new elements are written on one line and removed ones are cut out (with their
  indentation). That keeps `/>`, `" >`, `->` in attributes, attributes over several lines, comments, BOM and (mixed)
  line endings byte-identical; the reader does not normalise line endings, so copied indentation keeps the neighbour's line ending.
  A file without a change (often `Directory.Build.props`) yields no change.
- Safety rule (TAAK-111): if `EditAsync` fails anyway, the Fixer resets all `.cs` files of the projects involved
  to the original text and their Fixed findings become Manual ("Not rewritten: Could not edit the project files: …"). Never
  rewritten code without the new package.
- Non-migrated projects on disk (TAAK-118): a shared `Directory.Build.props`/`Directory.Packages.props` only loses the
  old package if no `.csproj` under its folder (with this file as the nearest) still uses it via `PackageReference`,
  including a project that was skipped on load (TAAK-112/135). Otherwise the line stays, the new package goes next to it
  and `XmlProjectFileStore` sets `ProjectFileChange.KeptRemovedPackageFor`; the Fixer reports that file as Manual.
- `Directory.Packages.props` without central package management (TAAK-210, kgrzybek: no `<PackageVersion>`, no
  `ManagePackageVersionsCentrally`, but `<PackageReference Version>` in ItemGroups with a `Condition`) is just an imported
  props file: it belongs to the project's chain (references are swapped there in place) and no `<PackageVersion>` is added.
  An extra package (e.g. the source generator) then does not go next to a reference in a conditioned group, but into the project itself.

## Same-named types (TAAK-024)

- `IFix.SwappedTypes` (default empty): types such as `MediatR.IRequest\`1` with a twin in the new namespace. Symbols of
  those types and their members do not count as usage in `UsingDirectives.UsedNamespaces` and `ProjectPackages`.
- A file without a pattern change still gets the using swap if only swapped types need the old using.
- `PackageMigration.AlsoRemovedPackages` (MediatR: `MediatR.Contracts`) is removed as well. See
  `docs/projectinfo/features/gelijknamige-typen-swap-plan.md`.

## Inactive `#if` code and global usings (TAAK-043, TAAK-044)

- Code in an inactive `#if` branch is `DisabledTextTrivia`: patterns do not see it and nothing binds to it. If a changed
  file has such trivia, the old using stays (the new one is added) and the Fixer reports Manual (pattern `usings`,
  line of the inactive code). `ProjectPackages` then also keeps the package. Chosen over parsing per symbol set: which
  symbols exist (`DEBUG`, own ones) cannot be known, and enabling everything at once does not parse `#else` branches.
- `global using <old>;` in an own file (not `*.g.cs`; `<Using Include>` is handled by `XmlProjectFileStore`) applies to the
  whole project. Files in such a project do not get their own `using <new>;`. After all files,
  `SolutionChecks.SwapGlobalUsingsAsync` decides per project file (all TFMs): if no file binds to the old namespace any more and there is no
  inactive code, it becomes `global using <new>;` in place; otherwise it stays with the new one next to it + Manual `usings`.
  A remaining global using keeps the package in `ProjectPackages`.

## Removing old usings safely (TAAK-281, TAAK-283)

- All FluentAssertions namespaces are removed namespaces, sub-namespaces too (`FluentAssertions.Common`): unused ones go, used ones keep all.
- An unused using with `#if`/`#else` trivia is removed with `KeepLeadingTrivia`, so the directive pairs stay whole.
- `FileFixer.Replace` gives Manual when a rewrite would drop directives inside the node; `SolutionChecks.UnreachableFilesAsync` skips files that a
  project without the old and the new library also compiles. See `docs/projectinfo/features/using-removal-edge-cases-plan.md`.

## Ambiguous through a new namespace (TAAK-174)

- After the using step, the Fixer binds the updated file again. If a call gives CS0121 between a method from an
  added namespace and an existing one (Akka.TestKit's own `ShouldBe` next to Shouldly's), the file stays as it was:
  the Fixed findings of that file become Manual ("Not rewritten: …") plus one Manual `usings` with both methods.
  The file thereby keeps the old library and the project the old package (project-file cascade). `UsingDirectives.AmbiguityWithAddedAsync`.
- Fully qualified Shouldly calls were rejected (unreadable). A global using that causes the conflict
  (`SwapGlobalUsingsAsync`) is not checked.
- Without ambiguity a rewritten call can silently bind to something else: an own extension in the enclosing
  namespace wins over `using Shouldly;` (file in `namespace Akka.TestKit`, TAAK-199). If a call with the name of an
  extension from an added namespace binds elsewhere, and the original had fewer of them, the same fallback follows with reason
  "binds to <type>.<method> instead of Shouldly". `UsingDirectives.MisboundAddedCallAsync`.
- **Targeted imports (TAAK-198)**: before that fallback, the Fixer tries to migrate the rest of the file anyway
  (`FileFixer.FixWithTargetedImportsAsync`). The file then gets no `using Shouldly;`, but a `using static` for every Shouldly class
  whose extensions it calls (`using static Shouldly.ShouldBeBooleanExtensions;`) and an alias for every named type
  (`using Should = Shouldly.Should;`). A class that makes a call ambiguous is left out; a rewrite that would call a clashing
  name (or a name from a left-out class) becomes Manual per call ("Not rewritten: … is ambiguous with … (CS0121) …")
  and stays FA. Every round runs the patterns again with the blocked names and checks the result without the global
  model using (`ConflictsWithAddedAsync`, plus: no more compile errors than the original). If nothing grows any more or another
  error appears (e.g. an own type `Should` clashing with the alias), the per-file fallback above follows.

## Partial migration of an all-or-nothing fix (TAAK-274)

- An `AllOrNothing` fix (MediatR) with Manual findings that also implements `IPartialFix` gets a second chance: the Fixer passes
  the original solution and the root findings (Manual, minus "Not rewritten: …" and the rows that only say the package or a
  global using stays; an `unmatched-use` row gets its line in the original file) to `PlanPartialAsync`. The fix returns a
  `PartialPlan`: a solution in which the code that stays is annotated (`PartialPlan.KeptAnnotationKind`), plus the rows that
  report it. The Fixer runs again on that solution; patterns never see an annotated node.
- A partial run is accepted when it has no Manual finding besides the plan rows and the "package stays" project-file rows,
  and every kept node is still in the fixed file with the same text. A new Manual finding becomes an extra root and the fix
  plans again (at most 4 rounds); otherwise the all-or-nothing result stands.
- MediatR's plan (`Fixes/MediatR/MediatROwners.cs`, union-find in `MediatROwnerGraph.cs`): see
  [features/mediatr-partial-migration-plan.md](../features/mediatr-partial-migration-plan.md). Kept references become
  `global::MediatR.X`; such a name needs no using (`UsingDirectives.IsGlobalQualified`) and keeps the package in its project
  even for a swapped type (`ProjectPackages`).

## Report-only fixes (TAAK-316)

Council round 8, S2. A fix that also implements `IReportOnlyFix` (one member, `ReportOnlyReason`) only takes inventory.
`Fixer.FixAsync` hands it to `ReportOnlyRun` before anything else: every pattern runs `Find` + `Rewrite` on the first copy of
each candidate file (same pre-filter as a normal run), and the solution, usings and project files stay as they are
(`FixReport.ProjectFileChanges` is empty, `IProjectFileStore` is never called, `FixReport.InventoryOnly` is true).

- Patterns return `Manual` or `PatternResult.Note(text)` (`InfoResult`, neither Fixed nor Manual). A `Fixed` result is
  reported as `Manual` with the `Not rewritten: ` prefix and not applied, so a report-only run never counts anything as Fixed.
- The first row is the header: pattern id `Fixer.InventoryOnlyPatternId` (`inventory-only`), line 0, the solution path, an
  Info row "Inventory only: ... " plus the fix's reason. CSV and JSON show it as a row with status `Info`; Markdown also
  writes `**Inventory only**` above the table.
- An ordinary fix may return `Note` too: the node stays as it is and the row has status `Info`.

## Project-file migrations (TAAK-317)

Council round 8, S3. A fix that also implements `IProjectFileMigration` (`EditsFor(Project)`, a Strategy) declares exact typed
edits per project from a closed set (`ProjectFileEdit` has a private-protected constructor):

| Edit | What it does | Manual when |
| --- | --- | --- |
| `PackageRenameEdit` | renames the project's `PackageReference` in place with the new version; under CPM adds a `PackageVersion` (the old one stays) | declared only in a shared `Directory.*.props`; no version and no CPM |
| `PackageRemoveEdit` | removes the project's `PackageReference` | declared only in a shared file |
| `PackageAddEdit` | adds a `PackageReference` after the last one (plus `PackageVersion` under CPM) | — (skipped when already declared) |
| `PropertyEdit` | sets one MSBuild property, added to the last unconditional `PropertyGroup` when missing | set twice or under a condition |
| `ConfigFileRenameEdit` | renames a file next to the project, and items that name it exactly (`Include`/`Update`/`Remove`) | the new name exists |
| `ConfigFileTransformEdit` | exact text replacements in a file next to the project | an old text does not occur exactly once |

- The Fixer calls `ProjectFileMigrations.ApplyAsync` at the end of a run, after the document pass, vendoring and the package
  migration, and only when the `IProjectFileStore` also implements `IProjectFileEditor` (`XmlProjectFileStore` does).
- A project whose documents have a blocking Manual row (`Kept` = None) gets no edits and one Manual `project-file` row.
- Each edit with something to do is one `project-file` row on its file:line: Fixed with `Info` = `Describe()`, or Manual with
  the reason. `OutputType` and `IsTestProject` edits always carry a `Warning`.
- The editor starts from earlier changes of the same run (package migration), keeps their `OldText`, and the Fixer replaces
  that change. A renamed file is a `Deletes` change plus a `Creates` change; `SaveAsync` deletes and restores on failure.
- An all-or-nothing fix that ends up changing nothing drops these changes like any other.

## Council round 10 engine rules (TAAK-318)

- **C1 reason codes.** `ManualResult.Code` (a string) is separate from `Kept`. A fix keeps its own enum and calls
  `PatternResult.Manual(code, reason)`. `KeptReasonText.Rendered` prints `<kept prefix><Code>: <reason>`, which is the text the fixes
  used to write by hand, so the rendered output stays byte-identical.
- **C2 fix-level Info.** `IFix.ProjectInfo(Project)` (default empty) gives Info rows on the project file (`project-file` pattern id),
  once per project file, at the end of a run and of a report-only run.
- **C3 blank first line.** When a removed using has no leading trivia at position 0, `UsingDirectives.WithoutBlankFirstLine` removes the one
  blank line it leaves on top (whitespace plus one line break). Any other leading trivia stays, and a second run changes nothing.
- **G1 header kind.** The report-only header is an `InfoResult` with `Kind = InfoKind.Header`. `FixReport.CountedFindings` /
  `FixReport.IsCounted` leave it out of every count.
- **G4 what blocks.** A project's file edits are blocked by any blocking document row, and by a project-level Manual row whose `Code` is a
  `ProjectBlockingReason` (`TargetFrameworkUnsupported`, `SharedDeclarationBlocked`, `LicensedDependency`). Fixes add such rows through
  `IProjectFileMigration.ProjectBlockers(Project)`. An outcome with a blocking code turns the project's other edits into Manual rows, and
  none of its changes are kept.
- **G3 shared declarations.** A package declared only in a shared `Directory.*.props` is edited there when every `.csproj` that imports it
  is loaded and unblocked (`ProjectFileEditContext`). Otherwise the outcome is Manual `SharedDeclarationBlocked`, naming that project.
- **G2 old central version.** After a rename or remove under CPM, the old `<PackageVersion>` goes only when it is no `GlobalPackageReference`,
  every importer of that `Directory.Packages.props` is loaded, and no importer (or its `Directory.Build.props`) still references it.
  Otherwise it stays, with a `Note` added to the row's Info.
- **G5 config transforms.** After the literal replacements, a `.json` result must parse (comments and trailing commas allowed), and so must an
  `.xml/.config/.props/.targets` result. Otherwise the transform is Manual.
- **P8 conditional remove.** `PackageRemoveEdit.AtMostVersion` removes the reference only when its declared version (its own, else the
  central one) is a plain version no higher than that. Otherwise the outcome has `KeptInfo`, and the reference stays with an Info row.
  Microsoft.Extensions.Resilience 10.11.0 brings Polly.Core 8.4.2 (via Polly.Extensions 8.4.2, checked in the nuspecs).
- **R1 IsPackable.** `IProjectProperties` (DataAccess `XmlProjectProperties`) reads `IsPackable` from the nearest `Directory.Build.props`,
  then the project, then `Directory.Build.targets`; the last unconditional value wins. When it is unset, the SDK default applies:
  `Microsoft.NET.Test.Sdk` or `IsTestProject` makes a project not packable, and every other project is packable. A conditional value,
  a computed value or an unreadable file counts as packable. `MediatRFix(IProjectProperties)` uses it in `AssemblyResolver`.

## Blocked projects stay untouched (TAAK-320)

- The Fixer asks `IProjectFileMigration.ProjectBlockers` before the document pass, and the rows come first in the report.
- If a project has a row with a `ProjectBlockingReason` code, every document of that project goes back to its original text after the
  document pass. That includes a file it shares with another project. Each Fixed row in those documents becomes Manual:
  `Not rewritten: blocked by <code> on <project>. ...`.
- The package step then sees the old code, so that project keeps the old package. Fixes without blockers produce the same output as before.
- Console: a report-only run starts its summary with "Inventory only: nothing was changed.", and the header row stays out of the table and
  the counts. A dry run shows a deleted file as one line, `--- <path> (deleted)`, instead of a diff.

## Package assets (TAAK-319)

- `IPackageAssetsReader.Read(projectPath)` returns `PackageAssets` and is implemented by DataAccess `JsonPackageAssetsReader`, which reads
  `obj/project.assets.json`. The result has a `State`, the resolved versions (the highest over all target frameworks), and every
  requirement with its range, whether from the project or from a package.
- `Current` means:
  - the assets file was restored for this project file;
  - it is not older than the project file or its nearest `Directory.Build.props` / `Directory.Packages.props`;
  - it lists every `PackageReference` the project declares.

  A file that is there but fails any of these checks is `Stale`. A missing or unreadable file is `Missing`.
- `ResolvesAtLeast(package, v)` and `RequiresAtLeast(package, v)` answer EPPlus E7 (council round 10). A requirement whose range cannot be
  read counts as "at least", so the pin is not made.

## SOLID in brief

| Principle | How                                                                                                                       |
| --------- | ------------------------------------------------------------------------------------------------------------------------- |
| S         | A pattern only describes what is rewritten. Loading, writing and reporting happen elsewhere                               |
| O         | A new library or new pattern is a new class. The Fixer does not change                                                    |
| L         | Every pattern returns `Fixed` or `Manual` and never throws an exception, so the Fixer treats them all the same           |
| I         | Small interfaces: find/rewrite, solution storage and report writing are separate                                         |
| D         | Core depends on interfaces. MSBuild, the file system and CSV live in DataAccess                                           |

## See also

- `docs/projectinfo/README.md` — overview and C4 diagrams
- `claude/docs/design-patterns.md` — GoF patterns in this design
- `src/Unshackle.Core/Fixer.cs` — implementation (with `PartialRun.cs`, `FileFixer.cs`, `SolutionChecks.cs`, `FindingFactory.cs`)
