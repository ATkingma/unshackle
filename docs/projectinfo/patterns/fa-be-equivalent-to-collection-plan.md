---
kingmadoc: 1
feature: "fa-be-equivalent-to-collection"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/ParamsArraySyntax.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/
---

# Feature: fa.be-equivalent-to-object: BeEquivalentTo op een collectie wordt ShouldBe(ignoreOrder: true)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 met de hand (kingmadoc ontbreekt) |

> Opgesteld uit TAAK-172 (corpus-run ronde 3: 207 Manuals "FluentAssertions ignores the item order by default").

## One-sentence summary

`x.Should().BeEquivalentTo(expected)` en `x.Should().BeEquivalentTo("a", "b")` op een collectie van waarde-items (leaf, sealed record, struct) worden `x.ShouldBe(expected, ignoreOrder: true)`; andere collecties blijven Manual.

## Scope (in / out)

**In scope**

- Uitbreiding van het collectiegeval van `BeEquivalentToObjectPattern` (TAAK-092, alleen leaf-items) naar `IsValueLike`-items, dezelfde check als `fa.contain-equivalent-of`.
- De params-vorm `BeEquivalentTo("a", "b")` (alleen `StringCollectionAssertions` heeft die) wordt `ShouldBe(new string[] { "a", "b" }, ignoreOrder: true)`, met de opmaak van `ParamsArraySyntax` (gedeeld met `fa.equal-sequence`, TAAK-153).

**Out of scope**

- Classes zonder waarde-gelijkheid, niet-sealed records, records met geneste leden, ander verwachtings- dan elementtype, options-lambda's: Manual.

## Requirements

- **REQ-1**: WHEN subject en verwachting collecties van hetzelfde leaf-elementtype zijn THE SYSTEM SHALL `ShouldBe(expected, ignoreOrder: true[, because])` geven (bestaande proofs `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal`).
- **REQ-2**: WHEN het element een sealed record, record struct of struct met leaf-leden is THE SYSTEM SHALL dezelfde herschrijving geven (proofs `collection-record`, `collection-struct`).
- **REQ-3**: WHEN de params-vorm `BeEquivalentTo("a", "b")` op `IEnumerable<string>` staat THE SYSTEM SHALL `ShouldBe(new string[] { ... }, ignoreOrder: true)` geven, ook leeg, met `null`-items en over meerdere regels met behoud van commentaar (proofs `params-strings`, `edge-collection-params`, `params-multi-line`).
- **REQ-4**: WHEN een array als argument wordt doorgegeven THE SYSTEM SHALL die als gewone verwachting behandelen (proof `params-array`).
- **REQ-5**: IF het element een class, niet-sealed record of record met geneste leden is, of het verwachtingselement verschilt van dat van het subject THEN THE SYSTEM SHALL Manual geven met een specifieke reden (proofs `collection`, `collection-record-unsealed`, `collection-record-nested`, `collection-element-types`).

## Assumptions

- Geprobeerd tegen FluentAssertions 7.2.2 en Shouldly 4.3.0: herordenen, dubbele items (aantal telt mee), ander aantal, hoofdlettergevoelige strings, `null`-items, lege collecties, null-subject, structs en record structs geven aan beide kanten dezelfde uitkomst.
- Alleen `StringCollectionAssertions` heeft een `params`-overload; voor `IEnumerable<int>` of records compileert `BeEquivalentTo(1, 2)` niet.

## Risks

- `BeEquivalentTo("ab")` op een `IEnumerable<char>`-subject bindt aan de `IEnumerable<char>`-overload (string is `IEnumerable<char>`); de bestaande collectiecheck behandelt dat als een gewone verwachting.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-be-equivalent-to-collection-modellen.md](fa-be-equivalent-to-collection-modellen.md).

## Zie ook

- [fa-be-equivalent-to-collection-verify.md](fa-be-equivalent-to-collection-verify.md)
- [fa-be-equivalent-to-object-plan.md](fa-be-equivalent-to-object-plan.md)
- `claude/tasks/archief/TAAK-172-be-equivalent-to-collectie-ignore-order.md`
