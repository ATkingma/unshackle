# TAAK-299 — Split CreateMapPattern.cs and Fixer.cs

**Status**: step 1/4 — not started
**Created**: 2026-10-21
**Goal**: execute the split proposal from TAAK-297 step 5: `CreateMapPattern.cs` (2040 lines) into five classes, `Fixer.cs` (966 lines) into five; behaviour and rendered output exactly the same.

## Context for the next agent

- The proposal with the line ranges per new class is in `claude/tasks/archief/TAAK-297-code-standards-cleanup.md` ("Proposal step 5"). Line numbers are from main `a1df338`; re-check them.
- Pure refactor: no test expectation may change. If one has to, stop and take it to the council (`claude/docs/council.md`).
- `FileFixer` is the seam TAAK-298 needs for parallel file processing (one file in, one `FilePass` out).
- Code standards: `claude/docs/code-standaarden.md` (one type per file, never `sealed`, `CancellationToken` required).
- Push procedure: `claude/docs/overdracht.md` "Push procedure".

## Steps

- [ ] 1. Split `CreateMapPattern.cs`: `CreateMapProblems`, `ProfileSyntax`, `MapAccessibility`, `MapMethodWriter`; update callers (subclasses and other AutoMapper patterns)
- [ ] 2. Split `Fixer.cs`: `PartialRun`, `FileFixer`, `SolutionChecks`, `FindingFactory`
- [ ] 3. Update `docs/projectinfo/architectuur/core.md` (and class models that name the moved methods)
- [ ] 4. Build, tests, sample tests green → commit (push by the main session)

## Progress log

_(filled in during execution)_

## See also

- `claude/tasks/archief/TAAK-297-code-standards-cleanup.md` — the proposal
- `claude/tasks/TAAK-298-performance.md` — uses the `FileFixer` seam
- `claude/docs/INDEX.md` — documentation index
