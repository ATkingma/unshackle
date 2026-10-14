# Handover (updated 2026-10-14)

Read this first if you are taking over this session. Then `CLAUDE.md` and `claude/docs/INDEX.md`.

## Status (2026-10-14, afternoon)

**FA is at 100%**: corpus round 5 on all 16 solutions, 0 regressions apart from the known Akka TAAK-200 warning (`claude/tasks/CORPUSRUN-fa.md`).
EventFlow needed TAAK-242 (keep an old using a Manual call only binds to after `using System;` is added) and TAAK-243 (`DateTime.At`).
CI on main green. Step 6 (SharpGrep) dropped from the roadmap by the user.

**AutoMapper (2026-10-14, 14:10): all pattern rows of `claude/tasks/automapper-queue.md` are done and pushed** (`8b06170`, CI green, 1407 tests):
nested maps, member list, conversions, profile shapes, reverse map, configuration test, constructor, value resolver, ProjectTo, ConvertUsing,
member options, flattening, unsupported (TAAK-245..257). The `/loop` was stopped by the user's time box (14:00).

**Next: row 16, the large AutoMapper corpus run.** TAAK-258 (baseline: harness in `<corpus>/corpus-automapper/tools/`, baseline tests of the 20 repos,
`claude/tasks/CORPUSRUN-automapper.md`) was still running at 14:10; check its results, then publish the tool from a clean worktree of HEAD,
run the after-fix round, fix regressions. Untested corners noted by agents: ProjectTo + value resolver/ConvertUsing (Manual), flattening inside a projection
(written without null checks, like AutoMapper's expression). Way of working today: up to 4 (briefly 8) agents in worktrees, merged one by one by the main session.

**Roadmap:** 1 Scan 100% · 2 FA 100% · 3 MediatR 100% · 4 Launch ~80% (together with the user after AutoMapper) · 5 AutoMapper ~85% (corpus run left).

**Corpus status:** FA round 5 full: 16/16 builds, 0 regressions (Akka: only TAAK-200).
MediatR round 6: jason, ardalis, amantin, nadirbad, eShopOnWeb, dotnet/eShop, kgrzybek migrated.

**User decisions today:** TAAK-200 → "Fixed with warning" for possibly asymmetric `Equals`. Chosen by agents themselves (present to the user when in doubt):
TAAK-220/221/222 → Manual (AggregateException unwrapping, cancelled task, meta-test on own helper); TAAK-214 → archtest difference legitimate.

**Way of working that worked:** parallel agents with strictly separated files (MediatR code / FA patterns / `Fixer.cs`+`UsingDirectives.cs`),
reserve task numbers per agent in advance, `TEMP`/`TMP` on a `D:` folder, never kill other people's dotnet processes, one commit+push after all agents.
## Local environment (Windows)

- This machine blocks starting `.exe` files from `%TEMP%` ("Access is denied"). The two `SampleShopTests` integration tests that build and test a fixed copy therefore fail locally.
  Run them with `TEMP`/`TMP` pointing to a folder outside `%TEMP%` (e.g. `TEMP=<map op D:> TMP=<map op D:> dotnet test`); in CI (Linux) this does not apply.
- `jq` is missing in Git Bash: `.claude/hooks/check-pattern-docs.sh` fails; do the check by hand (see the script).

## Push procedure

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

- Decision trees of some patterns have >12 arrows (not split).
- `.vibe-wise/` (learning notes) is in the repo; agree on this before making it public.
- Repo is private: GitHub does not enforce branch protection (no Pro).

## See also

- `claude/tasks/TAAK-019-lancering.md` — next step
- `claude/docs/patroon-loop.md` — pattern workflow
- `claude/docs/agent-werkwijze.md` — task management, §6 main stays green
