# TAAK-051 — Patroon fa.throw-exactly

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `act.Should().ThrowExactly<T>()` wordt `Should.Throw<T>(act).ShouldBeOfType<T>()`; `await act.Should().ThrowExactlyAsync<T>()` wordt `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()`; `.WithMessage` zoals `fa.throw`.

## Context voor de volgende agent

- Rij 23 in `claude/tasks/patroon-queue.md` (ronde 2). Bouwt voort op TAAK-008/011/012 (Throw, NotThrow, ThrowAsync) en TAAK-047/048 (`ThrowMessage`).

## Analyse

Semantiek gecontroleerd met een probe-project (FA 7.2.2 + Shouldly 4.3.0) in de scratchpad (`probe/`). Probe (FA 7.2.2 + Shouldly 4.3.0, scratchpad `probe/`): exact type, afgeleid type, basistype, geen exceptie en AggregateException geven bij beide dezelfde uitkomst (pass/fail), sync en async.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | `ShouldBeOfType<T>()` achter `Should.Throw<T>` | Probe: zelfde pass/fail als ThrowExactly voor exact, afgeleid, basis en geen exceptie; `ShouldBeOfType<T>` geeft `T` terug, dus `.Message` blijft werken |
| 2 | Geen AggregateException-check bij ThrowExactly | Probe: ThrowExactly pakt AggregateException niet uit (faalt), Shouldly ook niet; `MayThrowAggregate` alleen voor Throw<T> |
| 3 | Rewrite blijft in `ThrowPattern`/`ThrowAsyncPattern` (vlag `exactly`); `ThrowExactlyPattern` claimt alleen de nodes | Geen kopie van de message-, because- en delegate-regels; de Finds sluiten elkaar uit, dus geen dubbele meldingen |
| 4 | `because` alleen op `Should.Throw` | Kleinste vorm; de type-melding van Shouldly noemt het verschil zelf |

## Stappen

- [x] 1. Taak, onderzoek (probe), keuzes
- [x] 2. KingmaDoc-plan
- [x] 3. `ThrowExactlyPattern` + registratie in `s_assertions`
- [x] 4. Proofs in `ThrowExactly/`
- [x] 5. Build/test groen (515 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs` (nieuw), `ThrowPattern.cs`, `ThrowAsyncPattern.cs`, `NotThrowPattern.cs`, `ThrowMessage.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowExactly/` (oude Manual-proofs `Throw/throw-exactly`, `Throw/invoking`, `ThrowAsync/throw-exactly`, `ThrowAsync/awaiting`, `NotThrow/invoking` vervallen)
- `docs/projectinfo/patterns/fa-throw-exactly-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-048-fa-throw-aggregate-exception.md`
- `docs/projectinfo/patterns/fa-throw-exactly-plan.md`
