# TAAK-050 — Patroon fa.be-approximately

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `BeApproximately(x, tol)` / `NotBeApproximately` en `BeCloseTo` / `NotBeCloseTo` (DateTime, DateTimeOffset, TimeSpan) → Shouldly met tolerance. Dekt ook rij 25 (`fa.be-close-to`).

## Context voor de volgende agent

- Rij 24 in `claude/tasks/patroon-queue.md` (ronde 2).

## Analyse

FA 7.2.2 (reflectie): `NumericAssertionsExtensions.(Not)BeApproximately` voor double/float/decimal (+ nullable subject en nullable expected), `(Not)BeCloseTo` voor integers (unsigned delta), `DateTime(Offset)Assertions` en `SimpleTimeSpanAssertions.(Not)BeCloseTo(…, TimeSpan)`, ook TimeOnly en ExecutionTime. Shouldly 4.3.0: `ShouldBe(double|float|decimal, …, tolerance, customMessage)`, `ShouldBe/ShouldNotBe(DateTime|DateTimeOffset|TimeSpan, …, TimeSpan, customMessage)`; geen numerieke `ShouldNotBe` met tolerance en geen integer-tolerance. Semantiek gecontroleerd met een probe-project (FA 7.2.2 + Shouldly 4.3.0) in de scratchpad (`semprobe3/`). Grens: FA en Shouldly slagen allebei bij `|x - y| == tol` (`<=`), voor double, decimal en DateTime; `Not…` faalt op de grens bij beide. Verschil: `+inf` vs `+inf` slaagt bij FA, faalt bij Shouldly; NaN faalt bij beide (FA met ArgumentException).

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | `NotBeApproximately` → `System.Math.Abs(x - y).ShouldBeGreaterThan(tol)` | Geen Shouldly-API; probe bevestigt dezelfde grens; `System.` gekwalificeerd omdat gebruikerscode een eigen `Math` kan hebben |
| 2 | `BeCloseTo` mee in dit patroon | Zelfde vorm (`ShouldBe(expected, tolerance)`); rij 25 daarmee ook klaar |
| 3 | `Nullable<T>`-subject → `ShouldNotBeNull().ShouldBe(..)` | Shouldly's tolerance-overloads nemen de niet-nullable waarde; FA faalt ook op null |
| 4 | `Not…` op nullable subject → Manual | FA's null-gedrag bij `Not…` wijkt af per type; liever expliciet |
| 5 | Integer/TimeOnly/ExecutionTime en afwijkend expected-type → Manual | Geen Shouldly-equivalent / overload-resolutie niet zeker |

## Stappen

- [x] 1. Taak, onderzoek (reflectie + semantiek-probe), keuzes
- [x] 2. KingmaDoc-plan
- [x] 3. `BeApproximatelyPattern` + registratie in `s_assertions` (ook schakel van `fa.and-chain`)
- [x] 4. Proofs in `BeApproximately/` + `AndChain/equal-length-approximately`
- [x] 5. Build/test groen (474 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeApproximately/`
- `docs/projectinfo/patterns/fa-be-approximately-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-035-fa-string-start-end.md`
- `docs/projectinfo/patterns/fa-be-approximately-plan.md`
