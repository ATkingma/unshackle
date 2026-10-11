---
kingmadoc: 1
feature: "fa-contain-key"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
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
- **REQ-2**: WHEN het subject een `IReadOnlyDictionary<,>` of `IDictionary<,>` is THE SYSTEM SHALL dezelfde herschrijving geven (proofs `read-only.*.cs`, `idictionary.*.cs`).
- **REQ-3**: WHEN `NotContainKey(k)` staat THE SYSTEM SHALL `ShouldNotContainKey(k)` geven (proof `not-contain-key.*.cs`).
- **REQ-4**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als `customMessage` overnemen (proof `because.*.cs`).
- **REQ-5**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `lambda.*.cs`).
- **REQ-6**: IF `becauseArgs` gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `because-args.*`).
- **REQ-7**: IF het resultaat wordt gebruikt (`.And`, `.WhoseValue`) of de assertion een vervolg op een andere is THEN THE SYSTEM SHALL Manual geven (proofs `chain.*`, `which.*`, `and-chain.*`).
- **REQ-8**: IF `ContainKeys`/`NotContainKeys` wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `keys.*`, `not-contain-keys.*`).
- **REQ-9**: IF `ContainValue(s)`/`NotContainValue` wordt gebruikt THEN THE SYSTEM SHALL Manual geven, omdat Shouldly geen losse waarde-assertie kent (proofs `contain-value.*`, `contain-values.*`, `not-contain-value.*`).
- **REQ-10**: IF het subject in een nullable-enabled context mogelijk null is THEN THE SYSTEM SHALL `x.ShouldNotBeNull().ShouldContainKey(k)` schrijven (TAAK-021: FluentAssertions accepteert een nullable subject, Shouldly's receiver niet; zonder dit CS8602/CS8604 in projecten met warnings-as-errors. `ShouldNotBeNull()` behoudt FA's falen op null) (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/nullable.*`).

## Assumptions

- Shouldly 4.3.0 heeft `ShouldContainKey`/`ShouldNotContainKey` voor `IDictionary` en `IReadOnlyDictionary`; bewezen door de compileercheck van de after-proofs.
- FluentAssertions biedt de methoden alleen op dictionaries, dus een typecontrole op het subject is overbodig.

## Risks

- Foutmelding verandert ("Additional Info:" i.p.v. FA's because-voorvoegsel): gedocumenteerd verschil.
- Een eigen type met een `ContainKey`-extension van FA op een andere interface valt buiten de proofs.

## Modellen

Klassendiagram, sequence en beslisboom: [fa-contain-key-modellen.md](fa-contain-key-modellen.md).

## Open questions

- Geen.
