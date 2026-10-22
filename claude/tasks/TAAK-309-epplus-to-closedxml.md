# TAAK-309 — EPPlus → ClosedXML (roadmap step 9)

**Status**: step 3/5 — harness, skeleton and queue rows 1–2 done; queue rows 1, 2 and 11 done; next row 3 (`epplus.worksheets`)
**Created**: 2026-10-22
**Goal**: An `epplus` fix that rewrites EPPlus code (LGPL 4.5.3.3 and commercial 5+ API shapes) to ClosedXML, at 100% on an EPPlus corpus.

## Context for the next agent

- Why: EPPlus moved from LGPL to commercial from v5; many enterprise apps pin 4.5.3.3 or pay. ClosedXML (MIT) is the usual target.
- Production code producing files: tests rarely check the workbook, so behaviour proof is weak. Consider a proof harness that writes a workbook with old and new code and compares cells/styles (decide in the survey).
- Mapping sketch (verify): `ExcelPackage` -> `XLWorkbook`, `Workbook.Worksheets.Add` -> `Worksheets.Add`, `Cells[r, c].Value` -> `Cell(r, c).Value`, `Cells["A1"]`, `LoadFromCollection` -> `InsertTable`, `SaveAs`/`GetAsByteArray` -> `SaveAs(stream)`. Styles, number formats, charts, formulas: mostly Manual.
- `LicenseContext` lines (EPPlus 5+) are removed.
- Follow the AutoMapper route that worked: survey (`docs/projectinfo/architectuur/corpus-automapper.md` as the model), a pattern queue (`claude/tasks/automapper-queue.md` as the model), a KingmaDoc plan + verify per pattern in `docs/projectinfo/patterns/` and a README row, proofs in `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`, corpus rounds with the harness `claude/tools/corpus-am.sh` (`FIX=<id>`), then a blind round.
- Unit-test types come from real assemblies (last free version of the old library, pinned in `Directory.Packages.props`; check its license allows that, like AutoMapper 14 with `NoWarn`).
- Design choices (target library, what is Fixed vs Manual) go to the council (`claude/docs/council.md`). A pattern never throws; unsafe rewrites are `Manual(reason)`.
- User selection 2026-10-22: all four new libraries (Moq, Polly, EPPlus, MassTransit); order Moq -> Polly -> EPPlus -> MassTransit, each to 100% before the next.

## Steps

- [x] 1. Survey: repos with EPPlus (4.5.3.3 and 5-7), API frequency, what their tests cover; `docs/projectinfo/architectuur/corpus-epplus.md`
- [x] 2. Decide the behaviour proof (cell-compare harness or not) with the council (round 8, E1: mandatory); harness built
- [x] 3. `EPPlusFix` skeleton with proofs; `claude/tasks/epplus-queue.md`
- [ ] 4. Patterns from the queue, each with docs and proofs
- [ ] 5. Corpus round to 100%, `claude/tasks/CORPUSRUN-epplus.md`; blind round; roadmap row and README

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | new `Fixes/EPPlus/`, tests, docs |
| **How**   | New fix |
| **Why**   | Common commercial-license pain in enterprise code |

## Progress log

- 2026-10-22 (agent epplus): step 1 done. 42 repos (16 on 4.x, 7 on 5, 7 on 6, 9 on 7, 3 on 8) measured with a regex
  script; results in `docs/projectinfo/architectuur/corpus-epplus.md`, repos in `claude/tools/corpus-epplus-repos.txt`,
  clones/scripts/logs in `D:/playground/corpus-epplus/`. Builds stopped after 5 repos (C: full): excelchop, bookfx,
  zserver, xlobjmapper build; dexiom fails (net40); the rest "not built (C: full)". Draft queue and 6 council questions
  (cell-compare harness, type pin, worksheet index base, object values, ClosedXML version, wrapper libraries) in the doc.
  Next: step 2 (council round 8); re-run builds when disk space allows.
- 2026-10-22 (agent epplus-skeleton, claim #21): steps 2–3 done.
  - E5 licence check (primary sources): ClosedXML 0.105.1 nuspec `<license type="expression">MIT</license>`, repository commit
    `b4ebe47bd3ecebf8480dea7422188d16b35b9d9a`; LICENSE at that commit is plain MIT, no extra conditions:
    https://github.com/ClosedXML/ClosedXML/blob/b4ebe47bd3ecebf8480dea7422188d16b35b9d9a/LICENSE . No licence key, `LicenseContext` or
    sponsor string in `ClosedXML.dll`. Dependencies: DocumentFormat.OpenXml (MIT), ExcelNumberFormat (MIT), RBush.Signed (MIT),
    ClosedXML.Parser (MIT), SixLabors.Fonts `[1.0.0, 3.0.0)` → resolves to 1.0.0 (Apache-2.0); SixLabors.Fonts 2.x is under the Six
    Labors Split License (council question: pin < 2.0 when adding the package).
  - Cell-compare harness (E1) in `src/Unshackle.Tests/Core/EPPlus/`: `WorkbookProgram` (in-memory Roslyn compile + run of one
    `public static Write(string path)`), `WorkbookSnapshot` (reopen with ClosedXML, facts per cell: value + type, formula, formatted text,
    number format, font, fill, borders, alignment; merges, column widths, sheet names), `CellCompare.Differences`. `CellCompareTests`:
    equal workbook → 0 differences; 10 deliberate-difference rows (value, type, text, number format, font, fill, borders, merges, widths,
    sheet name) each reported. `EPPlusHarnessProofTests` runs every Fixed EPPlus proof with `Write` through it.
  - `EPPlusFix` (`epplus`, package EPPlus → ClosedXML 0.105.1, AllOrNothing), patterns `epplus.license-context` and `epplus.package`
    (narrow: using local, `Worksheets.Add("literal");`, `SaveAs(new FileInfo(path));`), 15 proofs, KingmaDoc plan/modellen/verify,
    `claude/tasks/epplus-queue.md`. EPPlus 5+ licence members for proofs come from an in-memory stub with C# 14 extension members (E2).
  - Harness findings: column width differs by the cell padding (EPPlus 20 → ClosedXML reads 19.289); built-in number formats stored
    as id only vs id + code. Both recorded in the queue.
  - `dotnet build` 0 warnings; `dotnet test` 1707 passed, 0 failed (22 EPPlus unit/harness tests + 15 EPPlus proofs).
  - Not registered in `Cli.cs` (main session); README rows pending (main session).

- 2026-10-22 (epplus-skeleton, row 11): `epplus.unsupported` (runs last; every member still naming EPPlus → Manual
  `[UnmigratedEPPlusApi]` with each API and its queue row; E6 `[PublicApiExposesOldLibrary]` for public/protected signatures in a
  packable project; unresolved 5+ members named with their receiver; licence/package findings not reported twice). 4 proofs, 2 unit
  tests, KingmaDoc docs. `dotnet build` 0 warnings; `dotnet test` 1809 passed, 0 failed. Council round 10 decided E7–E9, C1, C3
  (see council.md): E9 (plain local) goes with row 3; E7 (SixLabors.Fonts warning/pin) needs TAAK-317's IProjectFileMigration.

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
