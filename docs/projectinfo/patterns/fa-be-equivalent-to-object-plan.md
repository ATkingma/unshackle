---
kingmadoc: 1
feature: "fa-be-equivalent-to-object"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/
---
# Feature: fa.be-equivalent-to-object: obj.Should().BeEquivalentTo(other) wordt obj.ShouldBeEquivalentTo(other) of Manual

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-016. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.be-equivalent-to-object: `BeEquivalentTo` op een object wordt alleen `ShouldBeEquivalentTo` als beide libraries aantoonbaar hetzelfde beslissen (zelfde gewone type, alleen eenvoudige leden); al het andere is Manual.

## Scope (in / out)

**In scope**

- `a.Should().BeEquivalentTo(b)` (met optionele `because` zonder `becauseArgs`) waarbij `a` en `b` hetzelfde compile-time type hebben: een niet-abstracte class, struct of record zonder `Equals`-override, zonder cyclus en met minstens één publiek lid, waarvan alle bereikbare leden primitieven, `string`, `decimal`, enums, `DateTime`, `DateTimeOffset`, `Guid`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Nullable` daarvan, of zelf zo'n gewoon type zijn.
- `Manual(reden)` voor al het andere.

**Out of scope**

- Strings: `fa.be-equivalent-to-string` (Find sluit `StringAssertions` uit).
- Het zelf herschrijven van `options =>`-configuratie of collecties; dat heeft geen Shouldly-equivalent.
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `a.Should().BeEquivalentTo(b)` op twee waarden van hetzelfde gewone type staat THE SYSTEM SHALL het herschrijven naar `a.ShouldBeEquivalentTo(b)` (proofs `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/simple.*.cs`, `new-expectation.*.cs`, `record.*.cs`, `nested.*.cs`).
- **REQ-2**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als tweede argument (`customMessage`) meegeven (proof `.../because.*.cs`).
- **REQ-3**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `.../lambda.*.cs`).
- **REQ-4**: IF `NotBeEquivalentTo` wordt gebruikt THEN THE SYSTEM SHALL Manual geven, omdat Shouldly geen `ShouldNotBeEquivalentTo` heeft (proof `.../not-equivalent.*`).
- **REQ-5**: IF de assertion een vervolg op een andere assertion is of het resultaat wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proofs `.../and-chain.*`, `.../value-used.*`).
- **REQ-6**: IF `becauseArgs` worden gebruikt THEN THE SYSTEM SHALL Manual geven (proof `.../because-args.*`).
- **REQ-7**: IF een `options =>`-configuratie wordt meegegeven THEN THE SYSTEM SHALL Manual geven (proof `.../options.*`).
- **REQ-8**: IF subject en verwachting een verschillend compile-time type hebben (ook een anoniem type of basis- versus afgeleide klasse) THEN THE SYSTEM SHALL Manual geven, omdat FluentAssertions alleen de leden van de verwachting vergelijkt en Shouldly gelijke typen eist (proofs `.../anonymous.*`, `.../different-types.*`, `.../base-vs-derived.*`).
- **REQ-9**: IF het type een collectie is of een collectie als bereikbaar lid heeft THEN THE SYSTEM SHALL Manual geven, omdat FluentAssertions de volgorde standaard negeert en Shouldly niet (proofs `.../collection.*`, `.../collection-member.*`).
- **REQ-10**: IF het type of een bereikbaar lid `object`, een interface, abstract of een BCL-type buiten de eenvoudige set is THEN THE SYSTEM SHALL Manual geven, omdat het runtime-type de vergelijking bepaalt (proofs `.../object-subject.*`, `.../object-member.*`).
- **REQ-11**: IF het type of een bereikbaar lid `Equals` overschrijft THEN THE SYSTEM SHALL Manual geven, omdat FluentAssertions dan `Equals` gebruikt en Shouldly de leden (proofs `.../equals-override.*`, `.../equals-member.*`).
- **REQ-12**: IF het type deel is van een cyclische objectgraaf THEN THE SYSTEM SHALL Manual geven, omdat FluentAssertions faalt en Shouldly slaagt (proof `.../cyclic.*`).
- **REQ-13**: IF het type geen publieke leden heeft THEN THE SYSTEM SHALL Manual geven, omdat FluentAssertions een uitzondering gooit en Shouldly slaagt (proof `.../no-members.*`).
- **Uitbreiding TAAK-092**: WHEN subject en verwachting een `IEnumerable<T>` met hetzelfde leaf-elementtype zijn (primitives, string, enum, DateTime(Offset), Guid, … ook nullable) THE SYSTEM SHALL `x.ShouldBe(expected, ignoreOrder: true[, because])` geven (proofs `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal`, `AndChain/equivalent-collection`); ander elementtype of niet-leaf-elementen blijven Manual (`collection-element-types`, `collection`). Probe: duplicaten, null-elementen, leeg, andere lengte, null-subject/-verwachting geven hetzelfde oordeel.

## Assumptions

- Gedrag bepaald met een proefprogramma tegen FluentAssertions 7.2.2 en Shouldly 4.3.0 (zie TAAK-016): Shouldly is strenger dan FluentAssertions (volgorde van collecties, runtime-typen, afgeleide leden, `int` versus `long`, `Equals`-gebruik), dus daar faalt een herschreven test hoorbaar. Gevaarlijk is alleen het omgekeerde (FA faalt, Shouldly slaagt): `Equals`-override zonder gelijke gedrag, cyclische grafen en typen zonder publieke leden; die zijn Manual.
- Voor dezelfde gewone typen met eenvoudige leden beslissen beide gelijk: hoofdlettergevoelige strings, `null` versus lege string, enums, `decimal` (1.0 gelijk aan 1.00), `DateTime` (Kind genegeerd), `double.NaN` gelijk, structs, records, privé-leden genegeerd, publieke velden meegenomen.
- Een niet-afgeschermde klasse kan een afgeleid runtime-type hebben met extra leden; FluentAssertions negeert die, Shouldly niet. Dat geeft hoogstens een hoorbare extra fout en wordt geaccepteerd.
- Shouldly heeft `ShouldBeEquivalentTo(expected, customMessage)`; bewezen door de compileercheck van de after-proofs.

## Risks

- Hoorbare extra fout bij een afgeleid runtime-type (zie boven).
- Foutmelding krijgt "Additional Info:" in plaats van FA's "because"-voorvoegsel.
- Leden met een eigen `Equals` in een type buiten de bron (BCL) worden als niet-eenvoudig beschouwd en geven Manual.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-equivalent-to-object-modellen.md](fa-be-equivalent-to-object-modellen.md).

## Open questions

- Geen.
