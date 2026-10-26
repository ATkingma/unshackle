# Council round 13 — AutoMapper generics, EPPlus partial migration, open items, ReadyToRun

Date: 2026-10-26. Perspectives: correctness/safety (S), user value (V), implementation cost (C). All decisions joint.

| # | Decision | Joint rule |
| --- | --- | --- |
| A11 | Generic `Map<T>` on a type parameter | Option A (close per type argument at callers), bounded by R1/R2. Option B is used only as a filter on the closed set: constraints rarely exclude anything (`where T : class`). |
| A12 | Type-parameter destination (`ProjectTo<T>` / `Map` dest) | Same closure as A11; `ProjectTo<T>` dispatches on `typeof(T)` outside the query, never inside it. |
| E17 | EPPlus partial migration | Per project (not per file); a project migrates only with 0 blocking rows and no EPPlus type crossing to a project that stays on EPPlus. |
| E18 | Next EPPlus queue rows | Fields/parameters/return types first; then `ExcelAddressBase.Rows/Columns`; tables; drawings last. |
| M8 | Moq per-result pattern id | Yes: every row carries its sub-pattern id (rename only, like M7). |
| M9 | NSubstitute `Configure()` for overlapping setups | Never in output; M6 stands. The Manual reason may suggest it. |
| E19 | EPPlus read mapping semantics | E12 stands; no shim that emulates EPPlus semantics in user code. |
| R4 | TAAK-305 ReadyToRun | Not built; TAAK-305 archived as deferred. |

## A11 — generic `Map<T>` on a type parameter

- S: closing at callers is sound only if every caller is in the solution. A public generic base in a packable project can be called from outside, so it stays Manual (R1/R2).
- V: ~756 rows in 8 repos; generic repository/service bases are the largest remaining AutoMapper blocker.
- C: TAAK-193 already built caller closure for MediatR; extract and reuse it.

Rule: for each `Map` call whose source or destination type is a type parameter of the enclosing method or type, collect the closed type arguments from callers and derived types (transitively through generic chains, depth limit 3). Fixed only when: the member is not reachable from outside the solution (R1/R2); every call chain resolves (no reflection, `MakeGenericType`, `dynamic`, or open generic DI registration like `typeof(Repo<>)` without a found closed set); the set is non-empty; every closed pair has a generated map. The rewrite dispatches over the proven set and must be total over it. "May reach" then narrows to that set (constraints filter it further). Otherwise Manual `GenericMapOpenSet`, naming the unresolved caller.

Follow-up TAAK-324 "AutoMapper generic Map closure": pattern `automapper.generic-map`. Extract the TAAK-193 closure from the MediatR fix into a shared Core service; use it in `src/Unshackle.Core/Fixes/AutoMapper/`. KingmaDoc plan + verify in `docs/projectinfo/patterns/`, README row. Proofs in `src/Unshackle.Tests/Proofs/AutoMapper/GenericMap/`: two closing callers (after), generic base with closed derived repositories (after), constraint filter (after), packable public base (manual), `typeof(Repo<>)` registration (manual), reflection caller (manual). Re-measure the 8 corpus repos and report the row delta.

## A12 — type-parameter destination

- S: `ProjectTo<T>` builds an expression; a type switch inside an `IQueryable` pipeline changes translation. Higher risk than `Map`.
- V: ~363 rows; worthwhile, second after A11.
- C: A11 closure plus a projection check.

Rule: `Map` with a type-parameter destination follows A11. `ProjectTo<T>` is Fixed only when the closed set is proven and each member already has a Fixed projection; the generic method selects a closed `Expression` by `typeof(T)` before building the query. Otherwise Manual `GenericProjectionOpenSet`.

Follow-up TAAK-325 "AutoMapper generic destination" (after TAAK-324): pattern `automapper.generic-destination`; proofs in `Proofs/AutoMapper/GenericDestination/` plus a test that the selected expression translates identically on EF Core sqlite in-memory; manual cases for open set and unprojectable member.

## E17 — EPPlus partial migration

- S: per file leaves worksheets flowing between EPPlus and ClosedXML code inside one project; that cannot be proven. Per project, a value only crosses through a public signature, which is checkable. Coexistence of both packages makes a per-project split compile.
- V: 0% Fixed over 38 repos is the worst corpus number; per project lifts every clean project.
- C: G4/G6 already define blocking per project; mainly the gate and package step change.

Rule: the EPPlus gate becomes per project. A project migrates when it has 0 blocking rows and no EPPlus type appears in members used by a project that stays on EPPlus (else Manual `EpplusTypeCrossesProject`). EPPlus is removed only when nothing in the project still uses it; a project with any remaining EPPlus use is not migrated (no per-file mode). The report lists each project as migrated or kept with the reason. E7 applies per migrated project. No new opt-out flag.

Follow-up TAAK-326 "EPPlus per-project migration": `src/Unshackle.Core/Fixes/EPPlus/` gate, package step (G6/G7), report rows. Unit tests: two projects (one clean, one blocked), a cross-project EPPlus type. Two-project sample in `samples/` with an integration test. Corpus re-measure with Fixed % per repo.

## E18 — next EPPlus queue rows

- V/C: helper methods taking `ExcelWorksheet` block most methods and limit E17; `Rows/Columns` is cheap; tables and drawings have big semantic gaps.

Order: (1) `ExcelPackage`/`ExcelWorksheet`/`ExcelRange` as field, parameter or return type, rewritten when every use in the flow is Fixed, private/internal only; (2) `ExcelAddressBase.Rows/Columns` and `Start/End` with harness proofs; (3) tables; (4) drawings (likely Manual codes only).

Follow-up TAAK-327 "EPPlus flow types": reorder the queue in `claude/tasks/TAAK-309-epplus-to-closedxml.md`; implement (1) with proofs in `Proofs/EPPlus/FlowTypes/` (parameter, field, return: after; public member and one unfixed use in the flow: manual); (2) as a separate pattern with E1 harness cases.

## M8 / M9 / E19 — open items

- M8: per-result ids make the report actionable at no behaviour cost. Follow-up TAAK-328 "Moq sub-pattern ids": ids per row in `src/Unshackle.Core/Fixes/Moq/`, pattern docs and report tests updated; proofs unchanged.
- M9: `Configure()` output would carry ordering semantics (Moq last-matching vs NSubstitute last-wins) we cannot prove. M6 stands; the `SetupsOverlap` reason text names `Configure()` as a manual route (in TAAK-328).
- E19: an emulating shim hides real differences and ships our code into user code. E12 stands; no task.

## R4 — TAAK-305 ReadyToRun

- V: ~1 s per run is small next to MSBuild load time. C: 2.3x package size and one package per RID multiply release and CI work. S: unsupported RIDs get no package.

Decision: do not build. Archive TAAK-305 as "Deferred (council round 13, R4)" with the measurements; revisit only on a measured startup complaint.

## See also

- `claude/docs/council.md` — earlier rounds (add the round-13 table there)
- `claude/tasks/TAAK-309-epplus-to-closedxml.md`
- `claude/tasks/TAAK-305-readytorun-tool.md`
