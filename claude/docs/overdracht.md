# Handover (updated 2026-10-28)

Read this first if you are taking over this session. Then `CLAUDE.md` and `claude/docs/INDEX.md`.


## State on 2026-10-28 (switching to the user's PC) — start here

AI session: read `claude/docs/ai-handover-2026-10-28.md` first.

Main green (`ab8ae4d`, CI green). Merged 2026-10-27: TAAK-325, 327 (rest), 329, 335-338, 340 (slice 1, 3a-3f, 4a-4c), 341
(slice 1, 5a-5e), 342 (slice 1, steps 4-8b), 343, 344, 346/346b, 348, 349, 351, 352 (IProjectFileMigration async), 354
(Polly B5/B6), council rounds 16-21, AutoMapper and Polly corpus re-runs. Merge rule used: `verify-head.sh <dir>` without
pipe + green CI per branch; integration branches (`integ-1`, `integ-2`) where branches overlapped.

**Pushed, NOT on main yet (merge each after verify-head + green CI):**
- `taak-341c` — xUnit 5f + record-exception-async, type-name-attribute, extensibility (VERIFY OK on its own base).
- `council-22` — round 22 decisions (X10-X14, P11), docs only.
- `taak-355` — Polly kept row names the added package (P11); finished, verify state unknown (session ended).
- `taak-341d` — WIP steps 5g-5j (X10-X14), unverified.
- `efbulk-corpus` — WIP EF bulk corpus harness (TAAK-342 step 9), round not run to the end.
- `moq-rerun-wip` — WIP Moq corpus rerun (TAAK-330 step 5) harness change only; results not recorded.
- `epplus-corpus-wip` — WIP EPPlus corpus re-measure (TAAK-327 step 7).

**Next:** merge the branches above (order: taak-341c, council-22, taak-355, then finish taak-341d); rerun the three
corpus rounds (Moq, EF bulk, EPPlus) and the first SpecFlow/xUnit corpus rounds; regenerate `proofs.md` (LF);
update the roadmap table in `docs/projectinfo/project.md` (steps 11-13: SpecFlow→Reqnroll, xUnit v3, EF bulk);
TAAK-304 release 0.3.0 (user go). Local worktrees under `.claude/worktrees` are disposable: all work is on origin.

## State on 2026-10-27 (afternoon) — start here

Main green (CI per push, `verify-head.sh <scratch-dir-on-D:>` before each; C:-temp blocks the test exe). Merged today: TAAK-335
(Polly transient clause once), TAAK-327 rest (EPPlus flow fields/returns Manual, `epplus.address`), TAAK-325
(`automapper.generic-destination`, closure follows extension callers), council round 16 (`claude/tasks/COUNCIL-round-16.md`:
E20-E26, A15-A18, G10, P1). All old agent worktrees removed (3 unmerged `worktree-agent-*` branches kept).

**On main since:** TAAK-329, 338, 342 slice 1, council-18 (council 17/18, TAAK-336, 340, 341 slice 1), amr-remeasure,
council-19 (TAAK-337, 342b/c, 343). Merge per branch: `verify-head.sh D:/vh/<n>` + green CI.

**Integration branch `integ-1` (agent running):** merges taak-344, 348, 346, 346b, 341b, 349, 342d, 342e, 340b, council-20,
unifies the blocking-projects output (340b vs 341b) and renames the SpecFlow backlog task to TAAK-353. Merge it next.

**Agents running:** moq-rerun, polly-rerun (corpus rounds).

**Next:** TAAK-352 (IProjectFileMigration async, council X8) after integ-1; TAAK-351 done;
council round 21 on TAAK-342 Q13-14 and TAAK-340 Q9-11; TAAK-342 step 9 and TAAK-340/341 corpus rounds; EPPlus corpus
re-measure; regenerate `proofs.md` (LF); TAAK-304 release 0.3.0 (user go). Agent cap: 10 until 02:10, then 8.

## State on 2026-10-27 — start here

Main green (`b32cf79`+, CI green). Done: Polly B2 (`HandlerResultUsed`), B3 (`Exception!` in nullable onRetry), B1/B4
(project blocker `PackageDowngrade`, no ref bumps); Moq B2-B7 (`MockPassedAround` for out/deconstruction, `Arg.Is<T>(a => a == null)`,
`global::` on clashes, receiver members no longer keep a using, helper project keeps Moq when dependents use it);
`IProjectProperties` wired into `AutoMapperFix` (TAAK-324 closes sets in non-packable libraries).

**Next:** 1. Re-run Polly, Moq and AutoMapper corpus rounds to confirm (AutoMapper: letterbook, personalasst, skoruba have
generic-map rows; TAAK-324 step 6 and its verify corpus table still open). 2. Leftovers: Polly redundant `OrTransientHttpStatusCode`
clause; Moq B1, H1/H2, misleading kept-package text for the helper project (`Fixer.cs`), duplicate project-file rows, qualified
`Moq.Mock<T>` keeps the using. 3. TAAK-325, TAAK-327. 4. TAAK-304 release 0.3.0 (user go).

## State on 2026-10-26 (end of day, Windows) — start here

Main is green (CI per push, `verify-head.sh` before each). No agents running. Pool branches `pool/*` are merged except
`pool/taak-326-wip` (**do not merge**: it duplicates TAAK-326, which another session already finished on main, 17/30
EPPlus repos migrate per project; that branch only has ideas worth comparing: `EpplusTypeCrossesProject`, `samples/EPPlusSplit`).

Done today: L4 EPPlus/MassTransit corpus (0 bugs; E7 reader wired in `Cli.cs`), TAAK-323 (missing global.json SDK →
clean load failure), L5 re-measure (TAAK-313/314/315 already solved, archived), MediatR re-clone (7/9 repos migrated, 0
regressions), council round 13 (`claude/tasks/COUNCIL-round-13.md`), TAAK-328 (Moq per-result pattern ids,
`PatternResult.PatternId`), TAAK-324 (`automapper.generic-map`; 0 Manual→Fixed in the corpus because `AutoMapperFix` has
no `IProjectProperties`, so public members in library projects count as packable → R1 keeps the set open), TAAK-326 and
TAAK-329 (other session), Polly and Moq corpus rounds.

**Next (in order of value):**
1. Polly corpus bugs (`claude/tasks/CORPUSRUN-polly.md`): B1/B4 the Resilience package pulls newer
   Microsoft.Extensions.* → NU1605/NU1109/CS0433 in 6 repos (bump direct refs or Manual; relates to TAAK-329); B2
   `add-policy-handler` breaks when the builder result is used (CS0266); B3 nullable `Outcome<T>.Exception` (CS8600).
2. Moq corpus bugs (`claude/tasks/CORPUSRUN-moq.md`): B2 package removed from a shared test-helper project while
   referencing projects still use Moq; B3 `using Moq;` kept unused (IDE0005); B4/B5 mocks from `out var` / tuple
   deconstruction rewritten (must be Manual MockPassedAround); B6 emitted type names need `global::`; B7 `null` next to
   matchers → `Arg.Is<T>(null)` ambiguous. 8 Moq repos are not cloned on this machine.
3. Wire `IProjectProperties` into `AutoMapperFix` (`Cli.cs`) so TAAK-324 can close sets in non-packable libraries; re-measure.
4. TAAK-325 (`ProjectTo<T>`, council A12), TAAK-327 (EPPlus flow types, E18).
5. TAAK-304 release 0.3.0 (user go), go-candidates SpecFlow→Reqnroll, EF bulk, xUnit v3.

Cleanup: `tmp326` on the data disk (temp copy left by the TAAK-326 agent) can be deleted by hand.

## Pool checkpoint on Linux (2026-10-22, later) — start here first

Five lanes (L1 polly, L2 moq, L4 epplus-corpus, L5 remeasure, L6 corpus-prep) were running when the session ended. State,
agent ids, `pool/<lane>-wip` branches and next steps per lane: **`claude/tasks/CLAIMS.md`, "Pool checkpoint"**.
Done in this session: L3 engine (TAAK-322 step 4 in `core.md`; TAAK-305 measured, not built: council question), CA1859 false
positive on SDK 10.0.113 suppressed (`86ee846d`), tasks 301/302/311/312/316/317/320/321 archived, old worktrees removed.
`verify-head.sh` must be gated without a pipe (memory: push-gate-no-pipe). zsh note: write `${id}` before `:` in
refspecs (`$id:r` is a zsh modifier).

## Current state (2026-10-22, end of session) — start here

**Agent pool (POOL-2026-10-22) stopped for a device switch.** Main is green and pushed (`b4eb28b` + this handover; verify-head OK, 2244 tests + 38 SampleShop). 25/25 agents of the budget used; finished agents were reused per queue row.

On main now:
- **Moq** (TAAK-307): create, arg-matchers, setup-returns, verify, verify-all (M4), throws, properties (`moq.properties`), returns-lambda, callback; run-time harness (`Core/Moq/MoqRuntimeProofTests`); Moq default pins (round 9); NSubstitute 6.2.0. Next queue rows: 9 class-mocks, 11 setup-sequence, 12–13; then corpus round (main session). Harness found NSubstitute config-call side effects → `Configure()` for overlapping setups (council question open).
- **Polly** (TAAK-308): wait-and-retry-async (+filters, onRetry, private fields), retry-variants, add-policy-handler (+private method); P8/P9; run-time traces. **Corpus results before `c803b5a` are invalid** (pre-filter skipped every file). Next: row 10 Wrap (P4), row 4 sync, properties/parameters.
- **EPPlus** (TAAK-309): license-context, styles, layout, dimension, load-from, package (worksheets, cells, ranges, values E4, save, index base E10/E11), unsupported; cell-compare harness; E7 warning. All reads Manual (E12 matrix: no `GetValue<T>` qualifies). Next: E13/E15/E16 + full proofs for styles/layout/dimension/load-from (E14); E7 pin via TAAK-319 reader.
- **MassTransit** (TAAK-310): report-only detector (10 patterns, Info/Manual, T6–T8). Next: corpus run, roadmap row.
- **Engine**: TAAK-316 report-only, 317 project-file migrations, 318 round-10 rules, 319 assets reader, 320/321 blockers stop rewrites and package swap. In progress: TAAK-322 (pre-filter on assemblies G9, solution-level CPM G7, comment trivia G8).
- **AutoMapper/MediatR**: TAAK-311 (A1, A6–A10), 312 (A2); TAAK-313/314 wait for the corpus re-measure (started, see below).
- **Council** rounds 8–12 in `claude/docs/council.md`. Open questions for round 13: Moq per-result pattern id (M7 partly), `Configure()` in output; EPPlus read mapping keeping EPPlus semantics.

**Unmerged WIP branches pushed to origin** (`pool/*`, see `claude/tasks/CLAIMS.md` "Active claims"): merge each after build + tests + verify-head, or continue it. **Never clear `~/.nuget/packages` on the work laptop** (Profit); corpus runs use NuGet caches under `<corpus>/...`.

**Left for the main session**: corpus rounds (Moq, Polly, EPPlus, MassTransit) with NuGet cache on D:, TAAK-303 (worktree cleanup, after merging the pool branches), TAAK-305 (ReadyToRun), TAAK-304 release 0.3.0 (user go).

## Earlier state (2026-10-22, morning)

**TAAK-298 (performance) done and merged to main:** pre-filter per file (`OldLibraryFiles`, hidden `--check-every-file` to
turn it off), parallel files for FA and MediatR (`--sequential`, `--max-degree N`, default min(cores, 8)), AutoMapper
sequential. akka 547 s → 201 s, eShop 35 → 23 s, nop 86 → ~78 s. Corpus: 36/36 ok, all reports byte-identical to main
(`claude/tasks/CORPUSRUN-blind.md`, "TAAK-298"). Open: ReadyToRun tool packaging (see the task, step 2).

**Next (2026-10-22):** roadmap steps 7-10 chosen by the user (Moq, Polly, EPPlus, MassTransit; TAAK-307..310) and follow-ups
TAAK-301..306. A pool of sub-agents runs them per `claude/tasks/POOL-2026-10-22.md` (waves, file ownership, reserved numbers); live claims in `claude/tasks/CLAIMS.md` (budget 25 agents). TAAK-301 done (options in docs/usage.md §4 and README). TAAK-306 steps 1-2 done (`claude/tasks/ANALYSE-manual-round-2.md`); TAAK-311..315 wait for council round 8; the mediatr/automapper corpus reports predate TAAK-271, re-measure before TAAK-312..315. TAAK-302 done: verify-head and the pattern-docs hook need python3 or python, no jq. TAAK-310 steps 1-2 done, waits for council round 8 (OpenTransit unreleased, v8 end-of-2026 unverified). TAAK-307/308/309 surveys done (Moq 13 repos, Polly 20, EPPlus 42 with partial builds: re-run builds before step 3; Radarr in Moq too). C: ran full on 2026-10-22 from corpus NuGet restores: corpus agents use NUGET_PACKAGES/NUGET_HTTP_CACHE_PATH on D:, and never clear `~/.nuget/packages` (the user's Profit packages live there). Council round 8: `claude/tasks/COUNCIL-round-8.md`. Wave 2 merged: MoqFix (moq.create), PollyFix (polly.wait-and-retry-async, run-time trace proof), EPPlusFix + cell-compare harness (license-context, package; NOT registered in Cli.cs until queue row 11 `epplus.unsupported`), engine TAAK-316 (IReportOnlyFix) and TAAK-317 (IProjectFileMigration), TAAK-312 (assembly resolution; miru/starter rightly stay Manual). Council round 9 decided Moq defaults. Library scouts TAAK-370..440: all done (swashbuckle no-go for now); verdicts in `docs/projectinfo/architectuur/candidate-*.md`, council questions there. Go-candidates: SpecFlow→Reqnroll, EF bulk (narrow), xUnit v3; report-only: IdentityServer4; parked: ImageSharp, PDF, Newtonsoft.

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
