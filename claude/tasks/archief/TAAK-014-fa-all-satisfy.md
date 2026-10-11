# TAAK-014 — Patroon fa.all-satisfy

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().AllSatisfy(a => ...)` wordt `x.ShouldNotBeEmpty(); foreach (...)`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 13 in `claude/tasks/patroon-queue.md`. Sample: `OrderHandlerTests.cs` (`orders.Should().AllSatisfy(o => o.Total.Should().Be(20m))`).

## Analyse

### Huidige situatie

FA 7.2.2 `AllSatisfy(Action<T> expected, because, becauseArgs)`: voert per element een inspector met assertions uit en verzamelt alle fouten per index.

### Probleem

Shouldly's `ShouldAllBe` neemt een `Expression<Func<T,bool>>`: een lambda met assertions (void) past er niet in. Gemeten gedrag FA: faalt op een lege collectie en op `null`; `ShouldAllBe` slaagt op leeg.

### Nieuwe situatie

`AllSatisfyPattern` (ID `fa.all-satisfy`): statement `x.Should().AllSatisfy(lambda)` wordt `x.ShouldNotBeEmpty();` + `foreach (var a in x) { body }`.

### Verbetering

Sample wordt automatisch gemigreerd; de body-assertions worden door de andere patterns herschreven.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | `foreach` i.p.v. `ShouldAllBe` of `ShouldSatisfyAllConditions` | De body bevat assertions (void), geen bool-expressie; een bool-omzetting per assertion is onbetrouwbaar (`Be` is Equals, `BeEquivalentTo`, ...) |
| 2 | `x.ShouldNotBeEmpty();` ervoor | FA faalt op leeg/null (getest), foreach slaagt; zo blijft de semantiek gelijk |
| 3 | Subject moet naam of member access zijn (anders Manual "evaluate it twice") | Subject komt twee keer voor |
| 4 | Pattern staat **eerst** in `FluentAssertionsFix` | De `using Shouldly` komt pas na alle patterns; staat dit pattern achteraan, dan is de semantic model van de al herschreven body onbindbaar en vindt `Find` niets. Vooraan blijft de body FA en pakken ShouldBe e.a. hem als losse statements |
| 5 | `because` en `becauseArgs` Manual | Bericht kan niet in de loop; alleen de check zou het krijgen |
| 6 | Manual voor methodgroep, async lambda, `return` in body, parameter met modifier, niet in blok, resultaat gebruikt, `.And` | `return` zou de methode verlaten; de rest is niet als twee statements te schrijven |
| 7 | Getypeerde parameter behoudt zijn type, anders `var` | Zelfde types als de lambda |
| 8 | Blok-body wordt opnieuw laten inspringen via tekst (`Reindent`) en `ParseStatement` | Geen Formatter in Core; proofs vergelijken exacte tekst |
| 9 | Eigen `IsSimple` (kopie uit `BeGreaterLessPattern`) | Niet in andermans bestanden/helper-API ingrijpen |
| 10 | Foutmelding verschilt (eerste fout i.p.v. alle per index) | Gedocumenteerd in plan |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek (gedrag FA op leeg/null/meerdere fouten gemeten)
- [x] 3. KingmaDoc-plan (12 REQ)
- [x] 4. `AllSatisfyPattern` + registratie
- [x] 5. Proofs (`AllSatisfy/`, 13)
- [x] 6. Modellen, verify, archiveren, rij `klaar`

## Zie ook

- `claude/tasks/patroon-queue.md`
- `docs/projectinfo/patterns/fa-all-satisfy-plan.md`
