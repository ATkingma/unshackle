---
kingmadoc: 1
feature: "fa-equivalent-options"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/
---

# Feature: fa.equivalent-options: BeEquivalentTo met options

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 met de hand (kingmadoc ontbreekt) |

> Opgesteld uit TAAK-163.

## One-sentence summary

`x.Should().BeEquivalentTo(y, o => o.Option())` wordt waar het exact kan `ShouldBe`/`ShouldBeEquivalentTo` (opties die niets veranderen vervallen, `WithStrictOrdering` op waarde-collecties wordt `ShouldBe(y)`), anders Manual met een reden per optie.

## Scope (in / out)

**In scope**

- `BeEquivalentTo(expected, Func<EquivalencyAssertionOptions<T>, ...> config, because, becauseArgs)` op object- en collectie-assertions.
- Een inline lambda `o => o.A().B()`; de laatste `WithStrictOrdering`/`WithoutStrictOrdering` wint (zoals in FA).

**Out of scope**

- `NotBeEquivalentTo` met options (blijft Manual in `fa.be-equivalent-to-object`).
- `StringAssertions.BeEquivalentTo(.., o => o.IgnoringCase())` (zit in `fa.be-equivalent-to-string`).
- Opties in een blokbody, variabele of method group (Manual).

## Requirements

- **REQ-1**: WHEN `WithStrictOrdering()` (laatst geldend) op een collectie van waarden (leaf, sealed record, record struct of struct met alleen leaf-velden) met gelijk itemtype staat THE SYSTEM SHALL `x.ShouldBe(y)` geven, met `because`: `x.ShouldBe(y, ignoreOrder: false, because)` (proofs `strict-ordering`, `-lambda-parenthesized`, `-because`, `-because-args`, `-strings`, `-record`, `-record-struct`, `-struct`, `-with-missing-members`, `without-then-strict`).
- **REQ-2**: WHEN alleen `WithoutStrictOrdering()`, `ExcludingMissingMembers()` of `ThrowingOnMissingMembers()` (of `WithStrictOrdering()` op een niet-collectie) staat THE SYSTEM SHALL de opties laten vervallen en de vorm van `fa.be-equivalent-to-object` geven: `ShouldBeEquivalentTo(y)` of `ShouldBe(y, ignoreOrder: true)` (proofs `without-strict-ordering`, `strict-then-without`, `strict-ordering-object`, `excluding-missing-members`, `throwing-on-missing-members`, `lenient-collection-ints`).
- **REQ-3**: IF `WithStrictOrdering()` op een collectie met items die een class, niet-sealed of computed/collection-lid record zijn, of met verschillende itemtypes THEN THE SYSTEM SHALL Manual geven (proofs `strict-ordering-class`, `-record-unsealed`, `-record-computed`, `-record-list-member`, `-item-types`).
- **REQ-4**: IF een lid-selectie-, vergelijkings-, type-, cyclus- of trace-optie (`Excluding`, `Including`, `Using`, `ComparingByValue`, `RespectingRuntimeTypes`, `WithTracing`, ...) THEN THE SYSTEM SHALL Manual geven met per optie een eigen reden die de optietekst noemt (proofs `excluding`, `excluding-with-strict`, `excluding-two`, `including`, `using-comparer`, `comparing-by-value`, `respecting-runtime-types`, `with-tracing`, `collection-excluding`).
- **REQ-5**: IF `ExcludingMissingMembers()` bij verschillende compile-time types THEN THE SYSTEM SHALL Manual geven met die reden (proof `excluding-missing-members-other-type`).
- **REQ-6**: IF de options geen inline lambda van de vorm `o => o.Optie(...)` zijn THEN THE SYSTEM SHALL Manual geven (proofs `block-body`, `options-variable`).

## Assumptions

- Probe tegen FA 7.2.2 en Shouldly 4.3.0 (scratchpad `probe163`, 45 gevallen): voor leaf-collecties, sealed records, record structs en structs met leaf-leden geven `WithStrictOrdering` en `ShouldBe(y)` dezelfde uitkomst bij gelijk, andere volgorde, andere lengte, duplicaten, null-items, null-subject/-expectation, NaN, DateTime-kind en decimal-schaal.
- `ExcludingMissingMembers`/`ThrowingOnMissingMembers` doen niets bij hetzelfde compile-time type (geprobeerd); `WithStrictOrdering` op een object zonder collecties ook niet (collecties geven in het basispatroon Manual).

## Risks

- Een struct of record met een private auto-property, een berekende publieke property of een eigen `Equals` wordt bewust niet herkend (Manual): FA vergelijkt leden, `ShouldBe` vergelijkt met `Equals`.
- `WithTracing` valt onder Manual omdat het de foutmelding verandert, niet de uitkomst.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-equivalent-options-modellen.md](fa-equivalent-options-modellen.md).

## Zie ook

- [fa-equivalent-options-verify.md](fa-equivalent-options-verify.md)
- [fa-be-equivalent-to-object-plan.md](fa-be-equivalent-to-object-plan.md)
- `claude/tasks/archief/TAAK-163-fa-equivalent-options.md`
