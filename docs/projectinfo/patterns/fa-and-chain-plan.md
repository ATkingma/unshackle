---
kingmadoc: 1
feature: "fa-and-chain"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
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
- **REQ-10** (Uitbreiding TAAK-171): WHEN `x.Should().ContainKeys(a, b)` of `NotContainKeys(a, b)` (params-vorm) als statement of schakel staat THE SYSTEM SHALL één `ContainKey`/`NotContainKey` per key splitsen en die door `ContainKeyPattern` laten herschrijven, ook voor `IReadOnlyDictionary` (proofs `contain-keys`, `contain-keys-and`, `not-contain-keys`, `contain-keys-read-only`, `contain-keys-subject-call`).
- **REQ-11** (TAAK-171): WHEN `ContainValue(s)` of `NotContainValue(s)` op een type met een `Values`-eigenschap staat THE SYSTEM SHALL per waarde `x.Values.Should().Contain(v)` (resp. `NotContain`) splitsen, met `because` bij de enkelvoudige vorm, en `ContainKeys(..).And.ContainValues(..)` in één keer herschrijven (proofs `contain-value`, `contain-value-because`, `not-contain-value`, `contain-value-read-only`, `contain-values`, `not-contain-values`, `contain-keys-values`).
- **REQ-12** (TAAK-171): WHEN `x.Should().ContainKey(k).WhoseValue.Should().M(..)` (eventueel met `.And`) als statement staat, x een indexer op de key heeft en k een constante of veilig te herhalen expressie is THE SYSTEM SHALL `x.ShouldContainKey(k);` gevolgd door `x[k].<M herschreven>` geven (proofs `whose-value`, `whose-value-because`, `whose-value-other`, `whose-value-and`, `whose-value-read-only`, `whose-value-key-variable`, `whose-value-subject-call`).
- **REQ-13** (TAAK-171): IF `ContainKeys`/`ContainValues` een collectie of becauseArgs krijgt, IF het type geen `Values` of geen indexer op de key heeft, OR IF de key van `ContainKey(..).WhoseValue` een berekende expressie is THEN THE SYSTEM SHALL Manual geven met de reden (proofs `contain-keys-collection`, `contain-values-collection`, `whose-value-no-indexer`, `whose-value-computed-key`).

## Assumptions

- `Compilation.ReplaceSyntaxTree` geeft een bruikbaar semantisch model voor de gesplitste boom.
- Elke `.And` in FA 7 geeft dezelfde assertions terug als `x.Should()`; anders Manual (typecheck).

- Een `Dictionary`-indexer na `ShouldContainKey` gebruikt dezelfde comparer als `ContainKey`; `ContainValue(s)` en `Contain` vergelijken allebei met de standaard gelijkheid (probe FA 7.2.2 vs Shouldly 4.3.0: arrays met gelijke inhoud en `"X"` tegenover `"x"` falen in beide).

## Risks

- Property-getters met bijwerkingen worden twee keer uitgevoerd (bewust geaccepteerd: `IsSafeToEvaluateTwice` neemt aan dat getters puur zijn).
- Een schakel-patroon dat meerdere nodes teruggeeft die niet allemaal statements zijn, wordt als niet-ondersteund gezien (meerdere statements, zoals OnlyContain, worden overgenomen).

- `ContainKey(k).WhoseValue.Should().Be(v)` wordt `x.ShouldContainKey(k); x[k].ShouldBe(v);` en niet `ShouldContainKeyAndValue(k, v)`: de tweede-stap-regels (typeconversie, `because`, strings) komen zo van het bestaande patroon voor de waarde, en elk vervolg (`StartWith`, `.And`) werkt hetzelfde. Voor een key die berekend wordt, is er geen veilige vorm: Manual.
- Een nullable dictionary met `ContainValue(s)` geeft `x!.Values...`: een null-subject faalt dan met een NullReferenceException in plaats van een assertion-fout; de test blijft rood.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-and-chain-modellen.md](fa-and-chain-modellen.md).
