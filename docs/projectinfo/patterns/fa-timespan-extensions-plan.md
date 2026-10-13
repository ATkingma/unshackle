---
kingmadoc: 1
feature: "fa-timespan-extensions"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/TimeSpanExtensionsPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Tests/Core/TimeSpanExtensionsPatternTests.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/TimeSpanExtensions/
---

# Feature: fa.timespan-extensions: `5.Seconds()` → `TimeSpan.FromSeconds(5)`

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-13 by KingmaDoc |

> Opgesteld uit TAAK-224 (FA-corpusrun ronde 4: 380 unmatched-use in Akka en Steeltoe, 23 in EventFlow voor de maand-extensies).

## One-sentence summary

De tijdextensies uit `FluentAssertions.Extensions` (`FluentTimeSpanExtensions`, en de maanden van `FluentDateTimeExtensions`) worden de BCL-aanroep die FluentAssertions zelf doet, zodat FA niet alleen hierom in het project blijft.

## Scope (in / out)

**In scope**

- `n.Ticks()`, `Milliseconds()`, `Seconds()`, `Minutes()`, `Hours()`, `Days()` op `int`/`long`/`double` → `TimeSpan.FromTicks/FromMilliseconds/FromSeconds/FromMinutes/FromHours/FromDays(n)`; ook de statische vorm `FluentTimeSpanExtensions.Seconds(5)`.
- `d.March(y)` (alle twaalf maanden) → `new DateTime(y, 3, d)` als dag of jaar constant is.
- `FluentAssertions.Extensions` in `RemovedNamespaces` van de fix, zodat de using verdwijnt als niets hem nog nodig heeft.

**Out of scope (Manual)**

- `Seconds(n, offset)`, `And(..)`, `Nanoseconds`/`Microseconds`, `At`, `AsUtc`/`AsLocal`, `Before`/`After`, `WithOffset`, `AddNanoseconds`/`AddMicroseconds`.
- Een maand met berekende dag én berekend jaar (evaluatievolgorde draait om).

## Requirements

- **REQ-1**: WHEN `n.Seconds()` (of Ticks/Milliseconds/Minutes/Hours/Days) met één getal staat THE SYSTEM SHALL `TimeSpan.FromSeconds(n)` geven, haakjes om n weg (proof `simple`).
- **REQ-2**: WHEN de statische vorm `FluentTimeSpanExtensions.Seconds(5)` staat THE SYSTEM SHALL hetzelfde geven (proof `static-call`).
- **REQ-3**: WHEN `TimeSpan`/`DateTime` op die plek niet bindt THE SYSTEM SHALL `using System;` toevoegen (proof `no-using-system`); WHEN een eigen type de naam vangt THE SYSTEM SHALL `global::System.TimeSpan` schrijven (unittest `Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan`).
- **REQ-4**: WHEN `d.March(y)` met een constante dag of jaar staat THE SYSTEM SHALL `new DateTime(y, 3, d)` geven (proof `month`).
- **REQ-5**: IF dag en jaar allebei berekend zijn THEN THE SYSTEM SHALL Manual geven (proof `month-computed`).
- **REQ-6**: IF een andere extensie uit `FluentAssertions.Extensions` staat (`And`, `Seconds(n, offset)`, `At`, …) THEN THE SYSTEM SHALL Manual geven (proofs `and`, `offset`; unittest `Rewrite_DateTimeAt_Manual`).
- **REQ-7**: WHEN een eigen `Seconds()`-extensie bindt THE SYSTEM SHALL niets vinden (unittest `Find_OwnSecondsExtension_FindsNothing`).

## Aannames

- FA 7.2.2 implementeert `Seconds(int)` als `TimeSpan.FromSeconds(seconds)` en `March(day, year)` als `new DateTime(year, 3, day)`. In .NET 9+ kiest de nieuwe code de `long`-overload van `TimeSpan.From*`; voor gehele waarden is dat exact gelijk aan de `double`-overload.

## Risks

- De Fixer zet de verwijderde using in plaats om naar `using Shouldly;`, ook in een bestand zonder asserts (onschuldig: het project krijgt Shouldly).

## Modellen

[fa-timespan-extensions-modellen.md](fa-timespan-extensions-modellen.md).

## Open questions

- Geen.
