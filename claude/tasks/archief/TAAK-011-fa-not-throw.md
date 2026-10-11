# TAAK-011 — Patroon fa.not-throw

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `act.Should().NotThrow()` wordt `Should.NotThrow(act)`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 8 in `claude/tasks/patroon-queue.md`. Sample: `MappingTests.cs:14`. Voorbeeld: TAAK-008 (`fa.throw`).

## Analyse

FA 7.2.2: `NotThrow(because, args)` op `ActionAssertions`/`FunctionAssertions<T>` (`.Which` = waarde), `NotThrow<T>`, `NotThrowAfter`. Shouldly 4.3.0: `Should.NotThrow(Action[, customMessage])` en generieke `NotThrow<T>(Func<T>[, customMessage])`.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Subject: elke parameterloze `Action`/`Func<T>`, ook waardetypen | Shouldly's `NotThrow<T>(Func<T>)` is generiek (anders dan `Throw`) |
| 2 | `NotThrow<T>()` → Manual | FA verbiedt alleen type T; Shouldly verbiedt elke exception |
| 3 | `NotThrowAfter` → Manual | Wacht en pollt; geen equivalent |
| 4 | Resultaat gebruikt (`.Which`, `.And`, variabele) → Manual | Shouldly geeft de waarde terug, maar de vorm verschilt; later mogelijk |
| 5 | `Invoking`, becauseArgs → Manual | Zelfde als `fa.throw` |
| 6 | `NotThrowAsync` buiten scope; `Func<Task>.Should().NotThrow()` compileert niet in FA | Async hoort bij de async-patronen; geen guard nodig |
| 7 | `ThrowPattern` onaangeroerd; kleine duplicatie van subject-controle | Opdracht; gedeelde helper kan later |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (8 REQ)
- [x] 4. `NotThrowPattern` + registratie
- [x] 5. Proofs (10 cases in `NotThrow/`)
- [x] 6. Build/test groen (108 + 15 sample), verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs` (nieuw), `FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/`
- `docs/projectinfo/patterns/fa-not-throw-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-008-fa-throw.md`
