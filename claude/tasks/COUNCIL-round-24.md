# Council round 24 — MassTransit v8 rewrite to an open-source bus

Date: 2026-10-28. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. Three separate sub-agents
gave positions on the facts in `docs/projectinfo/architectuur/candidate-masstransit-rewrite.md`. All three chose the same option per
question; the only nuance (P: a simple consumer might be Fixed together with its registration) is moot because registration is
principally Manual, and P accepted Manual. Trigger lists were merged. No second round was needed.
Follow-ups: TAAK-310 (re-check date; detector guidance text "pin MassTransit < 9").

| # | Question | Votes (G / P / A) | Decision |
| --- | --- | --- | --- |
| R1 | Rewrite now (Q24.1) | no-go / no-go / no-go | **No-go** for a rewrite to another bus; report-only stays (round 8 T1). |
| R2 | Target (Q24.2) | v8 + fork / v8 + fork / v8 + fork | **Stay on 8.5.x (Apache-2.0)**; later a package-ID swap to an API-identical fork on NuGet. Wolverine/Rebus/Brighter are user choices, never a default. Detector guidance says "pin MassTransit < 9; 9.x is commercial". |
| R3 | Fixed patterns (Q24.3) | none / none / none | **Nothing is Fixed toward another bus.** Toward a fork, everything is Fixed at package level only, after a test pins the API diff against 8.5.x. |
| R4 | Re-check (Q24.4) | triggers / triggers / triggers | Re-check on the first of: OpenTransit (or another fork) ships on NuGet; MassTransit announces end of v8 patches; an unpatched 8.x CVE; no 8.x patch for 6 months; otherwise 2027-01-15. |
| R5 | Partial rewrite (Q24.5) | no / no / no | **No.** Consumers without working registration compile but receive nothing; under AllOrNothing the old package stays anyway (T3). |

## Arguments

- G: every target changes delivery semantics (Wolverine drops a publish without subscribers, Rebus needs Subscribe, Brighter forces `IRequest` and mappers); the fork may arrive late and suddenly, so the report must be ready.
- P: 98 of 129 consumers sit behind filters, plus 72 request clients and 66 transport rows: a rewrite leaves nearly all hand work and a false "migrated"; a precise report saves more.
- A: only the fork swap is predictable from the report (same API, other package); keep the 8.5.x test types (T5) and pin fork API drift with a test.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-23.md`
- `docs/projectinfo/architectuur/candidate-masstransit-rewrite.md`
