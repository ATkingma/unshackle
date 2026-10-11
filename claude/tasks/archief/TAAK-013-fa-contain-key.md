# TAAK-013 — Patroon fa.contain-key

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `dict.Should().ContainKey(k)` / `NotContainKey(k)` wordt `ShouldContainKey` / `ShouldNotContainKey`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 11 in `claude/tasks/patroon-queue.md`. Voorbeeld: TAAK-007 (`ContainPattern` laat dictionaries Manual). Sample: `RepositoryTests.cs:17` (`IReadOnlyDictionary`).

## Analyse

### Huidige situatie

FA 7.2.2 `GenericDictionaryAssertions`: `ContainKey(key, because, becauseArgs)` → `AndWhichConstraint` (`.WhoseValue`), `NotContainKey`, `ContainKeys(params)`, `ContainValue(s)`, `NotContainValue(s)`, `Contain(key, value)`.

### Probleem

Shouldly 4.3.0 kent alleen `ShouldContainKey`, `ShouldNotContainKey`, `ShouldContainKeyAndValue`, `ShouldNotContainValueForKey`: geen `ShouldContainValue`, geen meervoud, geen resultaat.

### Nieuwe situatie

`ContainKeyPattern` (ID `fa.contain-key`): losse `ContainKey`/`NotContainKey` met één sleutel (+ `because`) → Fixed; alle andere vormen Manual.

### Verbetering

Sample `repository.All.Should().ContainKey(id)` wordt automatisch gemigreerd, ook voor `IReadOnlyDictionary`.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Eén pattern voor `ContainKey` en `NotContainKey` | Identieke logica |
| 2 | Geen typecontrole op het subject; de compileercheck bewijst `Dictionary`, `IDictionary`, `IReadOnlyDictionary` | FA biedt de methoden alleen op dictionaries |
| 3 | `ContainKeys`/`NotContainKeys` Manual ("several keys") | Meerdere statements = ander aantal statements; buiten scope van een expressie-rewrite |
| 4 | `ContainValue(s)`/`NotContainValue(s)` Manual ("has no Shouldly equivalent") | Shouldly heeft geen waarde-assertie los van sleutel |
| 5 | `Contain(key, value)` blijft in `ContainPattern` (Manual) | Niet aanraken; `ShouldContainKeyAndValue` is een eigen patroon |
| 6 | `.WhoseValue`/`.And`/variabele (resultaat gebruikt) Manual | Shouldly geeft niets terug |
| 7 | `becauseArgs`/named args Manual; `because` → `customMessage` | Zoals andere patronen |
| 8 | Manual-redenen: "is used", "is not in the form", "becauseArgs", "several keys", "has no Shouldly equivalent" | `ProofTests` eist één reden per proof |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan
- [x] 4. `ContainKeyPattern` + registratie
- [x] 5. Proofs (`ContainKey/`)
- [x] 6. Build/test groen, verify, archiveren, rij `klaar`

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-007-fa-contain.md`
