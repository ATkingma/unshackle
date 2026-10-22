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
| 24b | mr-assembly-scan → engine-2 | TAAK-318/319/320 done; now a DataAccess `IConfigKeySearch` implementation (EPPlus E11) | `src/Unshackle.Core/*.cs` (root), `src/Unshackle.Core/Abstractions/**`, `src/Unshackle.Core/Models/**`, `src/Unshackle.DataAccess/**`, `src/Unshackle.Tests/Core/Engine/**`, `src/Unshackle.Tests/DataAccess/**`, `docs/projectinfo/architectuur/core.md`, `claude/tasks/TAAK-318-*.md` | — | 2026-10-22 |
| 22c | masstransit-detector → epplus-styles | TAAK-309 row 8 | `src/Unshackle.Core/Fixes/EPPlus/Patterns/Styles*.cs` (new), `src/Unshackle.Tests/Core/EPPlus/Styles*` (new), `src/Unshackle.Tests/Proofs/EPPlus/Styles/**`, `docs/projectinfo/patterns/epplus-styles-*`; one line in `EPPlusFix.cs` (pattern list); granted after the fact: the pattern list in `EPPlusFixTests.cs`, the migrated-annotation skip in `UnsupportedPattern.cs`; row 9 `Layout*` done; granted: the row-9 skip in `UnsupportedPattern.cs`, additive freeze/row-height facts in `WorkbookSnapshot.cs`; next: rows 7 (`Dimension*`) and 10 (`LoadFrom*`) | 356–359 | 2026-10-22 |
| 25 | am-mt-followups | TAAK-311 (A6–A8), then TAAK-310 (T6/T7) | `src/Unshackle.Core/Fixes/AutoMapper/**`, AutoMapper tests/proofs/docs, `claude/tasks/TAAK-311-*.md`; then `src/Unshackle.Core/Fixes/MassTransit/**`, MassTransit tests/proofs/docs, `claude/tasks/TAAK-310-*.md` | — | 2026-10-22 |
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

Agents spawned: 25 / 25 (7 wave 1, 8 library scouts, 3 council round 8, 6 wave 2). Reserve: 1. Finished agents are reused for the next queue rows (no new spawn).

## See also

- `claude/tasks/POOL-2026-10-22.md` — waves and rules
- `claude/docs/agent-werkwijze.md` — task rules
