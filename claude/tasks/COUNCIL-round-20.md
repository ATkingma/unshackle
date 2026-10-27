# Council round 20 — xUnit v3 detection, EPPlus helper names, unparsable pin bounds

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint
(round 1 split on X8 and X9; round 2 accepted the proposals below).
Questions: open council questions 1-3 in `claude/tasks/TAAK-341-xunit-v3.md`, council question 1 in
`claude/tasks/archief/TAAK-346-epplus-flow-rows.md`, council question 1 in `claude/tasks/archief/TAAK-349-transitive-pin-reasons.md`.
Follow-ups: TAAK-351 (E33, plus round 19 R3, which TAAK-346 archived without doing), TAAK-352 (X8), TAAK-341 step 5f (X9).

| # | Decision | Joint rule |
| --- | --- | --- |
| X7 | Non-test project with the `xunit` package (TAAK-341 Q1) | Accepted as built: `xunit`/`xunit.core` → `xunit.v3.extensibility.core` (+ `xunit.v3.assert`), OutputType unchanged, Info row naming the missing sign. Keeping v2 mixes v2 and v3 assemblies and does not build. |
| X8 | Sync-over-async test-attribute detection (Q2) | `IProjectFileMigration` becomes async: `EditsForAsync(Project, CancellationToken)` and `ProjectBlockersAsync(Project, CancellationToken)`; `Fixer` awaits them with the run's token. `GetAwaiter().GetResult()` with `CancellationToken.None` breaks `code-standaarden.md` §6 (token always required and passed on). All implementers move in one step; no sync overload kept. |
| X9 | Runner bump range (Q3) | `From` 2.0.0 accepted (an older 3.x is raised too). A 1.x runner is not bumped, but gets an Info row ("update `xunit.runner.visualstudio` 1.x to 3.1.5 by hand") instead of no row. |
| E33 | E29 conservative scope (TAAK-346 Q1) | Accepted: any return-helper call on the package in the scope blocks a rename to its name (Manual, never a wrong rename). The Manual reason names the helper call with file:line. Narrow (branch/order aware) only on corpus evidence. |
| T6 | Unparsable reference bound (TAAK-349 Q1) | Accepted: the row stays Manual with no direction text; no guess between NU1605 and downgrade. No work. |

## X7-X9 — xUnit v3

- G: a half-migrated helper project that mixes v2 and v3 cannot build; the rename is the only safe choice. A sync wait without the run's
  token makes cancellation unreliable on large solutions.
- P: silence about a 1.x runner leaves the user with a test run that finds no tests; one Info line costs nothing.
- A: the standard is explicit (token always required, passed on); an interface that forces sync-over-async is the defect, not the call
  site. One async contract keeps every migration predictable.

## E33 — EPPlus helper-added sheet names

- G: an order- or branch-aware check is easy to get wrong; a false Fixed renames the wrong sheet at run time.
- P: acceptable if the row names the helper call, so the hand work is one look.
- A: the scope rule is simple to predict from the report.

## T6 — transitive pins

- All: without a comparable version any direction text would be a guess; Manual already protects the user.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-19.md`
- `claude/tasks/TAAK-341-xunit-v3.md`
- `claude/tasks/archief/TAAK-351-epplus-row-fold.md`
- `claude/tasks/TAAK-352-async-project-migration.md`
- `claude/tasks/archief/TAAK-346-epplus-flow-rows.md`
- `claude/tasks/archief/TAAK-349-transitive-pin-reasons.md`
