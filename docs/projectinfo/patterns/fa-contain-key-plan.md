---
kingmadoc: 1
feature: "fa-contain-key"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/
---
# Feature: fa.contain-key: dict.Should().ContainKey(k) wordt dict.ShouldContainKey(k)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-013.

## One-sentence summary

fa.contain-key: `ContainKey`/`NotContainKey` met één sleutel wordt `ShouldContainKey`/`ShouldNotContainKey`; alle andere dictionary-vormen worden Manual.

## Scope (in / out)

**In scope**

- Losse `dict.Should().ContainKey(k)` en `NotContainKey(k)` (optioneel met `because` zonder `becauseArgs`) op `Dictionary`, `IDictionary` en `IReadOnlyDictionary`.
- `Manual(reden)` voor `ContainKeys`/`NotContainKeys`, `ContainValue(s)`/`NotContainValue(s)`, `becauseArgs`, ketens en gebruikt resultaat (`.WhoseValue`).

**Out of scope**

- `Contain(key, value)` (blijft Manual in `fa.contain`), `HaveCount` op dictionaries.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `dict.Should().ContainKey(k)` op een `Dictionary<,>` staat THE SYSTEM SHALL het herschrijven naar `dict.ShouldContainKey(k)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/dictionary.*.cs`).
- **REQ-2**: WHEN het subject een `IDictionary<,>` is THE SYSTEM SHALL dezelfde herschrijving geven (proof `idictionary.*.cs`). Voor `IReadOnlyDictionary<,>` zie REQ-11 (TAAK-168: `ShouldContainKey` bestaat daar niet, CS0411).
- **REQ-3**: WHEN `NotContainKey(k)` staat THE SYSTEM SHALL `ShouldNotContainKey(k)` geven (proof `not-contain-key.*.cs`).
- **REQ-4**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als `customMessage` overnemen (proof `because.*.cs`).
- **REQ-5**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `lambda.*.cs`).
- **REQ-6**: IF `becauseArgs` gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `because-args.*`).
- **REQ-7**: IF het resultaat wordt gebruikt (`.And`, `.WhoseValue`) of de assertion een vervolg op een andere is THEN THE SYSTEM SHALL Manual geven (proofs `chain.*`, `which.*`, `and-chain.*`).
- **REQ-8**: IF `ContainKeys`/`NotContainKeys` wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `keys.*`, `not-contain-keys.*`).
- **REQ-9**: IF `ContainValue(s)`/`NotContainValue` wordt gebruikt THEN THE SYSTEM SHALL Manual geven, omdat Shouldly geen losse waarde-assertie kent (proofs `contain-value.*`, `contain-values.*`, `not-contain-value.*`).
- **REQ-10**: IF het subject in een nullable-enabled context mogelijk null is THEN THE SYSTEM SHALL `x.ShouldNotBeNull().ShouldContainKey(k)` schrijven (TAAK-021: FluentAssertions accepteert een nullable subject, Shouldly's receiver niet; zonder dit CS8602/CS8604 in projecten met warnings-as-errors. `ShouldNotBeNull()` behoudt FA's falen op null) (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/nullable.*`).
- **REQ-11** (TAAK-168): WHEN het subject geen `IDictionary<,>` is maar een publieke `bool ContainsKey(key)` heeft (`IReadOnlyDictionary<,>`) THE SYSTEM SHALL `x.ContainsKey(k).ShouldBeTrue()` (`NotContainKey`: `ShouldBeFalse()`) geven, met `because` als `customMessage` (proofs `read-only.*`, `read-only-not-contain.*`). Gekozen boven `x.Keys.ShouldContain(k)`: `ContainsKey` behoudt de comparer van de dictionary en werkt voor elke implementatie.
- **REQ-12** (TAAK-168): WHEN het subject geen `ContainsKey` maar wel een publieke `bool Contains(key)` heeft (`HttpHeaders`) THE SYSTEM SHALL `x.Contains(k).ShouldBeTrue()`/`ShouldBeFalse()` geven (proof `headers.*`).
- **REQ-13** (TAAK-168): IF het subject geen van beide heeft (bv. `IEnumerable<KeyValuePair<,>>`) THEN THE SYSTEM SHALL Manual geven (proof `enumerable-pairs.*`).
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als `customMessage` geven (proof `because-args.after.cs`); benoemde argumenten of een niet-constante `because` blijven Manual. Vervangt REQ-6 voor dit geval.

## Assumptions

- Shouldly 4.3.0 heeft `ShouldContainKey`/`ShouldNotContainKey` alleen voor `IDictionary<,>` (TAAK-168; eerder stond hier ten onrechte ook `IReadOnlyDictionary`). Bewezen door de compileercheck van de after-proofs.
- FluentAssertions biedt `ContainKey` ook op `IReadOnlyDictionary`, `HttpHeaders` en elke `IEnumerable<KeyValuePair<,>>`, dus het patroon controleert het subjecttype (TAAK-168).

## Risks

- `HttpHeaders.Contains` negeert hoofdletters, FA's `ContainKey` op headers niet: `ContainKey("x-a")` slaagt na de migratie ook voor `X-A` (gemeten met een probe, TAAK-168). Bedoeld gedrag voor headers; `NotContainKey` wordt er strenger door, nooit zwakker.
- Foutmelding verandert ("Additional Info:" i.p.v. FA's because-voorvoegsel): gedocumenteerd verschil.
- Een eigen type met een `ContainKey`-extension van FA op een andere interface valt buiten de proofs.

## Modellen

Klassendiagram, sequence en beslisboom: [fa-contain-key-modellen.md](fa-contain-key-modellen.md).

## Open questions

- Geen.
