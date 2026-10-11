# TAAK-052 — Patroon fa.invoking

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Invoking(s => s.M()).Should().Throw<T>()` wordt `Should.Throw<T>(() => x.M())`; ook `Awaiting`/`ThrowAsync`, `ThrowExactly(Async)` en `NotThrow`.

## Context voor de volgende agent

- Rij 28 in `claude/tasks/patroon-queue.md` (ronde 2). Bouwt voort op TAAK-008/011/012 (Throw, NotThrow, ThrowAsync) en TAAK-047/048 (`ThrowMessage`).

## Analyse

Semantiek gecontroleerd met een probe-project (FA 7.2.2 + Shouldly 4.3.0) in de scratchpad (`probe/`). Probe (FA 7.2.2 + Shouldly 4.3.0, scratchpad `probe/`): Invoking/Awaiting + Throw/ThrowAsync/NotThrow geven dezelfde pass/fail als de Shouldly-vorm met `() => x.M()`; de actie draait één keer.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | Lambda-parameter vervangen door de receiver: `() => body[s := x]` | Leesbaarder dan een cast of een extra variabele; semantisch gelijk zolang x een stabiele waarde is |
| 2 | Alleen local, parameter, field of `this` als receiver | Invoking evalueert x één keer buiten de actie; een aanroep in de lambda zou een fout als verwachte exceptie kunnen tellen |
| 3 | Value-type receiver → Manual | Invoking werkt op een kopie, de lambda op de variabele |
| 4 | `InvokingPattern` claimt de nodes, de bestaande Throw-/ThrowAsync-/NotThrow-patterns herschrijven | Alle message-, because-, delegate- en aggregate-regels blijven op één plek; Finds sluiten elkaar uit |
| 5 | Async lambda in `Awaiting(async o => ...)`: geen proof | FA 7.2.2 geeft CS0121 (Task- en ValueTask-overload), zulke code compileert niet |

## Stappen

- [x] 1. Taak, onderzoek (probe), keuzes
- [x] 2. KingmaDoc-plan
- [x] 3. `InvokingPattern` + registratie in `s_assertions`
- [x] 4. Proofs in `Invoking/`
- [x] 5. Build/test groen (515 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` (nieuw), `ThrowPattern.cs`, `ThrowAsyncPattern.cs`, `NotThrowPattern.cs`, `ThrowMessage.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/` (oude Manual-proofs `Throw/throw-exactly`, `Throw/invoking`, `ThrowAsync/throw-exactly`, `ThrowAsync/awaiting`, `NotThrow/invoking` vervallen)
- `docs/projectinfo/patterns/fa-invoking-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-048-fa-throw-aggregate-exception.md`
- `docs/projectinfo/patterns/fa-invoking-plan.md`
