# TAAK-015 — Patroon fa.only-contain

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().OnlyContain(pred)` wordt `x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 12 in `claude/tasks/patroon-queue.md`. Mechanisme voor één statement → meerdere: `PatternResult.Fixed(IEnumerable<SyntaxNode>)`, zoals `BeGreaterLessPattern`. Sample: `OrderTests.cs:83`.

## Analyse

### Huidige situatie

FA 7.2.2 `GenericCollectionAssertions<T>.OnlyContain(Expression<Func<T,bool>>, because, becauseArgs)` geeft `AndConstraint`. Proefprogramma (scratchpad, FA 7.2.2 + Shouldly 4.3.0):

| Geval | FA `OnlyContain` | Shouldly `ShouldAllBe` |
|---|---|---|
| lege collectie | **faalt** ("but the collection is empty") | **slaagt** |
| alle items voldoen | slaagt | slaagt |
| item voldoet niet | faalt | faalt |
| `null` | assertion-fout | `ArgumentNullException` |

`ShouldNotBeEmpty()` is `void` en kent `customMessage`.

### Probleem

Een 1-op-1 `ShouldAllBe` laat een lege collectie slagen: stille semantiekwijziging.

### Nieuwe situatie

`OnlyContainPattern`: twee statements `x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);`; Manual waar dat niet kan.

### Verbetering

Sample `order.Lines.Should().OnlyContain(l => l.UnitPrice > 0)` migreert automatisch en behoudt het gedrag.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Extra `ShouldNotBeEmpty()`-statement, geen Manual voor alles | Behoudt FA-gedrag en houdt het patroon bruikbaar (sample) |
| 2 | Alleen als statement direct in een blok (of switch-sectie); anders Manual "cannot hold two statements" | Een lambda-body of `if` zonder accolades kan geen twee statements bevatten |
| 3 | Subject moet eenvoudig zijn (naam, `this`, ledenpad); anders Manual "evaluate the subject twice" | Splitsen evalueert het subject twee keer; zelfde regel als `fa.be-greater-less` |
| 4 | `because` op beide statements; `becauseArgs`/named args Manual | Zoals andere patronen |
| 5 | Predicaat ongewijzigd overgenomen | FA en Shouldly nemen beide `Expression<Func<T,bool>>`; delegate-predicaat bestaat bij FA niet |
| 6 | Subject moet generieke `IEnumerable<T>` zijn (lijst, array, dictionary slaagt) | Shouldly `ShouldAllBe` is generiek |
| 7 | Resultaat gebruikt / na `.And` Manual | Shouldly geeft niets terug |
| 8 | Manual-redenen: "is not in the form", "is used", "becauseArgs", "cannot hold two statements", "evaluate the subject twice" | `ProofTests` eist één reden per proof |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan
- [x] 4. `OnlyContainPattern` + registratie
- [x] 5. Proofs (`OnlyContain/`)
- [x] 6. Build/test groen, modellen, verify, archiveren, rij `klaar`

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-010-fa-be-greater-less.md`
