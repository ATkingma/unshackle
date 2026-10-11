---
kingmadoc: 1
feature: "fa-catch-assertion-exception"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CatchAssertionExceptionPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Core/CatchAssertionExceptionPatternTests.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/CatchAssertionException/
---

# Feature: fa.catch-assertion-exception: catch-plekken van de assertie-exception vangen ook Shouldly

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-140 (Polly, 26 regressies).

## One-sentence summary

Eigen code die `FluentAssertions.Execution.AssertionFailedException` of `Xunit.Sdk.XunitException` vangt of filtert (bijvoorbeeld een retry-helper) vangt na de omzetting ook `Shouldly.ShouldAssertException`.

## Scope (in / out)

**In scope**

- `catch (T)` zonder gebruikte variabele: extra `catch (ShouldAssertException)` met dezelfde filter en body erachter.
- `e is T` (ook in `when (...)` en `!(... || ...)`) met een identifier links: `e is T || e is ShouldAssertException`.
- Eén wijziging per catch-plek (eerste passende catch van een try, eerste `is` van een conditie); een plek die `ShouldAssertException` al noemt wordt overgeslagen (tweede run).
- Het oude type blijft staan: nog niet omgezette (Manual) assertions worden nog gevangen. De using van `FluentAssertions.Execution` en het pakket blijven daardoor staan.

**Out of scope**

- Afgeleide exceptiontypes, NUnit/MSTest-excepties, `as T`, `typeof(T)`.

## Requirements

- **REQ-1**: WHEN een conditie `e is AssertionFailedException` filtert THE SYSTEM SHALL `|| e is ShouldAssertException` toevoegen (proofs `retry-filter`, `when-filter`).
- **REQ-2**: WHEN een `catch (AssertionFailedException)` de variabele niet gebruikt THE SYSTEM SHALL een `catch (ShouldAssertException)` met dezelfde body toevoegen (proof `catch-clause`).
- **REQ-3**: WHEN de catch-variabele wordt gebruikt, of het een is-pattern is (`e is T x`, `is not T`) THE SYSTEM SHALL Manual geven met uitleg (proof `catch-uses-exception`, unittest `Rewrite_IsPatternWithDesignation_Manual`).
- **REQ-4**: WHEN één conditie zowel de FA- als de xunit-exception noemt (Polly) THE SYSTEM SHALL Shouldly één keer toevoegen (unittest `Rewrite_FilterOnFluentAssertionsAndXunit_AddsShouldlyOnce`).
- **REQ-5**: WHEN de plek `ShouldAssertException` al noemt THE SYSTEM SHALL niets vinden (unittest `Find_SiteAlreadyCatchesShouldly_FindsNothing`).

## Risks

- Het FA-pakket blijft in een project met zo'n catch staan zolang het type genoemd wordt; de gebruiker haalt het oude type weg als alle assertions om zijn.

## Modellen

[fa-catch-assertion-exception-modellen.md](fa-catch-assertion-exception-modellen.md).

## Open questions

- Geen.
