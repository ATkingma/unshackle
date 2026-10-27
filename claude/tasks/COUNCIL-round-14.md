# Council round 14 — Moq corpus leftovers (TAAK-330)

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint.
Questions: the "Council questions" section of `claude/tasks/TAAK-330-moq-corpus-leftovers.md`. Follow-up: TAAK-333.

| # | Decision | Joint rule |
| --- | --- | --- |
| M10 | Kept row vs Fixed project-file row | One row per project file stays; the kept row also names the package that was added next to the old one. |
| M11 | Dependents named in the kept row | List all keeping dependents in solution order, at most 5 in the text, then "and N more"; the JSON report has the full list. |
| M12 | Full-name rule on the add side | Symmetric: no new using is added when every rewritten reference to that namespace is a full name from the root. Proofs updated. |
| M13 | B1 (`.Object` proxy name) | Accepted limitation, confirmed; plus one fix-level Info line on every Moq run that changed code. |

## M10 — kept row hides the Fixed project-file row

- G: two rows (Manual + Fixed) for one file contradict each other; one row is right, but it must not hide an edit we made to the file.
- P: the developer opening the project file sees both packages; the row should say why.
- A: one outcome per file (round 12 G7: every row states the final outcome); the final outcome here is "old kept, new added".

Rule: a project file has exactly one row. When the old package is kept for a dependent and the new package was added, the kept row's
reason reads "Moq stays in this project because a project that references it still uses it: <paths>; NSubstitute was added next to it."
When nothing was added, the second clause is left out.

## M11 — naming only the first dependent

- G: naming one of several hides work; the user removes the use in one project and Moq still stays.
- P: the full list is the to-do list.
- A: a deterministic order (solution order) keeps reports diffable; a cap keeps rows readable.

Rule: `ProjectMigration` keeps all keeping dependents in solution order. The row text lists up to 5 paths, then "and N more"; the JSON
report carries the full list.

## M12 — full-name rule for adding new usings

- G: an unused added using is an IDE0005 warning; with warnings as errors that is a build break (the B3 class of bug). Not harmless.
- P: the asymmetric rule leaves a dangling `using Mediator;` the user deletes by hand.
- A: one rule for both sides ("a full name from the root needs no using") is predictable from the code; the asymmetry only kept a proof stable.

Rule: in the add-new-usings decision, a rewritten reference written as the full name from the root (e.g. `Mediator.IRequest`) does not
count as a use. A using is added only when at least one rewritten reference in the file is unqualified. Proof `MediatR/QualifiedSwappedType`
(and any other proof whose `after` changes) is updated to the new output; the task lists every changed proof.

## M13 — B1 fluentmigrator as accepted limitation

- G: the proxy type name differs between Moq and NSubstitute and cannot be proven equal; but flagging every escaping `.Object` would
  turn 628 Fixed rows into Manual for one test.
- P: one failing test in 5605 is a small, obvious fix; a warning is worth more than a refusal.
- A: a per-finding rule cannot be predicted (any `.Object` may reach `GetType()`); a fix-level Info line (round 10) is the right level.

Rule: B1 stays an accepted limitation; no Manual reason. The Moq fix emits one fix-level Info line when it changed code: "Mock objects
are now NSubstitute proxies; tests that assert a mock's GetType() name or ToString() will see a different value."

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/TAAK-330-moq-corpus-leftovers.md`
- `claude/tasks/TAAK-333-council-14-followups.md`
- `claude/tasks/CORPUSRUN-moq.md`
