# Pattern queue (EPPlus 4.5.3.3 / 5+ → ClosedXML 0.105.x)

**Decision (2026-10-22, council round 8)**: target ClosedXML 0.105.x (MIT, E5); test types EPPlus 4.5.3.3 only (E2); every pattern touching
values or styles needs a cell-compare harness proof (E1, `src/Unshackle.Tests/Core/EPPlus/CellCompare.cs`); a shape is Fixed only when it
is literal, visible in one place and pinned by a proof that would fail on a behaviour change (general rule).

Same way of working as `claude/tasks/automapper-queue.md`; a row gets a TAAK number (350–359 reserved, then new) when claimed.
Order by projects first, hits second, from `docs/projectinfo/architectuur/corpus-epplus.md` (42 repos, TAAK-309 step 1; "excl.
eppsamples" counts). Rows 3–9 widen the set of uses `epplus.package` accepts: until they land, a package whose worksheet is used is Manual.

| #   | Pattern ID               | EPPlus                                                                                     | Expected (ClosedXML)                                                      | Corpus (hits / projects) | Status |
| --- | ------------------------ | ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------- | ------------------------ | ------ |
| 0   | _infrastructure_         | `EPPlusFix` (package swap EPPlus → ClosedXML 0.105.1, AllOrNothing), proof runner entry, EPPlus 5 surface stub, cell-compare harness | —                                    | —                        | done (TAAK-309) |
| 1   | `epplus.license-context` | `ExcelPackage.LicenseContext = ...;`, `ExcelPackage.License.Set*(..);`                     | statement removed                                                         | 33 / 15                  | done (TAAK-309) |
| 2   | `epplus.package`         | `using var p = new ExcelPackage()` with `Workbook.Worksheets.Add("x");` / `SaveAs(new FileInfo(path))` only | `new XLWorkbook()`, `p.Worksheets.Add("x")`, `p.SaveAs(path)` | 26 empty / 18 (of 195 / 36 creations) | done (TAAK-309, narrow; E9 plain local added) |
| 3   | `epplus.worksheets`      | `var ws = p.Workbook.Worksheets.Add(..)` used further; `Worksheets["name"]`; `Worksheets[i]` | `IXLWorksheet ws = p.Worksheets.Add(..)`; `p.Worksheet("name")`; int index by declared version (E3: 4.x 1-based, 5+ 0-based → `Worksheet(i + 1)`), unknown → Manual `UnknownEPPlusVersion` | 147 / 27; 141 / 26 | done (TAAK-309; inside `epplus.package`; 4.x int index Manual, see harness finding d) |
| 4   | `epplus.cells`           | `Cells[r, c]`, `Cells[r1, c1, r2, c2]`, `Cells["A1"]`                                      | `Cell(r, c)`, `Range(r1, c1, r2, c2)`, `Cell("A1")` / `Range("A1:B2")`    | 879 / 33; 140 / 15; 49 / 9 | open |
| 5   | `epplus.values`          | `.Value = x`, `.Value` read, `.Text`, `GetValue<T>()`                                      | typed set `.Value = x` (harness per type, E4); `object` set → `SetValue` only for a covered closed type set, else Manual `UncoveredRuntimeType`; `.Text` → `GetFormattedString()` | 544 / 29; 70 / 11; 131 / 7 | open |
| 6   | `epplus.save`            | `GetAsByteArray()`, `SaveAs(Stream)`, `Save()`, `new ExcelPackage(stream / FileInfo / path)` | MemoryStream block + `ToArray()`; `SaveAs(stream)`; `Save()` on a stream package Manual | 14 / 12; 53 / 12; 80 / 12; 121 + 14 / 13 + 9 | open |
| 7   | `epplus.dimension`       | `ws.Dimension.End.Row` / `.Column` / `.Start.*`                                            | `ws.LastRowUsed()?.RowNumber()` etc., null kept (empty sheet)             | 210 / 21                 | open |
| 8   | `epplus.styles`          | `Style.Font.*`, `Style.Fill.*` (`SetColor(Color)`), `Style.Border.*`, `Style.Numberformat.Format`, alignment, `WrapText` | `Style.Font.*`, `Fill.BackgroundColor = XLColor.FromColor(..)` (solid implicit), `Border.*Border`, `NumberFormat.Format`, `Alignment.*` | Font 79 / 16, Fill 111 / 12, Border 76 / 8, Numberformat 40 / 11, align 55 / 9 | open |
| 9   | `epplus.layout`          | `AutoFitColumns()`, `Column(i).Width`, `Row(i).Height`, `Merge = true`, `View.FreezePanes` | `AdjustToContents()` (font metrics differ: harness tolerance or Manual), width needs the padding offset (harness finding, see below), `Range.Merge()`, `SheetView.Freeze` | 29 / 12; 34 / 7; 16 / 4; 11 / 5 | open |
| 10  | `epplus.load-from`       | `LoadFromCollection`, `LoadFromDataTable`                                                  | `InsertData` (plain cells), header flag decides; `InsertTable` only when a table was asked | 6 / 5; 14 / 7 | open |
| 11  | `epplus.unsupported`     | charts, pivots, conditional formatting, validation, VBA, protection, comments, `Formula`/`Calculate`, `[Epplus*]` attributes, any EPPlus symbol no pattern found | Manual with a typed reason (and E6 `PublicApiExposesOldLibrary` for public/protected signatures in packable projects) | see survey | done (TAAK-309) |
| 12  | _corpus run_             | build + tests before/after, plus the harness on extracted writer methods where possible   | 0 regressions; Manual only where justified; `claude/tasks/CORPUSRUN-epplus.md` | — | open |

**Recommended order**: 11 (catch-all, so a partly migrated file is never silently broken) → 3 → 4 → 5 → 6 → 7 → 8 → 9 → 10, then 12.
Rows 3–5 all widen `PackagePattern`'s use set and the worksheet/cell rewrites: work them one after another, not in parallel.

**Harness findings so far (TAAK-309)**: (a) `Column(i).Width = w` differs: EPPlus 4.5.3.3 stores `w`, ClosedXML stores `w` plus the cell
padding (reads EPPlus's 20 back as 19.289), so row 9 cannot be a rename (`CellCompareTests.Differences_SameColumnWidthNumber_IsReported`);
(b) built-in number formats are written as id only by EPPlus, id + code by ClosedXML (same meaning; the harness normalises the 15
culture-invariant ones); (c) an invalid or duplicate sheet name throws in both, with different exceptions (row 3: Manual unless literal and valid);
(d) EPPlus 4.5.3.3's .NET Standard build is 0-based for `Worksheets[i]` (`WorksheetIndexTests.Harness_EPPlus4OnNetStandard_IsZeroBased`), not 1-based as
council E3 assumed. Its .NET Framework builds default to 1-based, and configuration can switch both, so 4.x integer indexes are Manual
`[WorksheetIndexBaseNotProvable]` (council question).

## See also

- `claude/tasks/TAAK-309-epplus-to-closedxml.md` — the task
- `docs/projectinfo/architectuur/corpus-epplus.md` — survey and frequencies
- `claude/tasks/automapper-queue.md` — the queue this follows
- `claude/docs/council.md` — round 8, E1–E6
