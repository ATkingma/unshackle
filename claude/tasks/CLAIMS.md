# File claims — agent pool 2026-10-22

Live register of who owns which files. **Only the main session edits this file.** Agents read it (absolute path
`D:/playground/projects/unshackle/claude/tasks/CLAIMS.md`) before touching any file; a file not claimed by you is
read-only for you. Need another file? Put a `CLAIM REQUEST: <path> — <why>` line in your final report; the main session
grants it to you or your successor. Budget: 25 agents in total (user, 2026-10-22; was 50) across the roadmap; at most 3 CPU-heavy at once.

## Always main-session only

`claude/docs/INDEX.md`, `claude/docs/overdracht.md`, `claude/docs/council.md`, `docs/projectinfo/project.md`,
`docs/projectinfo/README.md`, `Directory.Packages.props`, `Unshackle.slnx`, `src/Unshackle.Interface/Cli.cs`,
`claude/tasks/POOL-2026-10-22.md`, this file.

## Pool checkpoint (2026-10-22, Linux, session ended) — resume here

Main `23e9cf29` is green. Each running lane's work is pushed to its own branch `pool/<lane>-wip` (never main); the
local worktrees under `.claude/worktrees/agent-<id>` hold the same commits. To resume a lane: in a new session either
`SendMessage` to the agent id (same machine, transcript kept) or start a new agent with `isolation: worktree`, tell it to
`git checkout -B <lane> origin/pool/<lane>-wip`, read its task file, `/taak-oppakken`, and continue. Merge to main one
lane at a time after `claude/tools/verify-head.sh <scratch> > log && git push` (no pipe) and a green CI.

| Lane | Agent id | Branch (origin) | Last commit | Done so far | Next |
| --- | --- | --- | --- | --- | --- |
| L1 polly (TAAK-308) | `aea372f23f1a4c394` (resumed 2026-10-22) | `pool/l1-polly-wip` | `b36d4d7f` | row 4 `polly.sync-retry` done | rows 8 circuit-breaker, 9 timeout, 11 no-op, 12 unsupported; last WIP commit may be half done: build + test first |
| L2 moq (TAAK-307) | `a1d1f9446e08e6453` (resumed 2026-10-22) | `pool/l2-moq-wip` | `2df82fa2` | row 9 `moq.class-mocks` done (only `CallBase = true` + `.Object`) | rows 11 setup-sequence, 12 logger-verify, 13 unsupported |
| L4 epplus-corpus (TAAK-309/310) | `a35be5d9a48f3449e` (resumed 2026-10-22) | `pool/l4-epplus-corpus-wip` | `9ea72a22` | EPPlus queue rows 7–10 verified done; CA1859 fix (also on main) | wire `IPackageAssetsReader` into `EPPlusFix` (E7, give the main session the `Cli.cs` line), C1 codes Layout/Dimension/LoadFrom → `EPPlusReason`, E14 combined after-proofs, corpus rounds EPPlus + MassTransit (`<corpus>/corpus-epplus`, `corpus-masstransit` exist) |
| L5 remeasure (TAAK-313–315) | paused (no agent) | `pool/l5-remeasure-wip` | `2dcc93b7` | re-measure started (ANALYSE-manual-round-2.md +16 lines, MediatR repos added); `<corpus>/corpus-mediatr` exists | finish the 22 scans (check `_results` first), then TAAK-313, 314, 315 |
| L6 corpus-prep (TAAK-307 row 14, 308 row 13) | paused (no agent) | `pool/l6-corpus-prep-wip` | `f07d3e2e` | clones exist under `<corpus>/corpus-moq/src`, `corpus-polly/src` (36 dirs) | baseline + `scan` null measurement per repo; `after` round only when L1/L2 are done |

Other open items: TAAK-305 ReadyToRun = council question (measured: ~1 s faster per run, package 2.3×, one package per RID; not built);
TAAK-304 release 0.3.0 needs the user's go; council round 13 questions in `claude/docs/overdracht.md` above.

## Active claims

Pool restarted 2026-10-22 on the Linux machine (max 5 agents; corpus under `<corpus>` on the data disk, never `/tmp`). Earlier claims 24b/22c/25 closed (their work is on main or on `pool/remeasure-wip`).

| # | Agent | Task | Files (owned, write) | Reserved task nrs | Since |
| --- | --- | --- | --- | --- | --- |
| L1 | polly | TAAK-308 rows 4, 8, 9, 11, 12 | `src/Unshackle.Core/Fixes/Polly/**`, Polly tests/proofs/docs, `claude/tasks/polly-queue.md`, `claude/tasks/TAAK-308-*.md` | 340–349 | 2026-10-22 |
| L2 | moq | TAAK-307 rows 9, 11, 12, 13 | `src/Unshackle.Core/Fixes/Moq/**`, Moq tests/proofs/docs, `claude/tasks/moq-queue.md`, `claude/tasks/TAAK-307-*.md` | 320–339 | 2026-10-22 |
| ~~L3~~ | engine | done: TAAK-322 step 4, TAAK-305 measured (not implemented, council question) | `src/Unshackle.Core/*.cs` (root), `Abstractions/**`, `Models/**`, `src/Unshackle.DataAccess/**`, `src/Unshackle.Tests/Core/Engine/**`, `docs/projectinfo/architectuur/core.md`, `claude/tasks/TAAK-322-*.md`, `TAAK-305-*.md`; packaging in `src/Unshackle.Interface/Unshackle.Interface.csproj` | — | 2026-10-22 |
| L4 | epplus-corpus | TAAK-309 queue status, then corpus rounds EPPlus + MassTransit | `src/Unshackle.Core/Fixes/EPPlus/**` (only bug fixes found by the corpus, with proof), EPPlus/MassTransit tests/proofs, `claude/tasks/epplus-queue.md`, `claude/tasks/TAAK-309-*.md`, `TAAK-310-*.md`, `claude/tasks/CORPUSRUN-epplus.md`, `CORPUSRUN-masstransit.md` (new), `claude/tools/corpus-epplus-*`, `corpus-masstransit-*` | 350–359, 360–369 | 2026-10-22 |
| L5 | remeasure | finish `pool/remeasure-wip`, then TAAK-313, 314, 315 | `src/Unshackle.Core/Fixes/AutoMapper/**`, `src/Unshackle.Core/Fixes/MediatR/**`, their tests/proofs/docs, `claude/tasks/ANALYSE-manual-round-2.md`, `claude/tasks/TAAK-313..315-*.md` | 311–319 | 2026-10-22 |
| L6 | corpus-prep | corpus rounds Moq (TAAK-307 row 14) and Polly (TAAK-308 row 13): clone + baseline now, `after` when L1/L2 finish | `claude/tasks/CORPUSRUN-moq.md`, `CORPUSRUN-polly.md` (new or existing), `claude/tools/corpus-moq-*`, `corpus-polly-*` | — | 2026-10-22 |
| — | main | merges, INDEX/overdracht/council, archive finished tasks (301, 302, 311, 312, 316–321), TAAK-303 | shared files | — | 2026-10-22 |

## Released claims

| Agent | Task | Result | Released |
| --- | --- | --- | --- |
| docs | TAAK-301 | done, merged 470c734 | 2026-10-22 |
| analysis | TAAK-306 st 1–2 | done, TAAK-311..315 created, merged 470c734 | 2026-10-22 |
| tooling | TAAK-302 | done, merged | 2026-10-22 |
| masstransit | TAAK-310 st 1–2 | done, waits for council | 2026-10-22 |
| scout-identityserver | TAAK-380 | no-go fix, go report-only (council) | 2026-10-22 |
| scout-specflow | TAAK-370 | conditional go (council) | 2026-10-22 |
| scout-imagesharp | TAAK-390 | conditional no-go (council) | 2026-10-22 |
| scout-newtonsoft | TAAK-420 | no-go blanket (council) | 2026-10-22 |
| scout-efbulk | TAAK-430 | conditional go narrow (council) | 2026-10-22 |
| scout-testfw | TAAK-440 | xUnit go / NUnit no-go (council) | 2026-10-22 |
| moq | TAAK-307 st 1 | done, council | 2026-10-22 |
| masstransit-detector | TAAK-310 st 4 | done: report-only detector, 9 patterns | 2026-10-22 |
| am-mocked-imapper | TAAK-311 | done: automapper.mock-set-up (A1); 0 Manual removed on blind repos, all blockers justified | 2026-10-22 |
| engine | TAAK-316, 317 | done: IReportOnlyFix, IProjectFileMigration | 2026-10-22 |
| mr-assembly-scan | TAAK-312 | done: AssemblyResolver (A2) | 2026-10-22 |
| epplus-skeleton | TAAK-309 st 2–3 | done: harness + license-context + package | 2026-10-22 |
| polly-skeleton | TAAK-308 st 4 | done: PollyFix + polly.wait-and-retry-async with run-time trace proof | 2026-10-22 |
| moq-skeleton | TAAK-307 st 3 | done: MoqFix + moq.create, queue rows 0–14 | 2026-10-22 |
| council (3) | round 8 | decided, recorded in council.md | 2026-10-22 |
| polly | TAAK-308 st 1–2 | done, council | 2026-10-22 |
| epplus | TAAK-309 st 1 | done (builds partial), council | 2026-10-22 |
| scout-swashbuckle | TAAK-410 | no-go for now (council) | 2026-10-22 |
| scout-pdf | TAAK-400 | iTextSharp slice conditional, rest no-go (council) | 2026-10-22 |

## Spawn log

Agents spawned: 28 / 28 (7 wave 1, 8 library scouts, 3 council round 8, 6 wave 2; +3 resume L1, L2, L4 on 2026-10-22, user-approved). L5 and L6 paused until the user approves more. Finished agents are reused for the next queue rows (no new spawn).

## See also

- `claude/tasks/POOL-2026-10-22.md` — waves and rules
- `claude/docs/agent-werkwijze.md` — task rules
