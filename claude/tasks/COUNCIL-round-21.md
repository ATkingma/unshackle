# Council round 21 — EF bulk kept rows, SpecFlow detection and dropped usings

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. Three separate sub-agents
gave positions. Unanimous on B13, S10, S11, S12. Round 1 split on B14 (G: every symbol per line; P: count + first line; A: one row
per file, first line). The joint rule for B14 combines the three positions (one row per file, every line listed); it was not put
to a second vote.
Questions: council questions 13-14 in `claude/tasks/TAAK-342-ef-bulk.md`, council questions 9-11 in
`claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
Follow-ups: TAAK-342 steps 8a-8b (B13, B14), TAAK-340 steps 4a-4c (S10, S11, S12). TAAK-354 is not used.

| # | Decision | Joint rule |
| --- | --- | --- |
| B13 | Kept-package row name (TAAK-342 Q13) | Name the Z package(s) the project file actually references (Plus, Extensions, or both in one row), read from the project file, not `RemovedPackage`. Unreadable project file → today's text plus "or `Z.EntityFramework.Extensions.EFCore`". Blocking logic unchanged; unit test for an Extensions-only project. |
| B14 | Usages without a call (Q14) | One unmatched-use row per file with any bound Z symbol outside a reported call/constructor, also when the file already has a Manual row; the row gives the count and every line ("N other Z usages: lines a, b, c"). Removal still depends only on the bound-symbol count, never on the report. |
| S10 | Static imports (TAAK-340 Q9) | Do not follow or edit imported props files; `SharedDeclarationBlocked` Manual stays. The row names the declaring file (and line when known) and the rename to make there; the `ProjectReference`-only case names the project that declares the package. |
| S11 | Autofac proof and build-only plugins (Q10) | Add `SpecFlow.Autofac` and `Reqnroll.Autofac` to `Directory.Packages.props` (test-only, after a license check) with a proof for the namespace map and the `ReqnrollVersion` pin. Until it passes, the Autofac rename is Manual "possible match". If the license blocks it: stub + recorded nupkg version (B6 precedent). Build-only plugins (LivingDoc): step 4 makes any unrecognised `SpecFlow.*`/`SpecRun.*` package reference in a project file a project blocker. |
| S12 | S7 scope (Q11) | Accepted as built. The plan lists the exclusions literally; the drop is decided by the semantic model (no symbol binds through the using), not by position alone. No Info row for a kept using. "Only using of its block" may be revisited with trivia cleanup. |

## B13-B14 — EF bulk kept rows

- G: a wrong package name ends up in hand edits; a usage hidden behind a Manual row becomes a build break after the user fixes that row.
- P: name what is in the csproj; a per-symbol list is noise, a count plus lines is enough.
- A: "what you see is what you have"; one row per blocking file keeps the rule predictable, dedupe by file.

## S10-S12 — SpecFlow detection and usings

- G: an imported props file can be shared beyond the analysed solution; an unproven namespace map is a run-time DI risk; dropping a
  using can change overload binding silently.
- P: a row pointing at the file and line is a one-line hand edit; a wrong Autofac rename breaks every DI project.
- A: one owner per edit (project file + nearest props); every mapping gets a proof, this one is no exception.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-19.md`
- `claude/tasks/TAAK-342-ef-bulk.md`
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md`
