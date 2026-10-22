# TAAK-309 — EPPlus → ClosedXML (roadmap step 9)

**Status**: step 1/5 — survey done (builds partial: C: full), awaiting council (step 2)
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
- [ ] 2. Decide the behaviour proof (cell-compare harness or not) with the council
- [ ] 3. `EPPlusFix` skeleton with proofs; `claude/tasks/epplus-queue.md`
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

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
