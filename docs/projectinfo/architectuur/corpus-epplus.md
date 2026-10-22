# Corpus: EPPlus 4.5–8 API usage in 42 open-source projects

Measured on 2026-10-22 for TAAK-309 step 1 (survey). Model: `docs/projectinfo/architectuur/corpus-automapper.md`.
Target library under discussion: ClosedXML (MIT).

## Method

- Candidates from GitHub code search on `"EPPlus" Version="4.5.3` / `5` / `6` / `7` in `*.csproj`,
  `id="EPPlus" version="4.5.3.3"` in `packages.config` and `PackageVersion Include="EPPlus"` (central package management);
  ranked by stars, forks and non-C# repos dropped, very large repos (>200 MB: game engines, PLC/Revit toolkits) skipped.
- 42 repos cloned shallowly (`--depth 1`) at the commit in `claude/tools/corpus-epplus-repos.txt` into
  `D:\playground\corpus-epplus\src\<name>\` (outside the repo, read only).
- Measurement: a regex script over `*.cs` files that contain `using OfficeOpenXml` or a qualified `OfficeOpenXml.` use
  (comments stripped; `bin`, `obj`, `node_modules`, `wwwroot`, `*.g.cs`, `*.Designer.cs` skipped). No Roslyn: numbers are
  indicative. `python -I D:\playground\corpus-epplus\tools\measure.py [names]` writes `tools\ep.json`; `agg.py` prints the
  table. "In tests" = hits in paths containing `test`.
- Builds: `dotnet build <solution> -m:2 -p:TreatWarningsAsErrors=false -p:NuGetAudit=false`, SDK 10.0.400, one repo at a
  time, 10 minute limit, `TEMP`/`TMP`/`NUGET_PACKAGES`/`DOTNET_CLI_HOME` on `D:` (the `C:` drive was full during the run).
  Logs: `D:\playground\corpus-epplus\logs\<name>.log`, summary `logs\_results.txt`. Builds were planned for 20 repos (the ones
  with tests or enough EPPlus code) but stopped after 5 because `C:` was full.

## Versions

| EPPlus line | Licence | Repos |
| ----------- | ------- | ----: |
| 4.1–4.5.3.3 | LGPL | 16 (2 of them also reference a 5+/8 package: `newdcms`, `officeimo`) |
| 5.x | Polyform Noncommercial / commercial | 7 |
| 6.x | Polyform Noncommercial / commercial | 7 |
| 7.x | Polyform Noncommercial / commercial | 9 |
| 8.x | Polyform Noncommercial / commercial (new `ExcelPackage.License` API) | 3 (+ `officeimo`) |

The 5+ lines differ from 4.x in ways that matter for a rewrite: `ExcelPackage.LicenseContext` (5–7) or
`ExcelPackage.License.SetNonCommercial*(..)` (8) must be set, and **`Worksheets[int]` is 0-based from 5.0** (1-based in
4.x unless `Compatibility.IsWorksheets1Based`). ClosedXML's `Worksheet(int)` is 1-based, so an integer index can only be
rewritten when the EPPlus version is known.

## Repos

"Tests": framework and number of test projects; "EPPlus files" = `.cs` files using EPPlus (of which in tests).

Builds were stopped after 5 repos because drive `C:` ran out of space (coordinator request); every other repo is
"not built (C: full)" and should be built in a later round. Build results: `excelchop`, `bookfx`, `zserver`,
`xlobjmapper` build; `dexiom` does not (MSB3644, targets .NET Framework 4.0).

| Name | Repo | EPPlus | TFM (main) | Tests | EPPlus files (tests) | Builds |
| ---- | ---- | ------ | ---------- | ----- | -------------------: | ------ |
| bookfx | `bookfx/bookfx` | 4.5.3.2 | netstandard2.0 | xUnit, 1 | 32 (5) | yes |
| dataextractor | `ipvalverde/EPPlus.DataExtractor` | 4.5.3.2 | net45/netcoreapp2.1 | xUnit, 1 | 16 (1) | not built (C: full) |
| dexiom | `Dexiom/Dexiom.EPPlusExporter` | 4.1/4.5.3.3 | net40/netstandard2.1 | MSTest, 1 | 17 (5) | no (net40 reference assemblies) |
| ebm | `xujiangjiang/Easy-Bug-Manager` | 4.5.3.3 | .NET Framework | — | 2 | not built (C: full) |
| etlast | `FizzCodeSoftware/EtLast` | 4.5.3.3 | net9.0 | MSTest, 5 | 1 | not built (C: full) |
| etlnet | `paillave/Etl.Net` | 4.5.3.3 | net9.0 | xUnit, 6 | 7 (1) | not built (C: full) |
| excelcake | `winstonwxj/ExcelCake` | 4.5.3.3 | netcoreapp3.1 | MSTest, 1 | 15 | not built (C: full) |
| griddly | `railapex/griddly` | 4.5.3 | net461/netcoreapp3.1 | MSTest, 1 | 1 | not built (C: full) |
| haohao | `alvinhao666/HaoHaoPlay_Back` | 4.5.3.3 | net5.0 | — | 2 | not built (C: full) |
| montr | `montr/montr` | 4.5.3.3 | net8.0 | NUnit, 12 | 1 | not built (C: full) |
| newdcms | `NEWDCMS/NEWDCMS` | 4.5.3.1 + 5.8.6 | net6.0 | — | 4 | not built (C: full) |
| officeimo | `EvotecIT/OfficeIMO` | 4.5.3.3 + 8.7.0 (benchmarks) | net10.0 | xUnit, 61 | 5 | not built (C: full) |
| packt8 | `PacktPublishing/ASP.NET-Core-8-and-Angular` | 4.5.3.3 | net8.0 | xUnit, 2 | 10 | not built (C: full) |
| pivotdata | `nreco/pivotdata` | 4.5.3.3 | netstandard2.0/net8.0 | xUnit, 1 | 2 | not built (C: full) |
| siocore | `Swastika-IO/sio.core` | 4.5.3.1 | netcoreapp2.2 | — | 0 (reference only) | not built (C: full) |
| zserver | `zmaplab/zserver` | 4.5.3.3 | net10.0 | xUnit, 1 | 1 (1) | yes |
| alizhou | `lizhounet/Alizhou.Office` | 5.4.0 | netstandard2.0 | — | 1 | not built (C: full) |
| dotnettools | `Bryan-Cyf/DotNetTools` | 5.1.2 | net6.0 | — | 1 | not built (C: full) |
| endurance | `Sathiyaraman-M/Endurance` | 5.8.3 | net6.0 | — | 1 | not built (C: full) |
| filecontextcore | `morrisjdev/FileContextCore` | 5.1.2 | netstandard2.0 | — | 2 | not built (C: full) |
| nopframework | `alirezakhosravi/NopFramework` | 5.7.0 | net5.0 | — | 3 | not built (C: full) |
| qpas | `qusma/QPAS` | 5.1.0/5.4.2 | netcoreapp3.1 | NUnit, 2 | 2 | not built (C: full) |
| tournament | `MatrikMoon/TournamentAssistant` | 5.8.6 | net8.0 | — | 0 (reference only) | not built (C: full) |
| closetcode | `karenpayneoregon/closet-code` | 6.0.4 | net6.0 | MSTest, 7 | 3 | not built (C: full) |
| dorisoy | `dorisoy/Dorisoy.Pan` | 6.2.9 | net7.0 | — | 3 | not built (C: full) |
| eppsamples | `EPPlusSoftware/EPPlus.Sample.NetCore` | 6.1.0 | net6.0 | — | 58 | not built (C: full) |
| excelchop | `mitchpaulus/excelchop` | 6.1.1 | net9.0 | NUnit, 1 | 2 | yes |
| geekconfig | `leeveel/GeekConfig` | 6.0.5 | netcoreapp3.1 | — | 2 | not built (C: full) |
| manga | `ivanpaulovich/clean-architecture-manga` | 6.2.6 | net7.0 | xUnit, 4 | 1 | not built (C: full) |
| xlobjmapper | `ShadyNagy/ExcelObjectMapper` | 6.2.6 | net5.0–net7.0 | — | 2 | yes |
| aistory | `AIStoryBuilders/AIStoryBuilders` | 7.3.2 | net10.0 | — | 1 | not built (C: full) |
| lazyfurs | `Hafinator/LazyFURS` | 7.0.9 | net9.0 | — | 1 | not built (C: full) |
| lesi | `Libihao520/LeSi-Platform-Api` | 7.4.1 | net8.0 | — | 2 | not built (C: full) |
| netdigger | `serptail/netDigger` | 7.2.1 | net8.0-windows | — | 0 (reference only) | not built (C: full) |
| openmirroring | `MarkPryceMaherMSFT/OpenMirroring` | 7.5.1 | net8.0 | — | 1 | not built (C: full) |
| panda | `842549829/Panda` | 7.5.3 | net9.0 | xUnit/NUnit/MSTest, 16 | 1 | not built (C: full) |
| salesapi | `cinthiabs/sales-management-api` | 7.1.3 | net8.0 | xUnit, 1 | 1 | not built (C: full; interrupted) |
| simoona | `VismaLietuva/simoona` | 7.5.3 | net10.0 | NUnit, 3 | 2 | not built (C: full) |
| yourbrand | `marinasundstrom/YourBrand` | 7.3.0 | net10.0 | xUnit, 17 | 1 | not built (C: full) |
| coreframework | `Boy-boy/CoreFramework` | 8.4.2 | net10.0 | MSTest, 1 | 3 | not built (C: full) |
| tunnetcom | `TunNetCom/TunNetCom-SilkRoadErp` | 8.6.3 | net10.0 | xUnit, 6 | 3 | not built (C: full) |
| zamin | `oroumand/Zamin` | 8.5.0 | net8.0/net10.0 | — | 1 | not built (C: full) |

## Frequencies

`eppsamples` is EPPlus's own sample app (vendor showcase, 58 files); like ABP in the AutoMapper survey it is counted but
not used for the ordering: the "Excl. eppsamples" column is the one to read. `officeimo` uses EPPlus only in
benchmarks (5 files).

| API / shape | Hits | Projects | In tests | Excl. eppsamples | Top repo |
| --- | ---: | ---: | ---: | ---: | --- |
| `using OfficeOpenXml` | 379 | 39 | 18 | 207 | eppsamples 172 |
| `new ExcelPackage(...)` | 254 | 37 | 20 | 195 | officeimo 84 |
| … `new ExcelPackage()` (empty) | 50 | 18 | 1 | 26 | eppsamples 24 |
| … `new ExcelPackage(stream)` | 121 | 13 | 0 | 121 | officeimo 79 |
| … `new ExcelPackage(new FileInfo(..))` | 15 | 9 | 1 | 14 | panda 5 |
| `ExcelPackage.LicenseContext = ...` (5–7) | 21 | 13 | 0 | 21 | xlobjmapper 6 |
| `ExcelPackage.License.Set*(..)` (8) | 12 | 2 | 0 | 12 | officeimo 8 |
| `Workbook.Worksheets.Add(..)` | 245 | 27 | 3 | 147 | eppsamples 98 |
| `Worksheets[i \| name]` / `.First()` | 187 | 26 | 39 | 141 | eppsamples 46 |
| `Cells[r, c]` | 1043 | 33 | 81 | 879 | officeimo 282 |
| `Cells[r1, c1, r2, c2]` (range) | 236 | 15 | 6 | 140 | eppsamples 96 |
| `Cells["A1"]` / `Cells[address]` | 514 | 9 | 20 | 49 | eppsamples 465 |
| `.Value` on a cell/range | 790 | 29 | 33 | 544 | eppsamples 246 |
| `.Text` | 80 | 11 | 34 | 70 | dexiom 34 |
| `.GetValue<T>()` / `GetCellValue<T>` | 146 | 7 | 0 | 131 | packt8 70 |
| `.Formula` / `.FormulaR1C1` | 63 | 4 | 3 | 18 | eppsamples 45 |
| `Calculate()` | 21 | 1 | 0 | 0 | eppsamples 21 |
| `LoadFromCollection` | 19 | 5 | 0 | 6 | eppsamples 13 |
| `LoadFromDataTable` | 18 | 7 | 0 | 14 | officeimo 7 |
| `LoadFromArrays` / `LoadFromText` / `ToDataTable` | 1 / 2 / 6 | 1 | 0 | 0 | eppsamples |
| `SaveAs(..)` | 76 | 12 | 1 | 53 | officeimo 40 |
| `Save()` | 93 | 12 | 2 | 80 | officeimo 62 |
| `GetAsByteArray()` | 15 | 12 | 0 | 14 | etlnet 3 |
| `.Dimension` (`.End.Row`, ...) | 221 | 21 | 0 | 210 | xlobjmapper 34 |
| `Style.Font.*` | 177 | 16 | 14 | 79 | eppsamples 98 |
| `Style.Fill.*` | 204 | 12 | 20 | 111 | eppsamples 93 |
| `Style.Border.*` | 123 | 8 | 20 | 76 | eppsamples 47 |
| `Style.Numberformat.Format` | 91 | 11 | 4 | 40 | eppsamples 51 |
| `Style.HorizontalAlignment` / `VerticalAlignment` | 64 | 9 | 4 | 55 | tunnetcom 20 |
| `Style.WrapText` | 10 | 6 | 2 | 9 | bookfx 4 |
| `AutoFitColumns()` | 75 | 12 | 0 | 29 | eppsamples 46 |
| `Column(i).Width` / `Row(i).Height` | 34 | 7 | 6 | 34 | lazyfurs 19 |
| `Merge = true` | 22 | 4 | 0 | 16 | tunnetcom 10 |
| `Tables.Add` / `ExcelTable` | 32 | 4 | 0 | 17 | eppsamples 15 |
| charts (`AddChart`, `Excel*Chart`) | 14 | 4 | 0 | 9 | eppsamples 5 |
| `Drawings.AddPicture` | 7 | 2 | 0 | 1 | eppsamples 6 |
| `ConditionalFormatting` | 69 | 3 | 0 | 19 | eppsamples 50 |
| `DataValidations` | 12 | 2 | 0 | 6 | eppsamples 6 |
| `PivotTables` | 36 | 3 | 0 | 7 | eppsamples 29 |
| `Protection` / `Encryption` | 11 | 1 | 0 | 0 | eppsamples 11 |
| named ranges (`Names.Add*`) | 8 | 2 | 0 | 4 | bookfx 4 |
| comments | 21 | 2 | 0 | 1 | eppsamples 20 |
| hyperlinks | 3 | 1 | 0 | 0 | eppsamples 3 |
| `View.FreezePanes` | 14 | 5 | 0 | 11 | officeimo 8 |
| `AutoFilter` | 34 | 6 | 0 | 11 | eppsamples 23 |
| `InsertRow` / `DeleteRow` / `InsertColumn` / `DeleteColumn` | 7 | 4 | 0 | 5 | dorisoy 3 |
| `ExcelRange` / `ExcelRangeBase` (type) | 145 | 16 | 11 | 95 | eppsamples 50 |
| `ExcelWorksheet` (type) | 219 | 27 | 3 | 177 | eppsamples 42 |
| `ExcelWorkbook` (type) | 3 | 3 | 0 | 2 | bookfx 1 |
| `ExcelPackage` (any type use) | 462 | 38 | 22 | 303 | eppsamples 159 |
| `[EpplusTable*]` / `[EpplusIgnore]` attributes (5+) | 20 | 1 | 0 | 0 | eppsamples 20 |
| `Workbook.Properties` | 30 | 6 | 0 | 8 | eppsamples 22 |
| VBA (`VbaProject`) | 7 | 1 | 0 | 0 | eppsamples 7 |

`Worksheets[...]` index forms: `[0]` 91, `[1]` 20, `[2]` 7, `[3]` 1, a variable 12, a string name ~80.

## What the tests cover

Only 6 of 42 repos have a test file that touches EPPlus at all (18 test hits on `using OfficeOpenXml`):

- `bookfx` (xUnit + FluentAssertions): renders a workbook and asserts cell values and styles back
  (`range.Style.Font.Bold.Should().BeFalse()`, border and fill tests). The only repo whose tests check styles.
- `dexiom` (MSTest, 98 assertions): exporter tests read cells back (`.Text`, `.Value`) after export. Library on EPPlus
  4.1/4.5.3.3; does not build on SDK 10 because it targets .NET Framework 4.0 (no reference assemblies).
- `dataextractor` (xUnit): reads a checked-in `.xlsx` and asserts the extracted objects.
- `etlnet` (xUnit): one doc-example test builds a workbook in memory and checks the ETL read result (row count).
- `zserver` (xUnit): reads a reference `.xlsx` as test data, no assertions about EPPlus output.
- `officeimo`: benchmarks only.

Everywhere else EPPlus is production code that **writes** a file (export endpoints, report generators: `new
ExcelPackage()` → `Cells[r, c].Value = ...` → styles → `GetAsByteArray()` / `SaveAs`) or **reads** an uploaded file
(`new ExcelPackage(stream)` → `Dimension.End.Row` loop → `Cells[r, c].Value`/`.Text`/`GetValue<T>`), and no test checks
either. A green test run after a rewrite therefore says almost nothing about behaviour: see the council question on a
cell-compare harness below.

## Observations

- The core is small and uniform: package construction (stream/empty/file), `Worksheets.Add` / indexer, `Cells[r, c]`
  get/set `.Value`, `Dimension` loops, `GetAsByteArray`/`SaveAs`. These five shapes appear in 25–37 of 42 repos.
- Styles are common (`Font`/`Fill`/`Border`/number format/alignment in 8–16 repos) and map to ClosedXML almost
  1:1 (`cell.Style.Font.Bold = true` is the same in both), but colour types differ (`Fill.BackgroundColor.SetColor(Color.X)`
  vs `Fill.BackgroundColor = XLColor.FromColor(..)`; `PatternType = ExcelFillStyle.Solid` is implicit in ClosedXML).
- Value semantics differ: EPPlus `.Value` is `object`; ClosedXML (0.100+) `.Value` is `XLCellValue` (implicit from
  string/number/bool/DateTime, not from `object`). Setting from an `object`-typed expression needs `SetValue(..)` or
  `XLCellValue.FromObject(..)`; reading into `object`/`ToString()` changes meaning (`Blank` vs `null`). `.Text` →
  `GetFormattedString()`.
- `Dimension` (null for an empty sheet!) → `RangeUsed()` (also null) / `LastRowUsed()`; loops usually do
  `ws.Dimension.End.Row`, a 1:1 shape if nullness is kept.
- `GetAsByteArray()` has no ClosedXML equivalent: `using var ms = new MemoryStream(); wb.SaveAs(ms); ms.ToArray()`
  (statement-level rewrite).
- `Save()` saves back to the file/stream given in the constructor; ClosedXML `Save()` only works for a workbook opened from a
  path. Stream-constructed packages need `SaveAs(stream)`: Manual unless the stream is in scope.
- `LicenseContext` / `License.Set*` lines (33 hits, 15 repos) can simply be removed.
- Rare in real code: charts, pivot tables, conditional formatting, data validation, VBA, protection, comments: all
  concentrated in `eppsamples`. Manual is fine for these.
- `LoadFromCollection`/`LoadFromDataTable` (12 repos together) → `InsertTable`/`InsertData`; header and table-style
  behaviour differs (`InsertTable` creates an Excel table with a style; `LoadFromCollection(x, true)` writes plain cells).

## Recommended order (draft queue)

| Order | Pattern | Evidence (excl. eppsamples) | Notes |
| ----: | ------- | --------------------------- | ----- |
| 1 | `epplus.license-context` | 33 hits / 15 repos | delete the statement; trivial, unblocks every 5+ repo |
| 2 | `epplus.package` | `new ExcelPackage` 195 / 36, type uses 303 | `ExcelPackage` → `XLWorkbook`, `pkg.Workbook.X` → `wb.X`; ctor forms stream/empty/FileInfo/path |
| 3 | `epplus.worksheets` | `Worksheets.Add` 147 / 27, indexer 141 / 26 | `Add(name)` 1:1; string indexer → `Worksheet(name)`; int index needs the EPPlus version (0- vs 1-based) |
| 4 | `epplus.cells` | `Cells[r, c]` 879 / 33, range 140 / 15, address 49 / 9 | `Cells[r, c]` → `Cell(r, c)`, `Cells[r1,c1,r2,c2]` → `Range(r1,c1,r2,c2)`, `Cells["A1"]` → `Cell("A1")` / `Range("A1:B2")` |
| 5 | `epplus.values` | `.Value` 544 / 29, `.Text` 70 / 11, `GetValue<T>` 131 / 7 | typed set → `.Value =`, `object` set → `SetValue`, `.Text` → `GetFormattedString()`, `GetValue<T>` 1:1 |
| 6 | `epplus.save` | `GetAsByteArray` 14 / 12, `SaveAs` 53 / 12, `Save()` 80 / 12 | `GetAsByteArray` → MemoryStream block; `Save()` on stream package → Manual |
| 7 | `epplus.dimension` | 210 / 21 | `Dimension.End.Row` → `LastRowUsed()?.RowNumber()` etc.; keep null semantics |
| 8 | `epplus.styles` | Font 79 / 16, Fill 111 / 12, Border 76 / 8, Numberformat 40 / 11, alignment 55 / 9 | colours via `XLColor.FromColor`; enum mapping (`ExcelBorderStyle` → `XLBorderStyleValues`) |
| 9 | `epplus.layout` | `AutoFitColumns` 29 / 12, width/height 34 / 7, `Merge` 16 / 4, `FreezePanes` 11 / 5 | `AdjustToContents()`, `Column(i).Width`, `Range.Merge()`, `SheetView.Freeze(r, c)` |
| 10 | `epplus.load-from` | `LoadFromCollection` 6 / 5, `LoadFromDataTable` 14 / 7 | `InsertData` (plain cells) rather than `InsertTable`; header flag decides |
| 11 | `epplus.unsupported` | charts, pivots, conditional formatting, validation, VBA, protection, comments, `Formula`/`Calculate`, `[Epplus*]` attributes | Manual with a reason |

## Council questions

1. **Behaviour proof (cell-compare harness).** Recommendation: yes. Tests in the corpus almost never check workbook
   output (6 of 42 repos, 1 with styles), so the AutoMapper-style "the repo's own tests stay green" proof is weak here.
   Proposal: a `Proofs/EPPlus/<Pattern>/` proof gets a third, optional part, a small runner (in `src/Unshackle.Tests`,
   not in `Core`) that executes the `before` snippet against EPPlus 4.5.3.3 and the `after` snippet against ClosedXML,
   saves both to `.xlsx`, reopens both with ClosedXML (or plain OpenXML) and compares per cell: value + type, formatted
   text, number format, font bold/italic/size/colour, fill colour, borders, merged ranges, column widths (with a tolerance).
   Differences are listed per cell; a pattern is only Fixed if its proofs compare equal. Open: run it in the unit tests
   (needs both libraries in the test project, compiled snippets via Roslyn in memory) or as a separate tool run in the
   corpus round only?
2. **Which EPPlus version for the unit-test types?** 4.5.3.3 (LGPL, last free) is the obvious pin; the 5+ shapes
   (`LicenseContext`, `License.Set*`, 0-based worksheets, `[EpplusTable]`) are not in it. Use 4.5.3.3 plus syntax-only
   detection for 5+ members, or also reference a 5+/8 package for types (Polyform Noncommercial licence: allowed for
   our use?)?
3. **Integer worksheet index.** The project's EPPlus version decides 0- vs 1-based. Read the version from the project's
   package reference (DataAccess already sees the project) and rewrite `Worksheets[i]` → `Worksheet(i + 1)` for 5+,
   or always Manual for integer indexes?
4. **`object`-typed values.** `cell.Value = someObject` (common in generic exporters) → `SetValue(someObject)` /
   `XLCellValue.FromObject(..)` (accepted) or Manual?
5. **Target version**: ClosedXML 0.105.x (current, `XLCellValue`) — confirm, and confirm MIT licence and no
   sponsor/licence check (package rule in `CLAUDE.md`).
6. **Scope of libraries built on EPPlus** (`dexiom`, `dataextractor`, `bookfx`: wrappers whose public API exposes
   `ExcelWorksheet`/`ExcelRange`): rewriting changes their public API. In scope or Manual-by-design?

## See also

- `claude/tasks/TAAK-309-epplus-to-closedxml.md` — the task
- `claude/tools/corpus-epplus-repos.txt` — the repos and commits
- `docs/projectinfo/architectuur/corpus-automapper.md` — the survey this one follows
- `claude/tasks/automapper-queue.md` — the queue format the draft queue will follow
