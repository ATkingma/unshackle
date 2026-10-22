# TAAK-308 — Polly v7 → Microsoft.Extensions.Resilience (roadmap step 8)

**Status**: step 5/6 — steps 1–4 done; next: queue row 2 (`claude/tasks/polly-queue.md`)
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

| Claim | Verdict | Source |
| --- | --- | --- |
| Polly adopts the OSMF; US $20/month per organisation with >= US $20,000 revenue from a product using Polly | confirmed | [announcement 2026-07-14 (update 2026-10-12)](https://www.thepollyproject.org/2026/07/14/polly-osmf-announcement.html), [README](https://github.com/App-vNext/Polly/blob/main/README.md) |
| Applies only to the next major version released after 2026-11-16; existing versions (7.x/8.x) are not subject | confirmed | same |
| Only direct references are subject; Polly via `Microsoft.Extensions.(Http.)Resilience` is exempt | confirmed (named explicitly) | same; maintainer "That is my understanding, yes" in [discussion #3183](https://github.com/App-vNext/Polly/discussions/3183) |
| `Microsoft.Extensions.Http.Resilience` builds on Polly.Core | confirmed: M.E.Resilience 10.11.0 (MIT) -> Polly.Extensions + Polly.RateLimiting 8.4.2 | [nuget M.E.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Resilience), [nuget M.E.Http.Resilience](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience) |
| Licence unchanged | **conflicting**: blog update says "Polly's license is not changing"; in #3183 a maintainer said the licence changes with the new version | both above |
| Calling Polly APIs with only a transitive / CPM-pinned reference | **unanswered** in #3183 | #3183 |

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

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | new `Fixes/Polly/`, tests, docs |
| **How**   | New fix |
| **Why**   | Timely (fee from 2026-11-16) and a migration teams avoid by hand |

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
