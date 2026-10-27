# TAAK-348 — Generic AutoMapper Manual reasons: name the untyped source and each row's own blocker

**Status**: done
**Created**: 2026-10-27
**Goal**: fix two report-wording bugs from the AutoMapper corpus re-measure (cats, kavita, neozhu; `CORPUSRUN-automapper.md`, findings 1 and 2).

## Context for the next agent

- `GenericDestinationPattern` printed `ProjectTo<TDestination> from ?:` when the query is a non-generic `IQueryable` (neozhu
  `MappingExtensions.cs:24`). It now prints `from an untyped IQueryable`.
- In an open set, `RewriteAsync` of `GenericDestinationPattern` and `GenericMapPattern` replaced the row's own open-set reason with the
  kept reason of `MapOwners`, which is keyed by pair and names the first blocking call of the set ("the call at X:14 is not migrated");
  kavita `ProjectToExtensions.cs:21` pointed to line 14. Both patterns now keep the kept marker and code but use their own reason.
  The outcome (Manual, kept on IMapper) is unchanged; only the reason text differs.

## Steps

- [x] 1. `GenericDestinationPattern`: name an unknown source "an untyped IQueryable"
- [x] 2. `GenericDestinationPattern` and `GenericMapPattern`: an open-set row keeps its own reason when the call is also kept
- [x] 3. Unit tests: `GenericDestinationPatternTests.FixAsync_UntypedQueryable_NamesUntypedSource` and
  `FixAsync_TwoOpenCallsInOneSet_EachNamesItsOwnBlocker` in both pattern test classes; they fail without the fix
- [x] 4. Proofs: no `manual.txt` asserts the changed text

## See also

- `claude/tasks/CORPUSRUN-automapper.md`
- `claude/tasks/archief/TAAK-338-automapper-closure-regressions.md`
