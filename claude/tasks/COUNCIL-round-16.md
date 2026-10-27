# Council round 16 — Polly user clauses, EPPlus flow types, AutoMapper generic destination, transitive package loss

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint.
Questions: P-Q1 in `claude/tasks/archief/TAAK-335-polly-redundant-transient-clause.md`, council questions 1-8 in
`claude/tasks/TAAK-327-epplus-flow-types.md`, 1-4 in `claude/tasks/TAAK-325-automapper-generic-destination.md`, step 1 in
`claude/tasks/TAAK-329-transitive-package-loss.md`. Follow-ups: TAAK-336, TAAK-337, TAAK-338; TAAK-329 continues with G10.

| # | Decision | Joint rule |
| --- | --- | --- |
| P1 | Dedupe user clauses equal to a generated transient clause | No. Only clauses Unshackle generates are deduplicated; user clauses stay verbatim. |
| E20 | Internal flow members (TAAK-327 Q1) | Not now; private only. Internal waits for a corpus count; `InternalsVisibleTo` will be a blocker (Manual), not a co-migration. |
| E21 | First Fixed field shape (Q2, Q6) | A private field of a non-partial type, written only inside that type from a migrated package/worksheet expression (no constructor parameter source). A constructor parameter source stays Manual `FlowFieldNotMapped`. |
| E22 | Return types (Q3, Q6) | Fixed for a private helper whose returned expression is itself Fixed (literal sheet name or index) and whose callers are all migrated. No constant propagation through calls; a non-literal sheet name stays Manual. |
| E23 | Helper chains (Q4) | Allowed to depth 3 (as A11), all members private in one tree; recursion or depth > 3 → Manual `FlowUseNotMigrated`. |
| E24 | Double rows (Q5) | Yes: `epplus.unsupported` skips a node already reported Manual by an earlier EPPlus pattern; one row per node with the most specific reason. |
| E25 | Range locals vs range parameter (Q7) | `ExcelRange` locals in `WorkbookUses` first, then the `ExcelRange` parameter flow type on top of them. |
| E26 | Non-positive coordinates (Q8) | Accept as today, consistent with the existing `Cells[..]` → `Range(..)` mapping; only the exception type of already-failing code differs. Documented as a limitation in the plan. |
| A15 | Inline selector (TAAK-325 Q1) | Keep the inline conditional; no named member. |
| A16 | Profile writes projections for closed pairs (Q2) | No for now; a closed pair without a projection stays Manual. Revisit with the TAAK-325 step 6 corpus numbers. |
| A17 | Reduced extension calls in `TypeArgumentClosure` (Q3) | Confirmed for all users of the closure; regression proofs for `generic-map` and `mediatr.generic-request` with extension callers. |
| A18 | `ProjectTo<T>` with parameters (Q4) | Stays Manual; no follow-up until the corpus shows more than Kavita. |
| G10 | Lost transitive package (TAAK-329 step 1) | Add it as a direct `PackageReference` pinned to the resolved version, only when the project (or a dependent) uses its types; a CPM version conflict → Manual. Each addition is its own Fixed row. |

## P1 — user-written duplicate clauses

- G: a user clause is user intent; equality by text is not equality by meaning once trivia, captured variables or overloads differ.
- P: the clause is redundant but harmless; no corpus row is wrong because of it.
- A: Unshackle owns only what it writes (`PollyPredicate`); touching user predicates needs a predicate equivalence model we do not have.

Rule: TAAK-335 behaviour stands. No task.

## E20 — internal members

- G: internal needs every caller in the project, and `InternalsVisibleTo` opens it to other projects; that crosses E17.
- P: private covers the shapes seen so far; there is no corpus count for internal helpers yet.
- A: when built, the caller set comes from `SymbolFinder` over the project; `InternalsVisibleTo` makes the member Manual (`FlowMemberNotPrivate` names it).

Rule: private only. Count internal helpers in the TAAK-327 step 7 re-measure; reopen if they are a top row.

## E21 / E22 — first Fixed field and return shapes

- G: a field fed by a constructor parameter depends on callers outside the method set; a returned non-literal sheet name hides the
  invalid-name exception difference (harness finding c). Both stay Manual.
- P: the common shapes are `_sheet = _package.Workbook.Worksheets.Add("Data")` in the same class, and small helpers returning a sheet.
- A: both reuse `FlowTypesPattern.Migratable`: a field is a flow node whose writes are all in the type; a return is a flow node whose
  value is a mapped expression. No constant propagation keeps the analysis local to one tree.

## E23 — chains

- G: a bounded depth with a cycle check is as provable as one level; recursion stays out.
- P: helpers calling helpers are common in report builders.
- A: same depth constant as `TypeArgumentClosure` (3); caller and callee still flip together via `Migratable`.

## E24 — one row per node

- G: two rows for one node inflate Manual counts in reports and corpus tables.
- P: a small change in `UnsupportedPattern`.
- A: patterns run in a fixed order; `epplus.unsupported` runs last and can skip spans already reported Manual.

## E25 / E26 — ranges

- E25: locals are the larger shape, and the parameter flow needs them anyway (a range parameter is used like a range local).
- E26: G accepts only because the code already throws in both libraries; a valid program behaves the same. The plan's limitation list names it.

## A15-A18 — AutoMapper generic destination

- A15: G and A: the inline conditional is evaluated before `Queryable.Select`, so A12 holds; P: no member insertion machinery needed.
- A16: G: feeding the closure into `TargetsAsync` couples two passes and can write projections nobody asked for; wait for data.
- A17: G: skipping callers was a soundness gap; following them can only add type arguments, never remove proof. Regression proofs required.
- A18: one repo; the Manual reason is clear.

## G10 — transitive package loss

- G: a Manual row leaves the user with a broken build after an otherwise Fixed migration; pinning the resolved version reproduces
  exactly what compiled before.
- P: the data is already in `project.assets.json` (TAAK-319).
- A: the detector lives in Core behind `IPackageAssetsReader`; the write uses the existing project-file writer; under CPM the version
  goes into `Directory.Packages.props` only when absent there; a different version present → Manual.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/TAAK-327-epplus-flow-types.md`
- `claude/tasks/TAAK-325-automapper-generic-destination.md`
- `claude/tasks/TAAK-329-transitive-package-loss.md`
- `claude/tasks/archief/TAAK-335-polly-redundant-transient-clause.md`
- `claude/tasks/archief/TAAK-336-epplus-flow-followups.md`
- `claude/tasks/TAAK-337-epplus-range-locals.md`
- `claude/tasks/TAAK-338-automapper-closure-regressions.md`
