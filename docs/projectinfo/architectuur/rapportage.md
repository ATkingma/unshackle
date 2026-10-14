# Reporting

Each format is its own `IReportWriter` in DataAccess (`CsvReportWriter`, `JsonReportWriter`,
`MarkdownReportWriter`). Interface picks one based on the extension of `--report`. An existing file is only overwritten with `--force`.
Markdown cells are HTML-encoded and `\`, `|`, `` ` `` and line breaks are escaped (reasons quote user code).

```csv
File,Line,Fix,Pattern,Status,Reason
tests/UserTests.cs,14,fluentassertions,fa.should-be,Fixed,
tests/OrderTests.cs,8,fluentassertions,fa.have-count,Manual,Dictionary: controleer of Count of Keys bedoeld is
```

Built in TAAK-005 (`src/Unshackle.DataAccess/Reports/`):

- `IReportWriter` (Core) has an `Extension` (`.csv`, `.json`, `.md`); `ScanCommand` picks the writer with the same
  extension (case-insensitive). Unknown extension → exit code 2, before loading.
- `ReportRow.From(Finding)` builds the columns; `Reason` is empty for `Fixed`.
- Paths in the report are relative to the solution folder, with `/` as separator (same on every OS).
- CSV via CsvHelper (quotes where needed), JSON via `System.Text.Json` (array of objects with the same names),
  Markdown as a table (`|` escaped). All writers write through `IFileSystem`, so they are testable with `MockFileSystem`.
- If the report cannot be written (folder does not exist, no permissions) → exit code 1.

## Own assertion extensions grouped (TAAK-091)

- `fa.custom-assertion` gives, per call of an own extension on an FA assertions type, a Manual with the same reason
  per extension ("Custom assertion `ResultAssertions.BeFailure` (ResultAssertions.cs): rewrite … once; its calls can stay").
- `ReportRow.FromAll` (all three formats) turns those into one row per extension: the first call, with the reason
  prefixed by "N call(s), first here.". Other findings stay one row per finding. Consequence: whoever counts Manual rows
  counts these extensions once.
- The console (`SpectreConsoleView.ShowSummary`) shows one advice heading below the table and `Nx reason` per extension, descending.
- Grouping happens on the exact reason via `CustomAssertionPattern.Group`; the "Not rewritten: …" Manuals of
  AllOrNothing fixes (MediatR) have a different pattern id and stay per finding.

## Project file cannot be edited (TAAK-111)

- If `XmlProjectFileStore` cannot edit a project file (not UTF-8, not valid XML, `<Using>` with Alias/Static),
  there is a Manual `project-file` per project ("Could not edit the project files: …") and all Fixed findings
  in the `.cs` files of those projects become Manual with "Not rewritten: Could not edit the project files: …". Those files
  stay unchanged.

## Source file not UTF-8 (TAAK-167)

- A `.cs` file that Roslyn did not read as UTF-8 (Latin-1, UTF-16: `SourceText.Encoding` is not a `UTF8Encoding`) is not touched by the
  `Fixer`: writing it back as UTF-8 would corrupt characters. It stays byte-for-byte identical.
- If that file mentions the old library, there is a Manual with pattern id `encoding` (line 0, "The file is not UTF-8 ...").
  The file therefore counts as remaining usage: the project keeps the old package (project-file cascade) and the rest of the
  solution is migrated. A non-UTF-8 file without the old library gets no row.
- Evidence: `FixerProjectFileTests.FixAsync_FileNotUtf8_*` and `SampleShopTests.Fix_FileInLatin1_LeavesItAndMigratesTheRest`.

## See also

- `docs/projectinfo/README.md` — overview and C4 diagrams
- `docs/projectinfo/features/end-to-end-keten-plan.md` — plan of the chain (REQ-3)
