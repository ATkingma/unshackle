# Handover (updated 2026-10-22)

Read this first if you are taking over this session. Then `CLAUDE.md` and `claude/docs/INDEX.md`.

## Current state (2026-10-22) — start here

**TAAK-298 (performance) done and merged to main:** pre-filter per file (`OldLibraryFiles`, hidden `--check-every-file` to
turn it off), parallel files for FA and MediatR (`--sequential`, `--max-degree N`, default min(cores, 8)), AutoMapper
sequential. akka 547 s → 201 s, eShop 35 → 23 s, nop 86 → ~78 s. Corpus: 36/36 ok, all reports byte-identical to main
(`claude/tasks/CORPUSRUN-blind.md`, "TAAK-298"). Open: ReadyToRun tool packaging (see the task, step 2).

## State on 2026-10-21 (end of day)

**Done today, all on main with CI green:** TAAK-297 (code standards), TAAK-295 (typed kept reason codes), council round 6 +
TAAK-300 (one kept-reason rendering in Core, MediatR kept code, stable IMapper example, §7 entry points), TAAK-299 (split of
`CreateMapPattern.cs` and `Fixer.cs`; `FileFixer` is the per-file seam), TAAK-298 steps 1–2 (load check parallel per project,
package check on the original compilation: 6–20% faster, output byte-identical). TAAK-276 archived (closed by 0.2.0).

**(Superseded 2026-10-22: TAAK-298 merged, see above.) Was in progress: TAAK-298 steps 3–7 on branch `taak-298-perf-wip`.** State at `d8d6c23`: steps 1–5 done (per-file
pre-filter, council round 7 in `claude/docs/council.md`, parallel file processing for FA and MediatR with ordered merge,
AutoMapper stays sequential). Build, 1660 tests green; akka 548 s → 340 s at degree 8 (peak memory 1.29×), reports
byte-identical. Open: the pre-filter skips almost nothing and costs nop/AutoMapper ~30 s (cause unknown), then step 6 (corpus
round, 36 repos) and step 7. The task file on that branch (`claude/tasks/archief/TAAK-298-performance.md`) has the progress log and
measurements; continue from there (`git checkout taak-298-perf-wip`), and merge to main only after the corpus round and
`verify-head.sh` → `VERIFY OK`. The branch may have newer commits than `d8d6c23`: the agent kept pushing milestones.

**Left for the user:** TAAK-019 step 8 (post the reddit draft).

## Status (2026-10-14, afternoon)

**FA is at 100%**: corpus round 5 on all 16 solutions, 0 regressions apart from the known Akka TAAK-200 warning (`claude/tasks/CORPUSRUN-fa.md`).
EventFlow needed TAAK-242 (keep an old using a Manual call only binds to after `using System;` is added) and TAAK-243 (`DateTime.At`).
CI on main green. Step 6 (SharpGrep) dropped from the roadmap by the user.

**AutoMapper (2026-10-14, 14:10): all pattern rows of `claude/tasks/automapper-queue.md` are done and pushed** (`8b06170`, CI green, 1407 tests):
nested maps, member list, conversions, profile shapes, reverse map, configuration test, constructor, value resolver, ProjectTo, ConvertUsing,
member options, flattening, unsupported (TAAK-245..257). The `/loop` was stopped by the user's time box (14:00).

**Corpus work after the patterns (2026-10-14 afternoon), all pushed (`a903203`, CI green, 1430 tests):** TAAK-258 (baseline: harness
`claude/tools/corpus-am.sh` (Windows original: `<corpus>/corpus-automapper/tools/`), 14 of 18 repos with a usable baseline, 7851 tests; bitwarden/gnomeshade/lancommander/ombi
do not build (npm/cargo/yarn); `claude/tasks/CORPUSRUN-automapper.md`), TAAK-259 (`Map` on collections, maybe-null sources), TAAK-261
(destination members without a source left out like AutoMapper), TAAK-260 (Map/ProjectTo reasons name the real blocker).

**AutoMapper is at 100% (2026-10-15):** TAAK-267 (`Map<T>` as method group, the eshoponweb regression) on main `d496488`, CI green, 1495 tests.
After-fix round TAAK-258 complete on Windows (corpus `<corpus>/corpus-automapper/`, harness `tools/corpus-am.sh` there, the Linux port in
`claude/tools/corpus-am.sh`): all 14 usable repos 0 regressions, eshoponweb fully migrated (4 Fixed). See `claude/tasks/CORPUSRUN-automapper.md`.
Remaining Manual is the all-or-nothing rule per solution plus root blockers in user code (duplicate maps, private constructors, cycles).

**Next (2026-10-17):** manual reduction with proof, `claude/tasks/manual-reduction-queue.md` (TAAK-271..276).

**Done (2026-10-17 evening), all on main (`5c3fb4b`, CI green, 1535 tests):** TAAK-272 (AutoMapper partial migration per map pair), TAAK-274 (MediatR partial migration next to Mediator), TAAK-275 (custom FA assertion classes), TAAK-278 (single `global using Shouldly`). No agents running. Next open: TAAK-273, 276, 277 (`claude/tasks/manual-reduction-queue.md`).

**Released (2026-10-17):** Unshackle 0.1.0 on nuget.org (TAAK-019). Left: the user posts the reddit draft. Repo public; branch protection on `main` (admins push directly, others via PR with green "Build and test").
Way of working: 2–3 sub-agents on effort low, each in a worktree; the main session merges one by one, then worktree check + push.

**Roadmap:** 1 Scan 100% · 2 FA 100% · 3 MediatR 100% · 4 Launch ~80% (together with the user after AutoMapper) · 5 AutoMapper 100%.

**Corpus status:** FA round 5 full: 16/16 builds, 0 regressions (Akka: only TAAK-200).
MediatR round 6: jason, ardalis, amantin, nadirbad, eShopOnWeb, dotnet/eShop, kgrzybek migrated.

**User decisions today:** TAAK-200 → "Fixed with warning" for possibly asymmetric `Equals`. Chosen by agents themselves (present to the user when in doubt):
TAAK-220/221/222 → Manual (AggregateException unwrapping, cancelled task, meta-test on own helper); TAAK-214 → archtest difference legitimate.

**Way of working that worked:** parallel agents with strictly separated files (MediatR code / FA patterns / `Fixer.cs`+`UsingDirectives.cs`),
reserve task numbers per agent in advance, `TEMP`/`TMP` on a `D:` folder, never kill other people's dotnet processes, one commit+push after all agents.

## Round 6 blockers (done 2026-10-19)

TAAK-288 (RequestHandlerDelegate lambdas and Moq mocks, qualified MediatR types), TAAK-289 (`automapper.unused-imapper`, `AddAutoMapper` dropped) and TAAK-290 (starter) are done on main. They were redone from scratch on 2026-10-19: the WIP branches were lost on another PC. TAAK-291 (models rendered, decision trees split to at most 12 arrows) is done.

**Former open decisions:** all settled by the council on 2026-10-19 (the user made the council the decision maker; see `claude/docs/council.md`): starter Manual, fullcart partial, scoped `GetService<IMapper>` removal, empty classes kept with an Info line. Release 0.2.0 waits only for the user's go to publish (external action).

## Council (2026-10-19)

Open choices go to the decision council (`claude/docs/council.md`), at the user's request. First decisions: starter Manual, fullcart partial, scoped `GetService<IMapper>` removal, empty classes kept with an Info line. Follow-up: TAAK-292 (done: scoped lookup rule, Info lines, 0 corpus regressions).
Round 3 (implementer questions): TAAK-293 done (typed `KeptReason` on the kept lookup, target-typed containers, deferred registrations, Scrutor reason for fullcart; 0 corpus regressions).
Round 4: TAAK-294 done (builder types matched on the qualified name incl. `HostApplicationBuilder` and the isolated Functions builders; `Decorate` named as Scrutor's only when it resolves to Scrutor's namespace or assembly, or not at all; 0 corpus regressions). TAAK-295 done (typed `KeptReason` on every kept AutoMapper finding, `AllowsPartial` only on the code, `IsContainer` qualified; output unchanged; open council Q1-Q4 in the task).
Round 5 condition: TAAK-296 done (blind rerun of all 36 repos on main `0f58486`: 36/36 build, 0 test regressions; go for 0.2.0 on that condition).

## Local environment (Windows, obsolete: the corpus work moved to Linux, TAAK-262)

- This machine blocks starting `.exe` files from `%TEMP%` ("Access is denied"). The two `SampleShopTests` integration tests that build and test a fixed copy therefore fail locally.
  Run them with `TEMP`/`TMP` pointing to a folder outside `%TEMP%` (e.g. `TEMP=<map op D:> TMP=<map op D:> dotnet test`); in CI (Linux) this does not apply.
- `jq` is missing in Git Bash: `.claude/hooks/check-pattern-docs.sh` fails; do the check by hand (see the script).

## Push procedure

**Use `claude/tools/verify-head.sh <scratch-dir> > <log> 2>&1 && git push origin main` — never put a pipe (`| tail`) between the script and `&&`: the pipe's exit code wins and the push goes out on a red run (happened twice on 2026-10-18).** Earlier wording: use `claude/tools/verify-head.sh <scratch-dir>` and push only with `&&` after it (it stops on the first failing exit code; never judge by grepped output, which hides the exit code: on 2026-10-18 a push went out with 1 failing (flaky) test that way).

Tasks to `archief/` → `git add -A` → commit → checks on the commit in a separate worktree, checked out with LF like CI
(`git -c core.autocrlf=false worktree add --detach <map buiten %TEMP%> HEAD`; with the global `autocrlf=true`, 15 tests fail on CRLF) (`dotnet build`, `dotnet test`,
`dotnet test --solution samples/SampleShop/SampleShop.slnx`, docs check) → everything green → `git push origin main`. Anything red → do not push, report it
(`claude/docs/agent-werkwijze.md` §6). Before every commit, grep for local paths (playground folder, user folder, `AppData`); write `<probe-dir>/…` or `<corpus>/…`.

## Working agreements with the user (do not forget)

- Speak Dutch; code, names and commits in English. **No Claude mention** in commits or PRs.
- **Never push to `main` with a failing test.** Pushing after every finished piece of work is a standing instruction, always after the worktree check.
- Every pattern: task (`/taak-specifiek`) → KingmaDoc plan → build + proofs → `<slug>-modellen.md` (class, sequence, decision tree) → verify → update general models → push. See `claude/docs/patroon-loop.md`.
- In a loop: after a finished pattern, start the next one **immediately**; do not wait for the cron.
- Working autonomously with agents is desired; give a short report after each agent. Always ask first before far-reaching external actions (making the repo public, publishing to NuGet, posting).
- Code standards: `claude/docs/code-standaarden.md` (e.g. never `sealed`, classic constructors, `CancellationToken` required). Design patterns: GoF/refactoring.guru.
- Visual models (C4, UML) are appreciated: see `docs/projectinfo/`.

## Known open issues

- Some pattern models are tables only (no figures were drawn when d2 was missing), e.g. most MediatR patterns since TAAK-27x and several AutoMapper/FA ones (TAAK-291 open point).
- `.vibe-wise/` (learning notes) is local only: removed from the index and in `.gitignore` (2026-10-15); still in the git history.
- Repo is private: GitHub does not enforce branch protection (no Pro).

## See also

- `claude/tasks/TAAK-019-lancering.md` — next step
- `claude/docs/patroon-loop.md` — pattern workflow
- `claude/docs/agent-werkwijze.md` — task management, §6 main stays green
