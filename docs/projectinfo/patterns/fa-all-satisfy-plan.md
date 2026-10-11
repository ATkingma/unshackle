---
kingmadoc: 1
feature: "fa-all-satisfy"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AllSatisfyPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/
---
# Feature: fa.all-satisfy: x.Should().AllSatisfy(a => ...) wordt een controle op leegte plus een foreach

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-014.

## One-sentence summary

fa.all-satisfy: `x.Should().AllSatisfy(a => body)` wordt `x.ShouldNotBeEmpty(); foreach (var a in x) { body }`, omdat `AllSatisfy` een `Action<T>` met assertions neemt en Shouldly's `ShouldAllBe` alleen een bool-predicate; onveilige vormen worden Manual.

## Scope (in / out)

**In scope**

- Losse `x.Should().AllSatisfy(lambda)` als statement, met een inline lambda met één parameter (expressie- of blok-body, ook met getypeerde parameter), een simpel subject (naam of member access) en direct in een blok.
- `Manual(reden)` voor `because`/`becauseArgs`, methodgroep, `return` in de body, async lambda, subject met aanroep, resultaat gebruikt, `.And`-vervolg en een statement buiten een blok.

**Out of scope**

- Herschrijven van de assertions in de body: dat doen de andere patterns (dit pattern staat vooraan in `FluentAssertionsFix`, zodat de body als losse statements nog door die patterns wordt gezien).
- `SatisfyRespectively`, `OnlyContain` (eigen patronen).

## Requirements

- **REQ-1**: WHEN `x.Should().AllSatisfy(i => body)` als statement staat THE SYSTEM SHALL het vervangen door `x.ShouldNotBeEmpty();` en `foreach (var i in x) { body; }` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/simple.*.cs`).
- **REQ-2**: WHEN de lambda een blok is THE SYSTEM SHALL het blok als loop-body overnemen met de juiste inspringing (proof `block.*.cs`).
- **REQ-3**: WHEN de lambda-parameter een type heeft THE SYSTEM SHALL dat type in de `foreach` gebruiken (proof `typed-parameter.*.cs`).
- **REQ-4**: WHEN het subject een member access is THE SYSTEM SHALL het herschrijven (proof `member-subject.*.cs`).
- **REQ-5**: IF de assertion een `because` heeft THEN THE SYSTEM SHALL Manual geven, omdat het bericht niet in de loop kan (proof `because.*`).
- **REQ-6**: IF `becauseArgs` of named arguments gebruikt worden THEN THE SYSTEM SHALL Manual geven (proof `because-args.*`).
- **REQ-7**: IF het argument geen inline lambda is (methodgroep) THEN THE SYSTEM SHALL Manual geven (proof `method-group.*`).
- **REQ-8**: IF de body een `return` bevat, of de lambda async is THEN THE SYSTEM SHALL Manual geven (proofs `return.*`, `async.*`).
- **REQ-9**: IF het subject geen naam of member access is THEN THE SYSTEM SHALL Manual geven, omdat het twee keer geëvalueerd zou worden (proof `subject-call.*`).
- **REQ-10**: IF het resultaat gebruikt wordt of de assertion op `.And` volgt THEN THE SYSTEM SHALL Manual geven (proofs `used.*`, `and-chain.*`).
- **REQ-11**: IF het statement niet direct in een blok staat THEN THE SYSTEM SHALL Manual geven (proof `not-in-block.*`).
- **REQ-12**: WHEN de body een FluentAssertions-assertion bevat (sample `OrderHandlerTests`) THE SYSTEM SHALL na de hele Fix een compileerbare loop met een Shouldly-assertion opleveren (integratietest op `samples/`, handmatig gecontroleerd met `fix` op een kopie).

## Assumptions

- FluentAssertions 7.2.2 faalt op een lege en op een `null` collectie (getest); `ShouldNotBeEmpty()` voor de loop behoudt dat.
- Een `foreach` stopt bij de eerste mislukte assertion; FA verzamelt alle mislukte elementen in één melding. Gedocumenteerd verschil.
- Property getters in het subject zijn puur (zelfde aanname als `fa.be-greater-less`).

## Risks

- Foutmelding verandert (eerste fout i.p.v. alle fouten per index).
- Een body met nog niet herschreven FluentAssertions-assertions (Manual elders) blijft FA-code in de loop.

## Modellen

Klassendiagram, sequence en beslisboom: [fa-all-satisfy-modellen.md](fa-all-satisfy-modellen.md).

## Open questions

- Geen.
