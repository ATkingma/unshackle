# TAAK-378 — SpecFlow patterns report on `obj/**/*.GlobalUsings.g.cs`

**Status**: open
**Created**: 2026-10-28
**Goal**: Generated documents under `obj/` (implicit `<Using>` items) give no finding; the `<Using>` item itself is migrated by `UsingRenameEdit`.

## Context for the next agent

- Corpus round 1 (`claude/tasks/CORPUSRUN-specflow.md`), repo `executeautomation/Playwrightdotnet`: `specflow.namespace` reported
  `SpecFlowProject1/obj/Debug/net7.0/SpecFlowProject1.GlobalUsings.g.cs:9` (the corpus run fixes after a baseline build, as a user would).
- Harmless here (the run was blocked anyway), but in a migrating run the fix would edit a file the next build overwrites, and a Manual
  row there would block the unit. S16 already skips `*.feature.cs`; `IFix.SkipsFile` is the hook.
- No proof: the proof harness cannot model a generated `obj/` document. Test it with a unit test on `SkipsFile` (or the Fixer's
  generated-document filter) instead.

## Steps

- [ ] 1. Check whether the Fixer skips generated documents for other fixes (`*.g.cs`, `obj/`); if so, find why this one got through
- [ ] 2. Skip it (shared, not SpecFlow-only if the gap is in the Fixer); unit test

## See also

- `claude/tasks/CORPUSRUN-specflow.md`
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md` — step 5 (S16)
