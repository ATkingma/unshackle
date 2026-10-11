# TAAK-039 — Patroon fa.have-length

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: string `s.Should().HaveLength(n)` → `s.Length.ShouldBe(n)`.

## Context voor de volgende agent

- Rij 26 in `claude/tasks/patroon-queue.md` (ronde 2).

## Analyse

FA 7.2.2: `StringAssertions.HaveLength(int, because, becauseArgs)`, `StreamAssertions.HaveLength(long, …)`. Semantiek gecontroleerd met een probe-project (FA 7.2.2 + Shouldly 4.3.0) in de scratchpad (`semprobe3/`). Null-string faalt bij FA en bij `ShouldNotBeNull()`.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | `FluentAssertionsSyntax.NotNull` voor het subject | Zoals `fa.have-count` (TAAK-021): nullable subject krijgt `ShouldNotBeNull()`, geen CS8602 |
| 2 | Samengesteld subject tussen haakjes; `HaveCountPattern.IsSafeReceiver` hergebruikt (nu `internal`) | Zelfde regel, geen kopie |
| 3 | Stream en `?.`-subject → Manual | Stream heeft een `long`-lengte en geen string-semantiek; `?.` maakt `Length` nullable |

## Stappen

- [x] 1. Taak, onderzoek (reflectie + semantiek-probe), keuzes
- [x] 2. KingmaDoc-plan
- [x] 3. `HaveLengthPattern` + registratie in `s_assertions` (ook schakel van `fa.and-chain`)
- [x] 4. Proofs in `HaveLength/` + `AndChain/equal-length-approximately`
- [x] 5. Build/test groen (474 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/`
- `docs/projectinfo/patterns/fa-have-length-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-035-fa-string-start-end.md`
- `docs/projectinfo/patterns/fa-have-length-plan.md`
