# TAAK-308 — Polly v7 → Microsoft.Extensions.Resilience (roadmap step 8)

**Status**: step 5 — queue rows 1-12 done (13 corpus open, main session); pool agent L1 finished rows 4, 8, 9, 11, 12
**Created**: 2026-10-22
**Goal**: A `polly` fix that rewrites Polly v7 policy code to v8 resilience pipelines via Microsoft.Extensions.Resilience / Http.Resilience, at 100% on a Polly corpus.

## Context for the next agent

- Why: Polly adopts the Open Source Maintenance Fee; it applies to the next major released after 2026-11-16 and only to direct references; Microsoft.Extensions.Http.Resilience (built on Polly.Core) does not trigger it (sources: thepollyproject.org announcement 2026-07-14, Polly README, GitHub discussion #3183). Re-check these facts first: they decide the target and the README wording.
- Value is twofold: off a direct Polly reference, and the v7 -> v8 API migration many teams postponed.
- Production code, less covered by tests than FA/Moq: be conservative, Manual whenever behaviour could shift (policy wrap order, `Execute` overloads with context, `PolicyRegistry`, `AddPolicyHandler`).
- Target decision for the council: `Microsoft.Extensions.Http.Resilience`/`Microsoft.Extensions.Resilience` (ResiliencePipeline) vs a fork such as Fences (BSD).
- Follow the AutoMapper route that worked: survey (`docs/projectinfo/architectuur/corpus-automapper.md` as the model), a pattern queue (`claude/tasks/automapper-queue.md` as the model), a KingmaDoc plan + verify per pattern in `docs/projectinfo/patterns/` and a README row, proofs in `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`, corpus rounds with the harness `claude/tools/corpus-am.sh` (`FIX=<id>`), then a blind round.
- Unit-test types come from real assemblies (last free version of the old library, pinned in `Directory.Packages.props`; check its license allows that, like AutoMapper 14 with `NoWarn`).
- Design choices (target library, what is Fixed vs Manual) go to the council (`claude/docs/council.md`). A pattern never throws; unsafe rewrites are `Manual(reason)`.
- User selection 2026-10-22: all four new libraries (Moq, Polly, EPPlus, MassTransit); order Moq -> Polly -> EPPlus -> MassTransit, each to 100% before the next.

## Steps

- [x] 1. Verify the OSMF facts and the transitive-dependency claim from primary sources; record in the task
- [x] 2. Survey: repos with Polly v7 (`Policy.Handle<>().WaitAndRetryAsync`, `AddPolicyHandler`, `PolicyWrap`), API frequency; `docs/projectinfo/architectuur/corpus-polly.md`
- [x] 3. Council: target library and the Fixed/Manual line (round 8, P1–P7)
- [x] 4. `PollyFix` skeleton with proofs; `claude/tasks/polly-queue.md`
- [ ] 5. Patterns from the queue, each with docs and proofs
- [ ] 6. Corpus round to 100%, `claude/tasks/CORPUSRUN-polly.md`; blind round; roadmap row and README

## Step 1 — verified facts (2026-10-22)

| Claim                                                                                                         | Verdict                                                                                                                                  | Source                                                                                                                                                                                   |
| ------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Polly adopts the OSMF; US $20/month per organisation with >= US $20,000 revenue from a product using Polly    | confirmed                                                                                                                                | [announcement 2026-07-14 (update 2026-10-12)](https://www.thepollyproject.org/2026/07/14/polly-osmf-announcement.html), [README](https://github.com/App-vNext/Polly/blob/main/README.md) |
| Applies only to the next major version released after 2026-11-16; existing versions (7.x/8.x) are not subject | confirmed                                                                                                                                | same                                                                                                                                                                                     |
| Only direct references are subject; Polly via `Microsoft.Extensions.(Http.)Resilience` is exempt              | confirmed (named explicitly)                                                                                                             | same; maintainer "That is my understanding, yes" in [discussion #3183](https://github.com/App-vNext/Polly/discussions/3183)                                                              |
| `Microsoft.Extensions.Http.Resilience` builds on Polly.Core                                                   | confirmed: M.E.Resilience 10.11.0 (MIT) -> Polly.Extensions + Polly.RateLimiting 8.4.2                                                   | [nuget M.E.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Resilience), [nuget M.E.Http.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) |
| Licence unchanged                                                                                             | **conflicting**: blog update says "Polly's license is not changing"; in #3183 a maintainer said the licence changes with the new version | both above                                                                                                                                                                               |
| Calling Polly APIs with only a transitive / CPM-pinned reference                                              | **unanswered** in #3183                                                                                                                  | #3183                                                                                                                                                                                    |

Consequence: the fix must remove direct `Polly`, `Polly.Core`, `Polly.Extensions.Http`, `Microsoft.Extensions.Http.Polly`
references (MEHP itself depends on Polly 7) and the v7 API; staying on Polly 8.x is fee-free today, so the selling point is
"future majors + v7 API debt", not "fee from 16 Nov" on current versions. README wording should say that.

## Progress log

- 2026-10-22 (agent polly): step 1 facts verified (table above). Step 2 survey: 38 candidates cloned, 20 kept,
  `docs/projectinfo/architectuur/corpus-polly.md`, `claude/tools/corpus-polly-repos.txt`; builds in
  `D:/playground/corpus-polly/logs/summary.txt`. Draft queue and council questions in the agent report.
- 2026-10-22 (agent polly-skeleton): step 4. `PollyFix` (id `polly`; removes `Polly`, `Polly.Extensions.Http`,
  `Microsoft.Extensions.Http.Polly`, adds `Microsoft.Extensions.Resilience` 10.11.0; v7 code left by a Manual row binds to
  `Polly` and so keeps the package per project). Queue row 1 `polly.wait-and-retry-async` with 7 proofs and
  `PollyBehaviourProofTests`: before (Polly 7.2.4) and after (Polly.Core) are compiled and run, traces must be equal, and a
  mutation without `AttemptNumber + 1` must differ. Manual texts lead with a `PollyReason` code. Queue in `claude/tasks/polly-queue.md`.
  Not yet registered in `Cli.cs`/README (main session).
- 2026-10-22 (polly-skeleton): queue row 2 folded into `polly.wait-and-retry-async` (`PollyPredicate`: Handle/HandleResult/
  Policy<T>/Or/OrResult chains → PredicateBuilder(<T>), generic pipelines; HandleInner and value forms Manual). Also council
  P9 (`PollyTargetFramework`, Manual `TargetFrameworkUnsupported`) and P10 (`FreshName`: args1.. after a Roslyn lookup). 1812/1812 green.
- 2026-10-22 (polly-skeleton): queue row 3 (`PollyOnRetry`): onRetry/onRetryAsync, positional or named, mapped 1:1 into a local
  function (council P3); a Context read is Manual `OnRetryUsesContext`. Arguments are now matched by parameter name.
- 2026-10-22 (polly-skeleton): queue row 7, new pattern `polly.retry-variants` (`RetryAsync`, `RetryForeverAsync`,
  `WaitAndRetryForeverAsync`, `WaitAndRetryAsync(literal durations)`); `PollyRetry.IsVariant` splits calls between the two
  patterns. 1836/1836 green. Open: council P8 (direct Polly.Core removal) needs package-level code outside this claim.
- 2026-10-22 (polly-skeleton): C1 (ManualResult.Code), P9 (`PollyFix.ProjectBlockers`, per-rewrite check kept until TAAK-320),
  P8 (`PollyFix.EditsFor`: add M.E.Resilience, remove Polly.Core <= 8.4.2). Row 5 first slice: private fields (REQ-15 / REQ-7).
- 2026-10-22 (polly-skeleton): P9 only via ProjectBlockers (TAAK-320); found that the pre-filter skipped every file
  (RemovedNamespaces lacked Polly) -> `FindsOnlyOldLibraryCode => false`. Row 6 first slice: `polly.add-policy-handler`.
  Row 5 slice 2: constructor-assigned private fields (REQ-16).
  Row 6 slice 2 (handler policy method), row 10 `polly.wrap` (retry operands, P4 order proofs). 2240/2240 green.
  STOPPED here (session end). Next: row 4 (sync Retry/Execute), not started; README row for polly.wrap pending (main).
- 2026-10-22 (pool L1): row 4 polly.sync-retry done: shared PollyRetry, Execute kept as written (v8 has Execute(Action) and Execute<T>(Func<T>)); 4 Fixed proofs with equal traces, 2 Manual (Context overload, policy used beyond Execute), unit tests, docs.

- 2026-10-22 (pool L1): rows 9, 11 done: PollyRetry also holds Timeout and NoOp; the token overload `ExecuteAsync(async ct => ..., token)` only for timeouts; 8 Fixed proofs with equal traces. Row 8 is Manual-only: probe (corpus folder probe-l1) shows the sequence F S F F F F breaks v7 at call 6 but not v8 with FailureRatio 1.0 / MinimumThroughput 3 (test `CircuitBreaker_V8RatioMapping_BreaksOnDifferentCalls`); v8 also needs BreakDuration >= 500 ms and MinimumThroughput >= 2. Timeout range: v8 rejected 5 ms, 0, 30 days and infinite in the probe, so Fixed only for 100 ms..1 day. Row 12: Manual reason codes (Cache and Contrib Backoff have no proof). Council question: circuit breaker by hand-chosen v8 mapping or Manual.

## Changes

| Field     | Content                                                          |
| --------- | ---------------------------------------------------------------- |
| **Where** | new `Fixes/Polly/`, tests, docs                                  |
| **How**   | New fix                                                          |
| **Why**   | Timely (fee from 2026-11-16) and a migration teams avoid by hand |

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
