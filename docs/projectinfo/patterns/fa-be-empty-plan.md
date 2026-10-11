---
kingmadoc: 1
feature: "fa-be-empty"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/
---
# Feature: fa.be-empty: x.Should().BeEmpty()/NotBeEmpty() wordt x.ShouldBeEmpty()/ShouldNotBeEmpty()

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-006. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.be-empty: x.Should().BeEmpty()/NotBeEmpty() wordt x.ShouldBeEmpty()/ShouldNotBeEmpty(), voor string, collecties en dictionaries.

## Scope (in / out)

**In scope**

- `x.Should().BeEmpty()` / `NotBeEmpty()` (met optionele `because` zonder `becauseArgs`) als losse assertion, op string, `IEnumerable<T>`, arrays, lijsten en dictionaries.
- `Manual(reden)` voor named arguments of `becauseArgs` met niet-constante `because` (sinds TAAK-097), ketens (`.And`) en gebruikt resultaat.

**Out of scope**

- `BeNullOrEmpty` / `NotBeNullOrEmpty`: eigen patroon (Shouldly: `ShouldBeNullOrEmpty`).
- `NotBeEmpty().And...` ketens splitsen.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `list.Should().BeEmpty()` op een collectie staat THE SYSTEM SHALL het herschrijven naar `list.ShouldBeEmpty()` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/be-empty-list.*.cs`).
- **REQ-2**: WHEN `list.Should().NotBeEmpty()` op een collectie staat THE SYSTEM SHALL het herschrijven naar `list.ShouldNotBeEmpty()` (proof `.../BeEmpty/not-be-empty-list.*.cs`).
- **REQ-3**: WHEN het subject een `string` is THE SYSTEM SHALL dezelfde `ShouldBeEmpty`/`ShouldNotBeEmpty` gebruiken (proof `.../BeEmpty/string.*.cs`).
- **REQ-4**: WHEN het subject een dictionary is THE SYSTEM SHALL het herschrijven (proof `.../BeEmpty/dictionary.*.cs`); idem `IEnumerable<T>` en array (proof `.../BeEmpty/enumerable.*.cs`).
- **REQ-5**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als `customMessage` overnemen (proof `.../BeEmpty/because.*.cs`).
- **REQ-6**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `.../BeEmpty/lambda.*.cs`).
- **REQ-7** (vervangen door REQ-10): IF `becauseArgs` gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `.../BeEmpty/because-args.*`).
- **REQ-8**: IF een vervolg op de assertion staat (`.And`) of het resultaat als waarde wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `.../BeEmpty/chain.*`, `value-used.*`).
- **REQ-9**: IF de assertion zelf een vervolg op een andere assertion is (`x.Should().NotBeNull().And.NotBeEmpty()`) THEN THE SYSTEM SHALL Manual geven (proof `.../BeEmpty/and-chain.*`; in de volledige Fix splitst `fa.and-chain` zo'n keten).
- **REQ-10** (Uitbreiding TAAK-097): WHEN `becauseArgs` met een constante, formatteerbare `because` staat THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (proof `.../BeEmpty/because-args.*.cs`); named arguments of een niet-constante `because` blijven Manual.

## Assumptions

- Shouldly 4.3.0 heeft `ShouldBeEmpty`/`ShouldNotBeEmpty` voor `string` en `IEnumerable<T>`; dictionaries vallen onder `IEnumerable<KeyValuePair<,>>` (bewezen door de compileercheck van `dictionary.after.cs`).
- Niet-generieke `IEnumerable` is niet bewezen en valt buiten de proofs.
- De `Fixer` beheert `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- Foutmelding krijgt "Additional Info:" in plaats van FA's "because"-voorvoegsel: gedocumenteerd verschil.
- `ShouldBeEmpty` op `null` faalt, net als FA's `BeEmpty`.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-empty-modellen.md](fa-be-empty-modellen.md).

## Open questions

- Geen.
