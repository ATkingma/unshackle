# TAAK-285 — MediatR: `AddMediatR(Action<MediatRServiceConfiguration>)` overload

**Status**: step 4/4 — verify
**Branch**: worktree-agent-ade469fda0960cfbe (worktree `.claude/worktrees/agent-ade469fda0960cfbe`)
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- projectpulse (round 5, 0 Fixed / 176 Manual) stops because the `AddMediatR(Action<MediatRServiceConfiguration>)` overload is not matched; one registration blocks the whole solution.
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- **Step 1 finding**: the overload is NOT the blocker. `AddMediatR(Action<MediatRServiceConfiguration>)` is the only `AddMediatR` overload in MediatR 12.x and `AddMediatRPattern` already matches it; scanning projectpulse after converting `src/API/Program.cs` to UTF-8 gives 175 Fixed / 0 Manual. The real cause: `Program.cs` is cp1252 (an em dash in a comment), `Fixer` skipped it as not UTF-8 (TAAK-167), and as it has no `using MediatR` no `encoding` row was written, so the report said "unmatched-use".
- **Step 2 (done)**: non-UTF-8 single-byte `.cs` files are loaded as Latin-1 (`MsBuildSolutionStore.MarkNotUtf8FilesAsync`, now also for a Windows ANSI code page) and written back byte-exact by `XmlProjectFileStore.SaveAsync` (Latin-1 maps bytes 1:1; encoder fallback throws, so nothing is lost silently). `Fixer` still skips UTF-16 and similar. Tests: `SaveAsync_SourceFileNotUtf8_WritesLatin1BackByteExact`, `FixAsync_FileInLatin1_IsMigratedLikeAnyOther`, integration `Fix_FileInLatin1_MigratesItAndKeepsItsBytes` (was `…LeavesItAndMigratesTheRest`), proof `AddMediatR/edge-assemblies-typeof`.
- Tool for the corpus: `<corpus>/probe285/tool1` (published from this branch).
- **Step 3 (done)**: projectpulse before 0 Fixed / 176 Manual, 618/686 tests; after 78 Fixed / 0 Manual, builds, 618/686, **0 regressions**. The first run (175 Fixed) had 101 regressions from two `mediatr.send-as-task` defects in Moq expression trees (`.AsTask()` inside `Setup(m => m.Send(..))`; `Returns(Task.FromResult(x))` / `Returns(Task.FromResult(Unit.Value))` not moved to ValueTask), both fixed with proofs `SendAsTask/expression-tree` and `SendAsTask/moq-setup-returns` + unit test, REQ-7/REQ-8 in `mediatr-send-as-task-plan.md`. Other 11 MediatR corpus repos: 0 regressions, unchanged numbers. Results in `<corpus>/corpus-blind/_results/t285{,b,c,d,e,f}`; summary in CORPUSRUN-blind.md.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

## Approach

Measure the overload forms in MediatR 12.5.0 (`RegisterServicesFromAssembly*`, `AddOpenBehavior`, `AddBehavior`, `Lifetime`, `NotificationPublisher`…) and map each to `AddMediator(options => …)` only where Mediator behaves the same; unknown options stay Manual with the option named in the reason.

## Steps

- [x] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [x] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [x] 3. Corpus `after` on projectpulse (and the earlier MediatR corpus repos): build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [→] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
