---
kingmadoc: 1
feature: "fa-and-chain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/
---

# Feature: fa.and-chain: x.Should().A().And.B() wordt één Shouldly-statement per schakel

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-033.

## One-sentence summary

fa.and-chain: `x.Should().A().And.B();` wordt per schakel gesplitst en elke schakel wordt door zijn eigen patroon herschreven (Composite), zonder logica te dupliceren.

## Scope (in / out)

**In scope**

- Statement-ketens met `.And` op een subject zonder bijwerkingen (local, parameter, field, property zonder argumenten, member access daarop).
- Elke schakel die een bestaand assertion-patroon Fixed maakt, inclusief `because`, nullable subjects en multiline-ketens.
- Manual voor AssertionScope, gebruikt resultaat (`.Which`, toewijzing), niet in een blok, subject met bijwerkingen, een niet-ondersteunde of Manual-schakel.

**Out of scope**

- `.Which`-ketens (rij `fa.which-chain`).
- Subject eenmalig in een temp-variabele zetten (bewust niet: verandert de leesbaarheid).

## Requirements

- **REQ-1**: WHEN `x.Should().A().And.B()` als statement staat THE SYSTEM SHALL `x.ShouldA(); x.ShouldB();` geven via de schakel-patronen (proofs `simple`, `three-links`, `be-of-type`, `edge-multiline`).
- **REQ-2**: WHEN een schakel `because` heeft THE SYSTEM SHALL die per statement meegeven (proof `because`).
- **REQ-3**: WHEN het subject een member access of nullable is THE SYSTEM SHALL de schakel-regels toepassen (proofs `member-subject`, `nullable`).
- **REQ-4**: IF het subject een aanroep of indexer is THEN THE SYSTEM SHALL Manual geven (proofs `subject-call`, `subject-indexer`).
- **REQ-5**: IF een schakel door geen patroon wordt herschreven of Manual geeft THEN THE SYSTEM SHALL de hele keten Manual geven (proofs `unsupported-link`, `link-manual`).
- **REQ-6**: IF het resultaat wordt gebruikt of het statement niet direct in een blok staat THEN THE SYSTEM SHALL Manual geven (proofs `used`, `which`, `no-block`).
- **REQ-7**: IF de keten in een AssertionScope staat THEN THE SYSTEM SHALL Manual geven (proof `assertion-scope`).

## Assumptions

- `Compilation.ReplaceSyntaxTree` geeft een bruikbaar semantisch model voor de gesplitste boom.
- Elke `.And` in FA 7 geeft dezelfde assertions terug als `x.Should()`; anders Manual (typecheck).

## Risks

- Property-getters met bijwerkingen worden twee keer uitgevoerd (bewust geaccepteerd, conservatieve definitie).
- Een schakel-patroon dat meerdere nodes teruggeeft wordt als niet-ondersteund gezien.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-and-chain-modellen.md](fa-and-chain-modellen.md).
