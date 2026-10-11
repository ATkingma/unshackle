# TAAK-036 — Patroon fa.contain-single-which

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `ContainSingle()` / `ContainSingle(pred)` en eenvoudige `var y = ….Which` → Shouldly.

## Context voor de volgende agent

- Rij 18 in `claude/tasks/patroon-queue.md` (ronde 2). `.Which.P.Should()…` blijft Manual en hoort bij rij 19 (`fa.which-chain`).

## Analyse

FA 7.2.2 `GenericCollectionAssertions`: `ContainSingle(because, becauseArgs)` en `ContainSingle(Expression<Func<T,bool>>, because, becauseArgs)`, beide `AndWhichConstraint` (`.Which` = het item). Faalt bij 0 of meer dan 1 (match). Shouldly 4.3.0 (reflectie): `ShouldHaveSingleItem<T>(IEnumerable<T>, customMessage)` geeft T terug; `ShouldContain<T>(IEnumerable<T>, Expression<Func<T,bool>>, int expectedCount, customMessage)` faalt als het aantal matches niet precies `expectedCount` is.

## Keuzes

| #   | Keuze                                                                                 | Reden                                                                                               |
| --- | ------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| 1   | Statement met predicate → `ShouldContain(pred, 1)`                                    | Neemt dezelfde `Expression`, dus ook een Expression-variabele werkt; zelfde semantiek (precies één) |
| 2   | `.Which` met predicate → `x.Where(pred).ShouldHaveSingleItem()` + `using System.Linq` | Alleen deze vorm geeft het item terug                                                               |
| 3   | `.Which` met een Expression-variabele → Manual                                        | `Enumerable.Where` wil een `Func`; `.Compile()` invoegen is te verrassend                           |
| 4   | `.Which` alleen als enige initializer van een declaratie                              | "Eenvoudig", zoals `fa.be-of-type`; rest is `fa.which-chain`                                        |
| 5   | `NotNull(subject)` bij MaybeNull                                                      | Zelfde aanpak als andere collectie-patronen; voorkomt nullable-warnings                             |
| 6   | Subject zonder `IEnumerable<T>` → Manual                                              | Shouldly heeft geen niet-generieke variant                                                          |
| 7   | AssertionScope, becauseArgs, `.And` → Manual                                          | Zelfde regels als andere patronen                                                                   |

## Stappen

- [x] 1. Taak, onderzoek, keuzes
- [x] 2. KingmaDoc-plan (6 REQ)
- [x] 3. `ContainSinglePattern` + registratie in `s_assertions`
- [x] 4. Proofs (10 cases in `ContainSingle/`)
- [x] 5. Build/test groen (432 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/`
- `docs/projectinfo/patterns/fa-contain-single-which-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/archief/TAAK-035-fa-string-start-end.md`
- `docs/projectinfo/patterns/fa-contain-single-which-plan.md`
