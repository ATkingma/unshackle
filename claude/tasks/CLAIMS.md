# File claims — agent pool 2026-10-22

Live register of who owns which files. **Only the main session edits this file.** Agents read it (absolute path
`D:/playground/projects/unshackle/claude/tasks/CLAIMS.md`) before touching any file; a file not claimed by you is
read-only for you. Need another file? Put a `CLAIM REQUEST: <path> — <why>` line in your final report; the main session
grants it to you or your successor. Budget: 25 agents in total (user, 2026-10-22; was 50) across the roadmap; at most 3 CPU-heavy at once.

## Always main-session only

`claude/docs/INDEX.md`, `claude/docs/overdracht.md`, `claude/docs/council.md`, `docs/projectinfo/project.md`,
`docs/projectinfo/README.md`, `Directory.Packages.props`, `Unshackle.slnx`, `src/Unshackle.Interface/Cli.cs`,
`claude/tasks/POOL-2026-10-22.md`, this file.

## Active claims

| # | Agent | Task | Files (owned, write) | Reserved task nrs | Since |
| --- | --- | --- | --- | --- | --- |
| 20 | polly-skeleton | TAAK-308 st 4 | `src/Unshackle.Core/Fixes/Polly/**`, `src/Unshackle.Tests/Core/Polly/**`, `src/Unshackle.Tests/Proofs/Polly/**`, `docs/projectinfo/patterns/polly-*`, `claude/tasks/polly-queue.md` (new), `claude/tasks/TAAK-308-*.md` | 340–349 | 2026-10-22 |
| 21 | epplus-skeleton | TAAK-309 st 2–3 | `src/Unshackle.Core/Fixes/EPPlus/**`, `src/Unshackle.Tests/Core/EPPlus/**` (incl. cell-compare harness), `src/Unshackle.Tests/Proofs/EPPlus/**`, `docs/projectinfo/patterns/epplus-*`, `claude/tasks/epplus-queue.md` (new), `claude/tasks/TAAK-309-*.md` | 350–359 | 2026-10-22 |
| 22 | engine | TAAK-316, then 317 | `src/Unshackle.Core/*.cs` (root files: Fixer, FileFixer, ProjectPackages, …), `src/Unshackle.Core/Abstractions/**` (additive only), `src/Unshackle.Core/Models/**` (additive only), `src/Unshackle.DataAccess/**`, `src/Unshackle.Tests/Core/Engine/**` (new), `src/Unshackle.Tests/DataAccess/**`, `docs/projectinfo/architectuur/core.md`, `claude/tasks/TAAK-316-*.md`, `claude/tasks/TAAK-317-*.md` | 318–319 | 2026-10-22 |
| 23 | am-mocked-imapper | TAAK-311 | `src/Unshackle.Core/Fixes/AutoMapper/**`, existing AutoMapper-only test files in `src/Unshackle.Tests/Core/` + new ones in `src/Unshackle.Tests/Core/AutoMapper/**`, `src/Unshackle.Tests/Proofs/AutoMapper/**`, `docs/projectinfo/patterns/automapper-*`, `claude/tasks/TAAK-311-*.md` | — | 2026-10-22 |
| 24 | mr-assembly-scan | TAAK-312 | `src/Unshackle.Core/Fixes/MediatR/**`, existing MediatR-only test files in `src/Unshackle.Tests/Core/` + new ones in `src/Unshackle.Tests/Core/MediatR/**`, `src/Unshackle.Tests/Proofs/MediatR/**`, `docs/projectinfo/patterns/mediatr-*`, `claude/tasks/TAAK-312-*.md` | — | 2026-10-22 |
| — | main | TAAK-303, 305 st 1 | git branches/worktrees, `claude/tasks/TAAK-303-*.md`, `claude/tasks/TAAK-305-*.md` | — | 2026-10-22 |

Shared grant (2026-10-22): `src/Unshackle.Tests/ProofTests.cs` — moq-, polly- and epplus-skeleton may each add only their own `s_fixes` entry; the main session resolves merge conflicts.

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
| moq-skeleton | TAAK-307 st 3 | done: MoqFix + moq.create, queue rows 0–14 | 2026-10-22 |
| council (3) | round 8 | decided, recorded in council.md | 2026-10-22 |
| polly | TAAK-308 st 1–2 | done, council | 2026-10-22 |
| epplus | TAAK-309 st 1 | done (builds partial), council | 2026-10-22 |
| scout-swashbuckle | TAAK-410 | no-go for now (council) | 2026-10-22 |
| scout-pdf | TAAK-400 | iTextSharp slice conditional, rest no-go (council) | 2026-10-22 |

## Spawn log

Agents spawned: 24 / 25 (7 wave 1, 8 library scouts, 3 council round 8, 6 wave 2). Reserve: 1 for the MassTransit detector after TAAK-316.

## See also

- `claude/tasks/POOL-2026-10-22.md` — waves and rules
- `claude/docs/agent-werkwijze.md` — task rules
