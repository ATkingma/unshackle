# TAAK-290 — MediatR: starter still blocked (`AddMediatR` + `InternalsVisibleTo`)

**Status**: ✅ done (principally manual; reason now names the real blocker)
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- TAAK-274 designed project-level partial migration for starter (two Mediator hosts + `InternalsVisibleTo` to a test project that calls `AddMediatR` → CS0121), but round 5 still shows starter blocked (0/11).
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- A first attempt on another machine was lost (its corpus rerun ended in `fix-fails`/`restore-fails` on all repos; logs gone). Redone from scratch.

## Findings (step 1)

starter (`youssefbennour/AspNetCore.Starter` @ `784d23e`) has two `AddMediatR` calls:

1. `Starter.Common/Events/Publisher/InMemory/InMemoryPublisherModule.cs`: `internal static AddInMemoryPublisher(services, Assembly assembly)` scans `assembly`.
   It is called by the **public** `PublisherModule.AddPublisher(services, Assembly assembly)`, which the host `Starter/Program.cs` calls with
   `Assembly.GetExecutingAssembly()`. The handlers live in `Starter`, the scan is only known at run time.
2. `Starter.IntegrationTests/.../ConfigurationExtensions.cs`: a test override scanning `typeof(FakeEvent).Assembly`.

The tool reported only the host clash (`Starter.Common grants InternalsVisibleTo to Starter.IntegrationTests ... CS0436, CS0121`).
That hid the real blocker: even without that `InternalsVisibleTo`, rewriting call 1 to `AddMediator` in `Starter.Common` would drop the scan,
and Mediator's source generator in `Starter.Common` cannot see `Starter`'s handlers (TAAK-144: the generator only sees its own compilation and
references). The existing check only caught a public method (`'…' of a public method`) or an internal one called from another project; a public
wrapper in the same project slipped through — a latent run-time break if the host clash ever goes away.

**Verdict: principally manual.** Migrating means moving the Mediator registration (and `Mediator.SourceGenerator`) out of the shared library into
the host `Starter`, changing the public API `AddPublisher(Assembly)` — a design decision for the user, not a mechanical rewrite. No library probe
needed: the generator's compile-time limit was measured in TAAK-144 and round 2 (MSG0007).

## Choices

- `AddMediatRPattern.RewriteAsync` follows the scanned parameter through call sites in the solution (`SymbolFinder`, by name or position,
  extension receiver skipped); when it reaches a parameter of a public method, the result is Manual naming that method.
- The host-clash reason (`MediatorHosts.SettingsAsync`) is now checked **after** the run-time scan checks, so the report names the real blocker.
- Proof: a solution-level unit test (`RewriteAsync_InternalScanFedByPublicParameter_ManualNamingThePublicMethod`), because file proofs have no
  second document; Fixed stays for a constant assembly and for a method without a public wrapper (proof `edge-assemblies` unchanged).

## Results

| Repo | Fixed / Manual before | after | Tests | Reason shown |
| --- | --- | --- | --- | --- |
| starter | 0 / 11 | 0 / 11 | 45/49 → 45/49 (no file written by `fix --force`) | before: IVT host clash; after: `comes from a parameter of the public method PublisherModule.AddPublisher` |

## Steps

- [x] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features (REQ-8 in `mediatr-add-mediatr-plan.md`)
- [x] 2. Implementation + unit tests (`AddMediatRPatternTests`, 3 cases)
- [x] 3. Corpus `after` on starter: `fix --force` writes 0 files, so build/tests are the baseline (45/49); 0 regressions. Harness not run (bash harness; nothing changes)
- [x] 4. Verify, models (`mediatr-add-mediatr-modellen.md` decision row), verify doc (REQ-8); `claude/tools/verify-head.sh`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-plan.md`, `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
