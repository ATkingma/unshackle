# TAAK-164 — Patroon fa.contain-equivalent-of

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: `x.Should().ContainEquivalentOf(item)` / `NotContainEquivalentOf` zo exact mogelijk automatisch naar `ShouldContain`/`ShouldNotContain`; anders Manual met een specifieke reden.

## Context voor de volgende agent

- Rij 33 in `claude/tasks/patroon-queue.md`. Voorheen werd deze aanroep door geen enkel patroon gevonden.
- FA 7.2.2: `ContainEquivalentOf<TExpectation>(TExpectation, because, args)` en een overload met `config`; Shouldly 4.3.0: `ShouldContain<T>(IEnumerable<T>, T, customMessage)`.

## Analyse

Probe (scratchpad `probe164`, ruim 40 gevallen): voor int, string, double (ook `0.1+0.2` vs `0.3`: beide falen), enum, int?, DateTime, DateTimeOffset (zelfde moment, andere offset: beide slagen), decimal (schaal), TimeSpan, Guid, NaN, records, record structs en structs geven FA en Shouldly dezelfde uitkomst; ook voor ontbrekend item, lege collectie, duplicaten, null-item en null-subject (beide falen, ook `NotContainEquivalentOf`).

- Strings: FA vergelijkt in equivalency **hoofdlettergevoelig** (`"abc"` vs `"Abc"` faalt), net als `ShouldContain` op een collectie (anders dan `ShouldContain` op een string, waar `Case.Sensitive` nodig is).
- Verschil 1: een class zonder `Equals` (`C { X = 1 }`): FA slaagt (leden), Shouldly faalt (referentie) → Manual.
- Verschil 2: ander verwachtingstype dan elementtype: `List<object>` met `1L` (FA slaagt), `List<int>` met `2L` of `2.0` (FA slaagt; `ShouldContain(2L)` compileert niet) → Manual.
- Null-subject: beide falen; `NotNull(subject)` zet `ShouldNotBeNull()` voor de aanroep bij een nullable subject, zoals `fa.contain`.

## Keuzes

| #   | Keuze | Reden |
| --- | ----- | ----- |
| 1 | `ContainEquivalentOfPattern : BeEquivalentToObjectPattern`; `IsValueLike`/`IsLeafOrNullable` verhuisd van `EquivalentOptionsPattern` naar het basispatroon (protected) | Eén definitie van "Equals gelijk aan ledenvergelijking" (TAAK-163) |
| 2 | Fixed alleen als verwachting en element hetzelfde type hebben én het element waarde-achtig is (leaf, sealed record, record struct, struct met leaf-leden) | Geprobeerd gelijk; anders compileert `ShouldContain` niet of verschilt het gedrag |
| 3 | Uitkomst `x.ShouldContain(item, because)` / `ShouldNotContain`, met `ShouldNotBeNull()` bij nullable subject | Zelfde vorm als `fa.contain`; geen zwakkere test bij null |
| 4 | `config`-overload: Manual ("equivalency options have no Shouldly equivalent") | Opties veranderen de vergelijking; `ShouldContain` heeft ze niet |
| 5 | Class, niet-sealed record, record met collectie-lid, dictionary (`KeyValuePair`): Manual | Niet bewezen gelijk (reden: "is no leaf, record or struct of leaf members") |
| 6 | Resultaat gebruikt (`.Which`) en AssertionScope: Manual via de template | Gedeelde regels |

## Stappen

- [x] 1. Onderzoek + probe
- [x] 2. Plan
- [x] 3. `ContainEquivalentOfPattern`, registratie, `IsValueLike` verplaatst
- [x] 4. 21 proofs, `proofs-doc.py`
- [x] 5. Build en tests groen (35 bekende CRLF-falers), modellen, verify, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs` (nieuw), `BeEquivalentToObjectPattern.cs`, `EquivalentOptionsPattern.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/`
- `docs/projectinfo/patterns/fa-contain-equivalent-of-{plan,modellen,verify}.md`, README-rij, `architectuur/proofs.md`

## Zie ook

- `claude/tasks/archief/TAAK-163-fa-equivalent-options.md`
- `docs/projectinfo/patterns/fa-contain-equivalent-of-plan.md`
