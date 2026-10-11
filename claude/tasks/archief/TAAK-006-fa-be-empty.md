# TAAK-006 — Patroon fa.be-empty

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().BeEmpty()` / `NotBeEmpty()` wordt `ShouldBeEmpty()` / `ShouldNotBeEmpty()`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 4 in `claude/tasks/patroon-queue.md`. Voorbeeld: TAAK-004 (zelfde vorm).
- Sample-voorkomens: `OrderTests.cs:82` (`Lines.Should().NotBeEmpty()`), `:92` (`Lines.Should().BeEmpty()`), `:93` (`CustomerName.Should().BeEmpty()`, string).

## Analyse

### Huidige situatie

FA 7.2.2: `BeEmpty(because, becauseArgs)` / `NotBeEmpty(...)` bestaan op `StringAssertions`, `GenericCollectionAssertions<T>` (ook dictionaries, arrays) en geven een `AndConstraint`.

### Probleem

Shouldly kent geen `becauseArgs`; FA geeft een constraint terug, `ShouldBeEmpty` niets.

### Nieuwe situatie

`BeEmptyPattern` (ID `fa.be-empty`); losse assertions worden herschreven, anders `Manual(reden)`.

### Verbetering

Eén patroon voor string, collecties en dictionaries.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Eén pattern voor `BeEmpty` en `NotBeEmpty` | Identieke logica op de naam na (zoals `BeNullPattern`) |
| 2 | Geen type-switch: Shouldly heeft `ShouldBeEmpty`/`ShouldNotBeEmpty` voor `string` en `IEnumerable<T>`, dictionaries vallen daaronder | Bewezen door de compileercheck op de `after`-proofs (string, List, Dictionary, IEnumerable, array) |
| 3 | `because` zonder args → `customMessage`; met `becauseArgs`/benoemd → Manual | Zelfde regel als andere patronen |
| 4 | Alleen herschrijven als resultaat genegeerd wordt en vorm `x.Should().…` is | `.And` vereist ketens splitsen; buiten scope |
| 5 | `BeNullOrEmpty`/`NotBeNullOrEmpty` buiten scope | Andere semantiek (null toegestaan); eigen patroon, Shouldly `ShouldBeNullOrEmpty` |
| 6 | Niet-generieke `IEnumerable` niet bewezen | Zeldzaam; eventueel later Manual |
| 7 | Manual-proofs: `chain`, `value-used` → "is used"; `and-chain` → "is not in the form"; `because-args` → "becauseArgs" | `ProofTests` eist één reden per proof |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (9 REQ)
- [x] 4. `BeEmptyPattern` + registratie
- [x] 5. Proofs (11 in `BeEmpty/`)
- [x] 6. Build/test groen (57 + 15 sample), verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/`
- `docs/projectinfo/patterns/fa-be-empty-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-004-fa-be-null.md`
