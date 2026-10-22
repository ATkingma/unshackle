# Council round 8 — questions (2026-10-22)

Input for the decision council (`claude/docs/council.md`). Each group points to a document whose **council questions**
section holds the numbered questions with context; answer every question by its id (e.g. `M3`). The main session
records the joint decisions in `council.md`.

| Group | Ids | Source (read its council-questions section and the facts above it) |
| --- | --- | --- |
| Moq → NSubstitute (TAAK-307) | M1–M5 | `docs/projectinfo/architectuur/corpus-moq.md` |
| Polly v7 → Resilience (TAAK-308) | P1–P7 | `docs/projectinfo/architectuur/corpus-polly.md`, `claude/tasks/TAAK-308-polly-to-resilience.md` (facts table) |
| EPPlus → ClosedXML (TAAK-309) | E1–E6 | `docs/projectinfo/architectuur/corpus-epplus.md` |
| MassTransit v8 (TAAK-310) | T1–T5 | `docs/projectinfo/architectuur/corpus-masstransit.md` |
| Manual reduction (TAAK-311..315) | A1–A5 | `claude/tasks/ANALYSE-manual-round-2.md`, `claude/tasks/TAAK-311..315-*.md` |
| New libraries (scouts TAAK-370..440) | S1–S4 | `docs/projectinfo/architectuur/candidate-*.md` (8 files) |

## Scout questions (S)

- **S1. Scope.** Unshackle's mission is license/sponsor/EOL problems. Which of these triggers are in scope: EOL/abandoned (SpecFlow, IdentityServer4), forced version upgrade without license issue (xUnit v3), preference migration (Swashbuckle, Newtonsoft)?
- **S2. Report-only fixes.** Is a fix whose patterns are all `Manual(reason)` (IdentityServer4 detector) an acceptable kind of fix?
- **S3. Non-C# edits.** SpecFlow (specflow.json, csproj, packages.config) and xUnit v3 (OutputType, package renames) need edits outside Roslyn documents. Allowed for the Fixer?
- **S4. Order.** Given the user's order Moq → Polly → EPPlus → MassTransit, which scouted candidates (if any) enter the roadmap, and in what order after step 10?

## See also

- `claude/docs/council.md` — members, procedure, earlier decisions
- `claude/tasks/POOL-2026-10-22.md` — wave 2 starts after this round
