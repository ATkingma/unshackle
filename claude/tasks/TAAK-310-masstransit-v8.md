# TAAK-310 — MassTransit v8 → open-source bus (roadmap step 10)

**Status**: step 4/4 — report-only detector built; corpus round in progress (L4), see `CORPUSRUN-masstransit.md`
**Created**: 2026-10-22
**Goal**: A council decision on whether and how Unshackle migrates MassTransit v8 code, and if yes a `masstransit` fix at 100% on a corpus.

## Context for the next agent

- Why: MassTransit v9 is commercial; v8 stays Apache 2.0 with support ending (reportedly) end of 2026. Alternatives: Wolverine, Rebus, Brighter, NServiceBus (commercial), OpenTransit (v8 community fork).
- Hardest of the four: transport configuration, sagas, consumer definitions, retry/outbox config are semantics, not syntax. A fork with the same API (OpenTransit) could make it a near-mechanical package/namespace swap; a move to Wolverine/Rebus is mostly Manual.
- Start with a feasibility study; do not commit to a full fix before the council decides.
- Follow the AutoMapper route that worked: survey (`docs/projectinfo/architectuur/corpus-automapper.md` as the model), a pattern queue (`claude/tasks/automapper-queue.md` as the model), a KingmaDoc plan + verify per pattern in `docs/projectinfo/patterns/` and a README row, proofs in `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`, corpus rounds with the harness `claude/tools/corpus-am.sh` (`FIX=<id>`), then a blind round.
- Unit-test types come from real assemblies (last free version of the old library, pinned in `Directory.Packages.props`; check its license allows that, like AutoMapper 14 with `NoWarn`).
- Design choices (target library, what is Fixed vs Manual) go to the council (`claude/docs/council.md`). A pattern never throws; unsafe rewrites are `Manual(reason)`.
- User selection 2026-10-22: all four new libraries (Moq, Polly, EPPlus, MassTransit); order Moq -> Polly -> EPPlus -> MassTransit, each to 100% before the next.

## Steps

- [x] 1. Feasibility: OpenTransit (status, API compatibility, license, maintainers) and the API distance to Wolverine/Rebus for consumers, publish/send, request/response, sagas, DI registration
- [x] 2. Survey: repos with MassTransit 8 and the features they use; `docs/projectinfo/architectuur/corpus-masstransit.md`
- [x] 3. Council: round 8 T1–T5 — a report-only detector now; a package swap to OpenTransit only once it ships on NuGet; re-check Q1 2027
- [ ] 4. Report-only `MassTransitFix`: 9 detector patterns, proofs and KingmaDoc docs done 2026-10-22; open: Cli registration, README rows, corpus round over the 24 repos, roadmap row

- [x] 5. Council round 11 follow-ups (agent am-mt-followups, branch worktree-agent-a04c16b800e2dab3f): C1 typed codes (`MassTransitCode`,
  one per detector, per-feature codes for consumers); T7 consumer classification (Info only for context.Message + cancellation with no
  definition or filter in the solution; table in `docs/projectinfo/patterns/masstransit-consumer-plan.md`); T6 new pattern
  `masstransit.publish-send` (Info, resolved MassTransit receiver types only). Proofs `Consumer/consumer` (now headers),
  `consumer-respond`, `consumer-publish`; 15 new test cases in `MassTransitFixTests`. Open: README/patterns.md rows, Info proof kind.

- [x] 6. Council round 12 T8: the simple-consumer Info row says "Filters registered outside the solution are not seen".

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | new `Fixes/MassTransit/` (if go), docs |
| **How**   | Feasibility, then new fix |
| **Why**   | Big demand, but only worth building if the rewrite can be safe |

## Progress log

- 2026-10-22 (agent masstransit): steps 1–2 done in `docs/projectinfo/architectuur/corpus-masstransit.md` (OpenTransit unreleased, API-identical fork; v8 end-of-2026 date only from a third-party source; 24 repos surveyed source-level, list in `claude/tools/corpus-masstransit-repos.txt`). Council questions listed there.
- 2026-10-22 (agent engine): step 4 detector. `MassTransitFix` (`masstransit`, `IReportOnlyFix`, no package) in `src/Unshackle.Core/Fixes/MassTransit/`, 9 `DetectorPattern` subclasses (registration, transport, outbox, retry, test-harness, consumer, consumer-definition, saga, request-client), each Manual with OpenTransit/Wolverine/Rebus guidance. Types: MassTransit 8.5.12. 9 manual proofs, `Core/MassTransit/MassTransitFixTests` (5), docs `docs/projectinfo/patterns/masstransit-*`. Full suite 1817 green.

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
