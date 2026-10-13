---
kingmadoc: 1
feature: "fa-not-throw-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowAsyncPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/NotThrowAsync/
---

# Feature: fa.not-throw-async

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-094 (actie uit `claude/tasks/ANALYSE-fa-100.md`).

## One-sentence summary

`await act.Should().NotThrowAsync()` wordt `await Should.NotThrowAsync(act)`, met `because` en `.ConfigureAwait(..)`.

## Scope (in / out)

**In scope**

- `await act.Should().NotThrowAsync([because])` als statement, ook met `.ConfigureAwait(..)`.
- Subject `Func<Task>` of `Func<Task<T>>` met T een referentietype.

**Out of scope**

- `NotThrowAsync<T>()`, `NotThrowAfterAsync`, niet-awaited of gebruikt resultaat, `Func<Task<int>>`, `FluentActions.Awaiting`, AssertionScope: Manual. (becauseArgs: zie Uitbreiding TAAK-097.)

## Requirements

- **REQ-1**: WHEN `await act.Should().NotThrowAsync()` een statement is THE SYSTEM SHALL `await Should.NotThrowAsync(act)` geven (proof `simple, lambda, reference-result`).
- **REQ-2**: WHEN `because` zonder becauseArgs staat THE SYSTEM SHALL die als customMessage meegeven (proof `because`).
- **REQ-3**: WHEN `.ConfigureAwait(x)` volgt THE SYSTEM SHALL die op `Should.NotThrowAsync` houden (proof `configure-await`).
- **REQ-4**: IF generiek, niet-awaited, resultaat gebruikt, waarde-Task, FA-subject of AssertionScope THEN THE SYSTEM SHALL Manual geven (proof `generic, not-awaited, result-used, value-result, awaiting, assertion-scope`).
- **Uitbreiding TAAK-173**: WHEN het subject `x.Awaiting(s => …)` of `FluentActions.Awaiting(d)` is THE SYSTEM SHALL de actie bouwen zoals `fa.invoking` (`ThrowSyntax.TryGetAction`: stabiele receiver, lambda/method group/delegate-variabele) en `await Should.NotThrowAsync(() => …)` geven; een geschreven lambda mag een `Task<T>` met waardetype teruggeven (proofs `awaiting`, `awaiting-receiver`, `fluent-actions-lambda`, `awaiting-value-result`); een receiver met bijwerkingen blijft Manual (`awaiting-side-effect`). Vervangt het FA-subject-deel van REQ-4.
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als customMessage geven (Fixed, proof `because-args`); benoemde argumenten of een niet-constante `because` blijven Manual.

## Assumptions

- Probe (FA 7.2.2 + Shouldly 4.3.0): beide slagen bij een voltooide taak en falen bij een synchrone throw, een async throw en een AggregateException (Shouldly gooit bij een synchrone throw de originele exceptie; de test is rood in beide).
- Daarom is de AggregateException-heuristiek van `ThrowMessage.MayThrowAggregate` hier niet nodig: NotThrow faalt op elke exceptie.
- Shouldly 4.3.0 (reflectie): `Should.NotThrowAsync(Func<Task>, string?)` en `(Task, string?)`.

## Risks

- Foutmelding bij een synchrone throw is de originele exceptie i.p.v. een assertion-melding.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-not-throw-async-modellen.md](fa-not-throw-async-modellen.md).

## Zie ook

- [fa-not-throw-async-verify.md](fa-not-throw-async-verify.md)
- `claude/tasks/archief/TAAK-094-fa-not-throw-async.md`
- [../README.md](../README.md)
