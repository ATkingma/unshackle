# Council round 22 — xUnit v3 step-5 patterns and the Polly kept row

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. Three separate sub-agents
gave positions. Unanimous in round 1 on X13, X14 and P11. Round 1 split on X10 (G: Fixed only when the result is used once, as X6;
P, A: always Fixed), X11 (P: a small built-in name map; G, A: no map) and X12 (G, A: report uses; P: not now, or Info only). Round 2
put a joint proposal per question; all three accepted, with the amendments folded into the rules below. An earlier round-2 draft for
X12 (Manual rows for types missing in v3 only) was superseded; the decided version is the Info rule below.
Questions: council questions 1-5 (step 5) in `claude/tasks/TAAK-341-xunit-v3.md`, the council question in
`claude/tasks/archief/TAAK-354-polly-downgrade-transitive-cpm.md`.
Follow-ups: TAAK-341 steps 5g-5j (X10-X13; X14 in step 7), TAAK-355 (P11).

| # | Decision | Joint rule |
| --- | --- | --- |
| X10 | `.AsTask()` on non-awaited `Record.ExceptionAsync` (TAAK-341 Q1) | Always Fixed, as built. `.AsTask()` converts the ValueTask once at the call site and returns exactly v2's `Task<Exception>`, which may be awaited any number of times. New proof: the result is stored, awaited twice, and both awaits give the same exception. The pattern plan states why this differs from X6. Method group stays Manual. |
| X11 | Built-in type names in string attributes (Q2) | No v2 → v3 name map now; built-in and other-assembly types stay Manual `TypeNameArgument`. The row names the type, says "no v2 → v3 name map" and gives the documented v3 name as a hint when known (text only, no rewrite). Step 7 counts these rows; a map entry may be added later only with a proof against the v3 assembly. |
| X12 | Extensibility inventory breadth (Q3) | Also report uses (not only declarations) of v2 `Xunit.Sdk` types, as Info (no AllOrNothing block), one row per file listing every line. Each row names the type and says when xunit.v3 3.2.2 has no same-name type; that list is built by a test from the real assemblies, not a hand table. One proof with a same-name v3 type with changed members shows the build fails there. Step 7 decides whether any become Manual. |
| X13 | `[PropertyData]` (Q4) | Dropped from scope: `[Obsolete(error: true)]` in xUnit 2.9.3, so a building v2 project cannot use it. The extensibility plan records why. |
| X14 | `TheoryData` / `SkipException` (Q5) | No pattern; the corpus (step 7) must show a real break, with the discovered-test count before/after as the check. `Xunit.SkippableFact` is out of scope; if found, an Info row says so. |
| P11 | Polly kept row (TAAK-354 Q1) | Yes: the kept row says "; Microsoft.Extensions.Resilience was added next to it", only when the project-file edit actually added the package in this run. Built through the existing generic "added next to it" text in `Fixer` (council 14 M10, `KeptPackageReason`), not hardcoded in the Polly fix; unit tests for the added and the not-added (blocked) case. |

## X10-X14 — xUnit v3 step-5 patterns

- G: a string type name fails only at run time, so no unproven map; an unreported `Xunit.Sdk` use can break later where nobody looks;
  a ValueTask must never be awaited twice, a Task may.
- P: under AllOrNothing every Manual row reverts the whole solution, so reporting rows stay Info unless they surely break; the row text
  gives the developer the next step.
- A: one predictable rule per pattern; any table (missing types, name map) comes from the real assemblies, never by hand.

## P11 — Polly kept row

- G: a row that hides an edit is a correctness problem in the report; only claim the add when it happened.
- P: one clause removes the contradiction the corpus run showed.
- A: one shared mechanism in `Fixer`, so every migration gets the same wording.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-21.md`
- `claude/tasks/TAAK-341-xunit-v3.md`
- `claude/tasks/TAAK-355-polly-kept-row-added-package.md`
