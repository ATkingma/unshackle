# TAAK-308 — Polly v7 → Microsoft.Extensions.Resilience (roadmap step 8)

**Status**: step 1/6 — not started
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

- [ ] 1. Verify the OSMF facts and the transitive-dependency claim from primary sources; record in the task
- [ ] 2. Survey: repos with Polly v7 (`Policy.Handle<>().WaitAndRetryAsync`, `AddPolicyHandler`, `PolicyWrap`), API frequency; `docs/projectinfo/architectuur/corpus-polly.md`
- [ ] 3. Council: target library and the Fixed/Manual line
- [ ] 4. `PollyFix` skeleton with proofs; `claude/tasks/polly-queue.md`
- [ ] 5. Patterns from the queue, each with docs and proofs
- [ ] 6. Corpus round to 100%, `claude/tasks/CORPUSRUN-polly.md`; blind round; roadmap row and README

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
