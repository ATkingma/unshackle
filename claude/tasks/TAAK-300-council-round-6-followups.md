# TAAK-300 — Council round 6 follow-ups

**Status**: step 0/7 — not started
**Created**: 2026-10-21
**Goal**: carry out the round-6 council decisions (`claude/docs/council.md`, "2026-10-21 — open questions from TAAK-295 and TAAK-297").
Only step 4 may change rendered output.

## Context for the next agent

- Q1: the prefix lives in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs` (`KeptPrefix`, `Rendered`) and is duplicated in
  `src/Unshackle.DataAccess/Reports/ReportRow.cs` (`KeptPrefix`, `ReasonOf`). DataAccess may reference Core.
- Q2: the MediatR kept row is built in `src/Unshackle.Core/Fixes/MediatR/MediatROwners.cs` (~line 525) as a plain `PatternResult.Manual`.
  The new code needs a per-fix prefix ("Stays on MediatR next to Mediator: "), so the rendering cannot stay one AutoMapper-only constant.
  Rendered text must remain byte-identical.
- Q3: `MapBlockers.cs:56` keeps quoting `MapOwners.Rendered(manual)`. Only a test is added.
- Q4: `AddAutoMapperPattern.Rewrite` (~line 38) takes `FirstOrDefault` over `Compilation.SyntaxTrees`; `RewriteAsync` has its own lookup.
  Both must pick the first user ordered by file path (ordinal, normalize `\` and `/`), then line.
- Q5: no code change; the `ThrowIfNull` calls stay. Only `claude/docs/code-standaarden.md` §7 gets the entry-point wording.
- Corpus: `claude/tasks/CORPUSRUN-blind.md`, harness `corpus-am.sh after`; the last baseline is `<corpus>/corpus-blind/_results/after-t295`.
  For MediatR, use the MediatR corpus run (`claude/tasks/CORPUSRUN-mediatr.md`).
- Push procedure: `claude/docs/overdracht.md` "Push procedure". Never push with a failing test.

## Steps

- [ ] 1. Q1: `ReportRow` uses the Core rendering (`MapOwners.Rendered` or a shared Core helper); delete `ReportRow.KeptPrefix`. No test expectation changes.
- [ ] 2. Q2: inventory every branch on `Kept != None` / `KeptReason` (report grouping, counts, filters, summaries, `AllOrNothing`,
  `AllowsPartial`, partial plans) and record it here. Then add a MediatR `KeptReason` code with a per-fix prefix, rendered
  byte-identical. Add a test that pins the rendered MediatR text, plus one test per branch that proves it behaves exactly as it did when that row had `Kept == None` (same grouping, counts, filtering, `AllOrNothing` and partial plan result). No existing expectation may change.
- [ ] 3. Q3: test that the `MapBlockers` reason contains `Stays on AutoMapper: ` exactly once.
- [ ] 4. Q4: stable first IMapper user (path ordinal, separator-independent, then line) in `Rewrite` and `RewriteAsync`; add a test with
  two users in reverse tree order. **Changes rendered output**: the `IMapper is still used (<file>:<line>)` example in the kept
  `add-automapper` row. Test expectations that name that example (search the tests for `IMapper is still used`: `AddAutoMapperPattern`
  tests, `CombinedProofTests` partial-*, proofs `*.manual.txt`) are updated only where the chosen file changes; list them here.
- [ ] 5. Q5: add "fix constructors and public `IFix` members" to the entry-point list in `claude/docs/code-standaarden.md` §7.
- [ ] 6. Corpus rerun: `after` on the six AutoMapper repos and the MediatR repos. Steps 1–3 and 5 must give zero diff in every report
  output (rows, counts, grouping, summaries). Step 4 may only change the example in the kept `add-automapper` row, and it must then be
  identical across two runs. Fixed/Manual counts stay as in `after-t295`.
- [ ] 7. Build, `dotnet test`, SampleShop tests green; `claude/tools/verify-head.sh` → `VERIFY OK`; docs; archive the task.

## Progress log

_(filled in during execution)_

## See also

- `claude/docs/council.md` — round-6 decisions
- `claude/tasks/archief/TAAK-295-kept-reason-codes.md` — origin of Q1–Q4
- `claude/tasks/archief/TAAK-297-code-standards-cleanup.md` — origin of Q5
- `claude/docs/INDEX.md` — documentation index
