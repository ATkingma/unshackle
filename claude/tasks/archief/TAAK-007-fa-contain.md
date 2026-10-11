# TAAK-007 — Patroon fa.contain

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().Contain(item|predicate)` / `NotContain` wordt `ShouldContain` / `ShouldNotContain`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 5 in `claude/tasks/patroon-queue.md`. Voorbeeld: TAAK-006. Sample: `OrderTests.cs:81`, `OrderHandlerTests.cs:56-57`.

## Analyse

### Huidige situatie

FA 7.2.2: `Contain(T item | Expression<Func<T,bool>> | IEnumerable<T>, because, becauseArgs)` op `GenericCollectionAssertions`, `Contain(string, because, becauseArgs)` (+ occurrence-overload) op `StringAssertions`, `Contain(KeyValuePair/params)` en `Contain(key, value)` op dictionaries. Alle geven een `AndConstraint`.

### Probleem

Shouldly kent geen `becauseArgs`. Belangrijker: `string.ShouldContain(x)` is standaard **hoofdletterongevoelig**, FA is hoofdlettergevoelig (ontdekt met een proefprogramma).

### Nieuwe situatie

`ContainPattern` (ID `fa.contain`) herschrijft losse assertions; strings krijgen `Shouldly.Case.Sensitive`; de rest `Manual(reden)`.

### Verbetering

Eén patroon voor item, predicate en substring, `Contain` en `NotContain`, zonder stille semantiekwijziging.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | `NotContain` in hetzelfde pattern | Identieke logica op de naam na |
| 2 | Overload bepalen via `method.OriginalDefinition.Parameters[0].Type` (type-parameter / string / Expression = Fixed; `IEnumerable<T>` = Manual) | Onderscheidt item van alle-items ook als T zelf een IEnumerable is |
| 3 | String: `ShouldContain(x, Shouldly.Case.Sensitive[, because])` | Shouldly default is case-insensitive; geldt ook voor `ShouldNotContain`. Overload is `(string, Case, customMessage)`, dus `because` komt na `Case` |
| 4 | Dictionary altijd Manual | Contain(KVP/params/key,value) heeft geen eenduidige Shouldly-vorm; `ContainKey` is rij 11 |
| 5 | Occurrence-constraint (2 waarde-argumenten) Manual | Geen equivalent |
| 6 | Niet-generieke `IEnumerable`-subject Manual | Shouldly heeft alleen `IEnumerable<T>` |
| 7 | Manual-redenen: `chain`/`value-used` → "is used"; `and-chain` → "is not in the form"; `because-args` → "becauseArgs"; `dictionary` → "Dictionary:"; `items` → "several items"; `occurrence` → "occurrence constraint" | `ProofTests` eist één reden per proof |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (11 REQ)
- [x] 4. `ContainPattern` + registratie
- [x] 5. Proofs (28 bestanden in `Contain/`)
- [x] 6. Build/test groen (71 + 15 sample), verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/`
- `docs/projectinfo/patterns/fa-contain-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-006-fa-be-empty.md`
