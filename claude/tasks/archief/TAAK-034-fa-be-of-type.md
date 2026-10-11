# TAAK-034 — Patroon fa.be-of-type

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `BeOfType<T>()`, `BeAssignableTo<T>()`, `NotBeOfType<T>()`, `NotBeAssignableTo<T>()` → Shouldly, inclusief `var v = ….Which`.

## Context voor de volgende agent

- Rij 17 in `claude/tasks/patroon-queue.md` (ronde 2). `.Which.P.Should()…` hoort bij rij 19 (`fa.which-chain`).

## Analyse

FA 7.2.2 (`ObjectAssertions`/`ReferenceTypeAssertions`): generiek en met `Type`, plus `because`/`becauseArgs`; `BeOfType<T>`/`BeAssignableTo<T>` geven `AndWhichConstraint<…, T>` (`.Which`/`.Subject`). Shouldly 4.3.0: `ShouldBeOfType<T>(object?, customMessage)` en `ShouldBeAssignableTo<T>` geven de cast-waarde terug; `Type`-overloads en `ShouldNot…` bestaan ook (proofs compileren ertegen).

## Keuzes

| #   | Keuze                                                                                   | Reden                                                                            |
| --- | --------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| 1   | Eén patroon voor alle vier, via een naam-tabel                                          | Zelfde vorm, alleen de naam verschilt                                            |
| 2   | Generieke naam behouden (`WithIdentifier`)                                              | Type-argumenten en trivia blijven gelijk                                         |
| 3   | Geen `NotNull(subject)`                                                                 | Shouldly accepteert `object?` en faalt zelf op null, zoals FA (proof `nullable`) |
| 4   | `.Which`/`.Subject` alleen als enige initializer van een declaratie                     | "Eenvoudig"; de rest is `fa.which-chain`                                         |
| 5   | `FluentAssertions.Types` → Manual                                                       | `typeof(T).Should().BeAssignableTo<U>()` controleert het type zelf               |
| 6   | AssertionScope, becauseArgs, `.And` → Manual (`.And.BeOfType` werkt via `fa.and-chain`) | Zelfde regels als de andere patronen                                             |

## Stappen

- [x] 1. Taak, onderzoek, keuzes
- [x] 2. KingmaDoc-plan (7 REQ)
- [x] 3. `BeOfTypePattern` + registratie (ook als schakel van `fa.and-chain`)
- [x] 4. Proofs (11 cases in `BeOfType/`)
- [x] 5. Build/test groen (412 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/`
- `docs/projectinfo/patterns/fa-be-of-type-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-033-fa-and-chain.md`
- `docs/projectinfo/patterns/fa-be-of-type-plan.md`
