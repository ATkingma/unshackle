---
kingmadoc: 1
feature: "epplus-package"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14, REQ-15]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/PackagePattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/EPPlusSyntax.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/WorkbookUses.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/FontsLicence.cs
    - src/Unshackle.Tests/Core/EPPlus/FontsLicenceTests.cs
    - src/Unshackle.Tests/Core/EPPlus/CellValueHarnessTests.cs
    - src/Unshackle.Tests/Core/EPPlus/WorksheetIndexTests.cs
    - src/Unshackle.Tests/Proofs/EPPlus/Package/
    - src/Unshackle.Tests/Core/EPPlus/EPPlusFixTests.cs
    - src/Unshackle.Tests/Core/EPPlus/EPPlusHarnessProofTests.cs
    - src/Unshackle.Tests/Core/EPPlus/CellCompare.cs
---

# Feature: epplus.package: `ExcelPackage` → `XLWorkbook` where provable

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-309 step 3 (queue row 2, `claude/tasks/epplus-queue.md`); council round 8, general rule and E1.

## One-sentence summary

epplus.package: a `new ExcelPackage()` held in a using local becomes `new XLWorkbook()` only when every use of that local is a mapped,
harness-proven shape (`p.Workbook.Worksheets.Add("name");` and `p.SaveAs(new FileInfo(path));`); everything else is Manual with the first
blocking use.

## Scope (in / out)

**In scope**

- `using (var p = new ExcelPackage()) { ... }` and `using var p = new ExcelPackage();` / `using ExcelPackage p = new ExcelPackage();` /
  target-typed `new()`, with an empty argument list, as the only ExcelPackage local of its scope.
- Uses of `p`: `p.Workbook.Worksheets.Add("<literal>");` as a statement with a valid sheet name (1–31 characters, none of `[]:*?/\`, no
  leading/trailing apostrophe) → `p.Worksheets.Add("<literal>");`; `p.SaveAs(new FileInfo(<string>));` as a statement → `p.SaveAs(<string>);`.
- An explicit `ExcelPackage` type of the local → `XLWorkbook`; the `ClosedXML.Excel` using is added, `OfficeOpenXml` goes when unbound.

**Out of scope (Manual, each a later queue row)**

- Constructor arguments (`FileInfo`, `Stream`, path, password): opening/decrypting is not mapped yet (rows 2b/6).
- A package outside a using local (field, property, parameter, return value, a plain local), or two packages in one scope.
- Any other use of the local: the worksheet result used (`var ws = p.Workbook.Worksheets.Add(..)`: row 3), `Worksheets[..]`,
  `GetAsByteArray()`, `Save()`, `SaveAs(Stream)` (row 6), passing `p` on.

## Design

The pattern finds the scope (the using statement, or the block of a using declaration) when it holds exactly one package creation, so one
`Fixed` result replaces the creation, the declared type and every use together; otherwise it returns the creation, which is Manual. Uses
are the identifiers bound to the local's symbol. A use outside the mapped set stops the rewrite with
`ExcelPackage local p is used as '<use>' (line N), which is not mapped yet`.

`EPPlusFix.AllOrNothing` is true: a Manual row leaves the whole solution as it is (half on EPPlus, half on ClosedXML does not build once a
worksheet crosses a method boundary). The package swap (`EPPlus` → `ClosedXML 0.105.1`) only happens per project when nothing of EPPlus is
left (general rule, round 8).

**Behaviour proof (E1).** Every Fixed proof declares `public static void Write(string path)`; `EPPlusHarnessProofTests` compiles `before`
against EPPlus 4.5.3.3 and `after` against ClosedXML in memory, runs both, reopens both files with ClosedXML and requires 0 differences per
cell (value + type, formatted text, number format, font, fill, borders, alignment, merges, column widths, sheet names and order).
`HarnessProofs_CoverEveryFixedPackageProof` fails if a Fixed Package proof has no `Write`.

Facts pinned while building: EPPlus 4.5.3.3 and ClosedXML both throw on an empty workbook save and on a duplicate or invalid sheet name,
but not with the same exception, so invalid literal names are Manual. `SaveAs(FileInfo)` and `SaveAs(string)` both overwrite an existing
file.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | `using (var p = new ExcelPackage())` with Add + SaveAs(FileInfo) → `XLWorkbook`, same workbook in the harness (proof `using-statement`). |
| REQ-2 | Using declaration with explicit type and two sheets → `XLWorkbook` type and creation, same workbook (proof `using-declaration`). |
| REQ-3 | Any unmapped use is Manual naming the use and line (proofs `used-worksheet`, `byte-array`, `invalid-sheet-name`; `Fix_SaveAsStream_ManualAndSolutionUntouched`). |
| REQ-4 | Constructor arguments are Manual (proof `existing-file`). |
| REQ-5 | A package that is not a using local is Manual (proof `field`); two in one scope are Manual (`Fix_TwoPackagesInOneBlock_Manual`). |
| REQ-6 | Every Fixed proof passes the cell-compare harness, and the harness fails on a deliberate difference in each compared aspect (`CellCompareTests`). |
| REQ-8 | Row 3 / E9: a plain local without `using` is rewritten like a using local (proof `plain-local`). |
| REQ-9 | Row 3: `var ws = p.Workbook.Worksheets.Add("x")` / `ExcelWorksheet ws = p.Workbook.Worksheets["x"]` used only for `Name` → `IXLWorksheet`, `p.Worksheet("x")` (proofs `worksheet-local`, `worksheet-name`); any other worksheet use is Manual (`used-worksheet`). |
| REQ-10 | Row 3: a name lookup only when the exact literal was added before in the scope's own block (not in a branch or loop) (proofs `name-missing`, `conditional-add`); a rename only to a valid name no added sheet has (`rename-to-existing`). |
| REQ-11 | Row 3 / E3: `Worksheets[i]` with a literal index is Fixed only for EPPlus 5+ (0-based → `Worksheet(i + 1)`) and only within the added sheets (`WorksheetIndexTests`); EPPlus 4.x is Manual `WorksheetIndexBaseNotProvable` (proof `worksheet-index`, harness fact `Harness_EPPlus4OnNetStandard_IsZeroBased`); no EPPlus reference → `UnknownEPPlusVersion`. |
| REQ-12 | E7 (council round 10): every Fixed package carries a Warning naming the SixLabors.Fonts 2.x licence and the `< 2.0` pin; a project whose compilation already references SixLabors.Fonts 2.x is Manual `LicensedDependency` (`FontsLicenceTests`). The pin itself is not written yet: it needs a current assets file, which Core cannot read (engine request). |
| REQ-13 | Row 4: on a worksheet local, `ws.Cells[r, c]` (two `int` expressions) and `ws.Cells["A1"]` (a literal single-cell address) → `ws.Cell(r, c)` / `ws.Cell("A1")`, as the target of a covered `Value` assignment or as the receiver of a style statement `epplus.styles` already migrated. Ranges (`Cells[r1, c1, r2, c2]`, `"A1:B2"`) and any other cell use are Manual (proofs `cells-values`, `used-worksheet`, `cell-range`). |
| REQ-14 | Row 5 / E4: `Value = x` is Fixed for string, bool and the int types (int, short, byte, sbyte, ushort) always, and for double, float, decimal and long only as a constant that round-trips in 15 significant digits. Every other type (DateTime, enum, object, a non-constant double, `null`) is Manual `UncoveredRuntimeType` (`CellValueHarnessTests`; proofs `uncovered-datetime`, `uncovered-double`). |
| REQ-15 | `epplus.styles` runs before `epplus.package`, so a cell that is both styled and given a value is migrated in one run, and the result is equal in the harness (`Fix_CellValuesAndStyles_RewrittenTogetherAndSameWorkbook`). |
| REQ-7 | With the licence pattern in the same file, both are rewritten, the result compiles against ClosedXML and compares equal (`Fix_LicenseLineAndPackage_BothRewrittenAndAfterCompiles`). |

## See also

- [verify](epplus-package-verify.md), [modellen](epplus-package-modellen.md)
- `claude/tasks/epplus-queue.md`, `claude/tasks/TAAK-309-epplus-to-closedxml.md`
- `docs/projectinfo/architectuur/corpus-epplus.md` — `new ExcelPackage()` empty: 50 hits / 18 projects
