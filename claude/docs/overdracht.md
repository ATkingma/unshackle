# Handover (updated 2026-10-19)

Read this first if you are taking over this session. Then `CLAUDE.md` and `claude/docs/INDEX.md`.

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

**Open user decisions:**

- starter and fullcart: accept as principally manual (starter: run-time assembly scan via the public `PublisherModule.AddPublisher(Assembly)`; fullcart: Scrutor `Decorate` of an open generic `INotificationHandler<>`, partial: requests on Mediator, notifications on MediatR).
- `automapper.unused-imapper`: keep removing the `GetService<IMapper>` null check (Fixed with warning) or make it Manual.
- `automapper.unused-imapper`: remove the empty constructors / factory classes it leaves behind, or leave them.

Release 0.2.0 is proposed to the user only after these decisions.

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
