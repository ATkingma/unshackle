---
kingmadoc: 1
feature: "fa-invoking"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/
---

# Feature: fa.invoking

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-052.

## One-sentence summary

`x.Invoking(s => s.M()).Should().Throw<T>()` wordt `Should.Throw<T>(() => x.M())`; ook `Awaiting`/`ThrowAsync`, `ThrowExactly(Async)` en `NotThrow`.

## Scope (in / out)

**In scope**

- `Invoking`/`Awaiting` met een lambda met één parameter op een local, parameter, field of `this`, gevolgd door `Throw`, `ThrowAsync`, `ThrowExactly(Async)` of `NotThrow`.

**Out of scope**

- Receiver met mogelijke bijwerkingen (aanroep, property, indexer): Manual.
- Value-type receiver, method group: Manual.
- `FluentActions.Invoking(...)`: blijft Manual zoals voorheen.

## Requirements

- **REQ-1**: WHEN `x.Invoking(s => s.M()).Should().Throw<T>()` staat THE SYSTEM SHALL `Should.Throw<T>(() => x.M())` geven (proof `simple, func`).
- **REQ-2**: WHEN `because` of `.WithMessage` volgt THE SYSTEM SHALL de regels van `fa.throw` toepassen (proof `because-message`).
- **REQ-3**: WHEN `ThrowExactly<T>` volgt THE SYSTEM SHALL `.ShouldBeOfType<T>()` toevoegen (proof `throw-exactly, awaiting-exactly`).
- **REQ-4**: WHEN `NotThrow()` volgt THE SYSTEM SHALL `Should.NotThrow(() => x.M())` geven (proof `not-throw`).
- **REQ-5**: WHEN `x.Awaiting(s => s.MAsync()).Should().ThrowAsync<T>()` staat THE SYSTEM SHALL `await Should.ThrowAsync<T>(() => x.MAsync())` geven (proof `awaiting`).
- **REQ-6**: IF de receiver bijwerkingen kan hebben of een value type is, of het argument geen lambda is THEN THE SYSTEM SHALL Manual geven (proof `side-effect-receiver, value-type, method-group`).
- **REQ-7**: IF becauseArgs, een AssertionScope of een blokkerende Wait in de lambda THEN THE SYSTEM SHALL Manual geven (proof `because-args, assertion-scope, aggregate`). _Voor becauseArgs vervangen door Uitbreiding TAAK-097._
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (Fixed, proof `because-args`); benoemde argumenten of een niet-constante `because` blijven Manual. `InvokingPattern` zoekt nu via `ThrowSyntax.FindThrow`/`FindThrowAsync`/`FindNotThrow` en herschrijft via `ThrowSyntax.Rewrite`.
- **Uitbreiding TAAK-173**: `FluentActions.Invoking(d)`/`Awaiting(d)` geven `d` zelf terug: een lambda, of een stabiele `Action`/`Func<Task>`-variabele, gaat direct naar Shouldly (proof `fluent-actions`). Een lambda die de fix schrijft converteert naar `Action`/`Func<object?>`/`Func<Task>`, dus ook een waardetype-resultaat (`int`, enum, `Task<int>`) is Fixed (proof `value-result`); alleen een doorgegeven delegate moet een type hebben dat Shouldly accepteert.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0, scratchpad `probe/`): Invoking/Awaiting + Throw/ThrowAsync/NotThrow geven dezelfde pass/fail als de Shouldly-vorm met `() => x.M()`; de actie draait één keer.

## Risks

- Een local die tussen `Invoking` en het uitvoeren wordt gewijzigd kan niet: beide gebeuren in dezelfde expressie.
- Properties worden geweigerd, ook als ze geen bijwerkingen hebben (shortcut in de code).

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-invoking-modellen.md](fa-invoking-modellen.md).
