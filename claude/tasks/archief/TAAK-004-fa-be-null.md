# TAAK-004 — Patroon fa.be-null

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-10
**Doel**: `x.Should().BeNull()` / `NotBeNull()` wordt herschreven naar `ShouldBeNull()` / `ShouldNotBeNull()`, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 3 in `claude/tasks/patroon-queue.md`. Fundament en voorbeelden: TAAK-001..003.
- Sample-voorkomens: `RepositoryTests.cs:18,26` (`Find(id).Should().NotBeNull()` / `BeNull()` op een referentietype) en `OrderHandlerTests.cs:25` (`dto.Should().NotBeNull(); dto!.LineCount...`).

## Analyse

### Huidige situatie

FA 7.2.2: `BeNull(string because = "", params object[] becauseArgs)` en `NotBeNull(...)` bestaan op `ReferenceTypeAssertions` (object, string, collecties) en op de nullable-assertionklassen (`NullableNumericAssertions<T>`, `NullableBooleanAssertions`, ...). Ze geven een `AndConstraint` terug; `NotBeNull` op sommige types een `AndWhichConstraint` (`.Which`).

### Probleem

- Shouldly kent geen `because`-format-args.
- FA geeft een constraint terug (`.And`/`.Which`); `ShouldBeNull` geeft niets, `ShouldNotBeNull` geeft de waarde zelf terug.

### Nieuwe situatie

`BeNullPattern` (ID `fa.be-null`) herschrijft losse assertions; anders `Manual(reden)`.

### Verbetering

Eén patroon voor alle subjecttypes; geen type-switch nodig.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Eén pattern, `Find` op `BeNull` en `NotBeNull` | Identieke logica op de naam na (zoals `BeTrueFalsePattern`) |
| 2 | Geen type-switch: reference types én `Nullable<T>` → dezelfde `ShouldBeNull`/`ShouldNotBeNull` | Shouldly 4.3.0 heeft `ShouldBeNull<T>(this T? actual, string? customMessage)`; bewezen door de compileercheck met `int?`/`bool?` (anders dan `bool?` bij `ShouldBeTrue`) |
| 3 | `because` zonder args → `customMessage`; met `becauseArgs` of benoemde args → Manual | Shouldly formatteert niet; zelfde regel als andere patronen |
| 4 | Alleen herschrijven als het resultaat wordt genegeerd en de vorm `x.Should().…` is | `.And`/`.Which`/waarde vereisen ketens splitsen of `var v = x.ShouldNotBeNull()`; bewust buiten scope |
| 5 | `x!` in code na `NotBeNull()` blijft ongemoeid | Onschadelijk; Shouldly's `[NotNull]` maakt het alleen overbodig |
| 6 | Manual-proofs hebben elk één reden: `chain` (`.NotBeNull().And.NotBeEmpty()`) en `value-used` → "is used"; `and-chain` (`.NotBeEmpty().And.NotBeNull()`) → "is not in the form"; `because-args` → "becauseArgs" | `ProofTests` eist dezelfde reden voor alle findings in één proof (TAAK-003) |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan
- [x] 4. `BeNullPattern` + registratie
- [x] 5. Proofs (10 proofs in `BeNull/`)
- [x] 6. Build/test groen, verify, archiveren, rij `klaar`

## Aanpassingen bij realisatie

- Proof `which` vervallen: `NotBeNull().Which` bestaat niet op string/object-assertions; vervangen door `value-used`.
- Proofs missen implicit usings: `lambda` en `collection` hebben expliciete `using System;` / `System.Collections.Generic`.

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/`
- `docs/projectinfo/patterns/fa-be-null-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-003-fa-be-true-false.md`
