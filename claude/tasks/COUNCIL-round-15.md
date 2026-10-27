# Council round 15 — AutoMapper re-measure questions and TAAK-333 checks

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint.
Questions: the "Council questions" section of `claude/tasks/TAAK-332-automapper-remeasure.md`, plus a confirmation of the
conservative choices recorded in `claude/tasks/archief/TAAK-333-council-14-followups.md`. Follow-up: TAAK-334.

| # | Decision | Joint rule |
| --- | --- | --- |
| A13 | SDK-default packable library in an application solution | Stays packable (Manual); no heuristic. The Manual reason names the fix: set `<IsPackable>false</IsPackable>`. |
| A14 | Generic profiles (`CreateMap<TRole, TRole>`) | Close per type argument like A11 (same closure, depth 3, same packability rule); open set stays Manual `GenericProfileOpenSet`. |
| H1 | Corpus harness on a full disk | Yes: check free space before each repo, stop the run (not skip the repo) below the threshold, mark the run incomplete. |
| M14 | TAAK-333 conservative choices | Confirmed: M12 only for the fix's own namespaces; M10 clause only on the kept-for-dependent row; M13 Info row on the solution file, id `changed-code-info`. |

## A13 — SDK-default packability

- G: `dotnet pack` packs a class library unless told otherwise; skoruba ships exactly such libraries. A wrong guess breaks
  external callers we cannot see. No rewrite of public API on a heuristic.
- P: letterbook/personalasst are blocked on a one-line project setting. Tell the user that line instead of guessing.
- A: reachability must stay derivable from project properties (`IProjectProperties`), not from solution shape.

Rule: R1/R2 are unchanged. The Manual reason for "public member of a packable library" ends with: "Set `<IsPackable>false</IsPackable>`
in <project> if this library is not published." A re-run after that change closes the set.

## A14 — generic profiles

- G: closing is safe only with the same proof as A11: every type argument known from the solution, constraints only filter.
- P: 45 skoruba rows hang on one shape; the A11 machinery already exists.
- A: a generic profile is a generic type whose `CreateMap<T, T>` calls are instantiated by its closed constructions;
  `TypeArgumentClosure` applies to type parameters of a class as well as of a method.

Rule: collect closed constructions of the generic profile (closed subclasses, `new P<X>()`, `AddProfile<P<X>>`); any open use
(`typeof(P<>)`, assembly scanning that cannot be resolved) → Manual. Each closed pair is treated as a normal `CreateMap<X, X>` for the
map patterns. Fixed only when every pair is Fixed; otherwise Manual `GenericProfileOpenSet` naming the open source. A13 applies to the
profile's library; skoruba is packable, so its corpus delta is expected to stay low.

## H1 — harness free-disk check

- G: restore/pdb failures from a full disk look like results; a run with fake results is worse than no run.
- P: D: is nearly full; the check is a few lines.
- A: the harness lives outside the repo; the rule is recorded here and in the CORPUSRUN notes.

Rule: before each repo the harness checks free space on the corpus disk (threshold 20 GB, configurable). Below it: stop, write
"run incomplete: disk below threshold" in the CORPUSRUN section, do not count the remaining repos. Clean `obj/bin` of finished repos
when JOBS > 1. CORPUSRUN-automapper results produced on the full disk are marked suspect and re-measured.

## M14 — TAAK-333 conservative choices

- M12 only for the fix's own namespaces: correct; other usings are not ours to add or remove.
- M10 clause only on the kept-for-dependent row: correct; other rows have no "kept" context.
- M13 Info row on the solution file, id `changed-code-info`: correct; it is fix-level, and the generic id lets other fixes reuse it.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/TAAK-332-automapper-remeasure.md`
- `claude/tasks/TAAK-325-automapper-generic-destination.md`
- `claude/tasks/archief/TAAK-334-council-15-followups.md`
- `claude/tasks/CORPUSRUN-automapper.md`
