---
kingmadoc: 1
feature: "fa-and-chain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9]
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

- Statement-ketens met `.And`; een subject met bijwerkingen wordt eerst gehoist (TAAK-098).
- Elke schakel die een bestaand assertion-patroon Fixed maakt, inclusief `because`, nullable subjects en multiline-ketens.
- Manual voor AssertionScope, gebruikt resultaat (`.Which`, toewijzing), niet in een blok, een niet-ondersteunde of Manual-schakel.

**Out of scope**

- `.Which`-ketens (rij `fa.which-chain`).
- ~~Subject eenmalig in een temp-variabele zetten~~: sinds TAAK-098 wel, alleen voor subjects met bijwerkingen.

## Requirements

- **REQ-1**: WHEN `x.Should().A().And.B()` als statement staat THE SYSTEM SHALL `x.ShouldA(); x.ShouldB();` geven via de schakel-patronen (proofs `simple`, `three-links`, `be-of-type`, `edge-multiline`).
- **REQ-2**: WHEN een schakel `because` heeft THE SYSTEM SHALL die per statement meegeven (proof `because`).
- **REQ-3**: WHEN het subject een member access of nullable is THE SYSTEM SHALL de schakel-regels toepassen (proofs `member-subject`, `nullable`).
- **REQ-4** (vervangen door REQ-8): IF het subject een aanroep of indexer is THEN THE SYSTEM SHALL Manual geven (proofs `subject-call`, `subject-indexer`).
- **REQ-5**: IF een schakel door geen patroon wordt herschreven of Manual geeft THEN THE SYSTEM SHALL de hele keten Manual geven (proof `unsupported-link`; een Manual-schakel geeft "Link X of the .And chain: reden", geen eigen proof meer).
- **REQ-6**: IF het resultaat wordt gebruikt of het statement niet direct in een blok staat THEN THE SYSTEM SHALL Manual geven (proofs `used`, `which`, `no-block`).
- **REQ-7**: IF de keten in een AssertionScope staat THEN THE SYSTEM SHALL Manual geven (proof `assertion-scope`).
- **REQ-8** (Uitbreiding TAAK-098): WHEN het subject niet veilig twee keer te evalueren is (aanroep, indexer) THE SYSTEM SHALL het subject hoisten naar `var subject = x;` (meerdere in één member: `subject2`, …) en daarna splitsen, i.p.v. Manual (proofs `subject-call`, `subject-indexer`, `subject-call-twice`).
- **REQ-9** (Uitbreiding TAAK-092..097): WHEN een schakel door een nieuwer of uitgebreid patroon Fixed wordt THE SYSTEM SHALL die schakel meenemen, ook met `becauseArgs` (proofs `be-same-as`, `dates`, `equal-length-approximately`, `equivalent-collection`, `greater-less`, `link-because-args`).

## Assumptions

- `Compilation.ReplaceSyntaxTree` geeft een bruikbaar semantisch model voor de gesplitste boom.
- Elke `.And` in FA 7 geeft dezelfde assertions terug als `x.Should()`; anders Manual (typecheck).

## Risks

- Property-getters met bijwerkingen worden twee keer uitgevoerd (bewust geaccepteerd: `IsSafeToEvaluateTwice` neemt aan dat getters puur zijn).
- Een schakel-patroon dat meerdere nodes teruggeeft die niet allemaal statements zijn, wordt als niet-ondersteund gezien (meerdere statements, zoals OnlyContain, worden overgenomen).

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-and-chain-modellen.md](fa-and-chain-modellen.md).
