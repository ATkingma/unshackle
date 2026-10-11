---
kingmadoc: 1
feature: "fa-be-greater-less"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13, REQ-14]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/
---
# Feature: fa.be-greater-less: BeGreaterThan/BeLessThan (+ .And-ketens) worden ShouldBeGreaterThan/ShouldBeLessThan, ketens gesplitst

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-010. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.be-greater-less: vergelijkingen (`BeGreaterThan`, `BeLessThan`, `...OrEqualTo`, `BeInRange`, `BePositive`, `BeNegative`) worden `ShouldBe...`; `.And`-ketens worden gesplitst in losse statements.

## Scope (in / out)

**In scope**

- `BeGreaterThan`, `BeGreaterThanOrEqualTo` (+ alias `BeGreaterOrEqualTo`), `BeLessThan`, `BeLessThanOrEqualTo` (+ alias `BeLessOrEqualTo`), `BeInRange`, `NotBeInRange`, `BePositive`, `BeNegative`, op niet-nullable numerieke types en andere `IComparable<T>`-types, met optionele `because`.
- `.And`-ketens van alleen deze assertions: splitsen naar één statement per schakel als het subject zonder bijwerkingen twee keer geëvalueerd mag worden.
- `Manual(reden)` voor nullable subjects, `becauseArgs`, gebruikt resultaat, ketens met andere assertions, subjects met aanroepen en ketens buiten een blok.

**Out of scope**

- `BeAfter`/`BeBefore`/`BeOnOrAfter` op `DateTime` (andere FA-methoden, eigen patroon), `BeCloseTo`, `BeApproximately`.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `x.Should().BeGreaterThan(n)`, `BeGreaterThanOrEqualTo`/`BeGreaterOrEqualTo`, `BeLessThan` of `BeLessThanOrEqualTo`/`BeLessOrEqualTo` als losse assertion op een niet-nullable numeriek type staat THE SYSTEM SHALL `x.ShouldBeGreaterThan(n)` resp. `ShouldBeGreaterThanOrEqualTo`, `ShouldBeLessThan`, `ShouldBeLessThanOrEqualTo` geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/simple.*.cs`).
- **REQ-2**: WHEN het subject een niet-numeriek type is dat `IComparable<T>` van zichzelf implementeert (TimeSpan, Version) THE SYSTEM SHALL dezelfde herschrijving geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/comparable.*.cs`).
- **REQ-3**: WHEN `BeInRange(min, max)` of `NotBeInRange(min, max)` staat THE SYSTEM SHALL `ShouldBeInRange` resp. `ShouldNotBeInRange` geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/in-range.*.cs`).
- **REQ-4**: WHEN `BePositive()` of `BeNegative()` op int, long, short, decimal, double of float staat THE SYSTEM SHALL `ShouldBePositive()` resp. `ShouldBeNegative()` geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/sign.*.cs`).
- **REQ-5**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als laatste argument (`customMessage`) overnemen (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/because.*.cs`).
- **REQ-6**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/lambda.*.cs`).
- **REQ-7**: WHEN een `.And`-keten uitsluitend uit vergelijkingen bestaat, als statement in een blok staat en het subject een naam of member-access op namen is THE SYSTEM SHALL de keten splitsen in één statement per schakel, elk met eigen `because` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/and-chain.*.cs`).
- **REQ-8**: IF het subject een nullable (`int?`) of onbekend type is THEN THE SYSTEM SHALL Manual geven, omdat `ShouldBeGreaterThan<T>` `IComparable<T>` van een non-nullable T eist (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/nullable.*`).
- **REQ-9**: IF `BePositive`/`BeNegative` op een ander type dan int, long, short, decimal, double of float staat THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/sign-byte.*`).
- **REQ-10**: IF `becauseArgs` gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/because-args.*`).
- **REQ-11**: IF het resultaat wordt gebruikt (variabele of lambda met waarde, of keten in een lambda) THEN THE SYSTEM SHALL Manual geven (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/value-used.*`, `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-used.*`, `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-lambda.*`).
- **REQ-12**: IF een `.And`-keten een andere assertion bevat THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-other.*`).
- **REQ-13**: IF het subject van een keten een aanroep of ander expressie met mogelijke bijwerkingen is THEN THE SYSTEM SHALL Manual geven, omdat splitsen het twee keer evalueert (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-subject-call.*`).
- **REQ-14**: IF een gesplitste keten niet direct in een blok staat (bijv. `if` zonder accolades) THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/chain-no-block.*`).

## Assumptions

- Shouldly 4.3.0 heeft `ShouldBeGreaterThan<T>(T, string? customMessage)` (idem voor `LessThan`, `...OrEqualTo`, `InRange`, `NotInRange` met `where T : IComparable<T>`) en `ShouldBePositive`/`ShouldBeNegative` voor int, long, short, decimal, double, float; bewezen door de compileercheck van de after-proofs.
- Property-getters in een ketensubject (`order.Total`) zijn zonder bijwerkingen (shortcut, in code gedocumenteerd).
- De `Fixer` zet de trivia en inspringing van de extra statements en beheert de usings.

## Risks

- Een property met bijwerkingen als subject van een keten wordt twee keer gelezen; beperkt door alleen namen en member-accesses toe te staan.
- Foutmeldingen verschillen van FluentAssertions (Shouldly zet "Additional Info:" voor de `because`).
- `double.NaN` en afrondingsgedrag zijn niet apart onderzocht; de vergelijking is `CompareTo` bij beide bibliotheken.

## Modellen

Klassen-, sequence- en beslismodel van dit patroon: [fa-be-greater-less-modellen.md](fa-be-greater-less-modellen.md).

## Open questions

- Geen.
