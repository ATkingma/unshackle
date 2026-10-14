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

# Feature: fa.catch-assertion-exception: sites that catch the assertion exception also catch Shouldly

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drafted from TAAK-140 (Polly, 26 regressions).

## One-sentence summary

User code that catches or filters `FluentAssertions.Execution.AssertionFailedException` or `Xunit.Sdk.XunitException` (for example a retry helper) also catches `Shouldly.ShouldAssertException` after the conversion.

## Scope (in / out)

**In scope**

- `catch (T)` without a used variable: an extra `catch (ShouldAssertException)` with the same filter and body after it.
- `e is T` (also in `when (...)` and `!(... || ...)`) with an identifier on the left: `e is T || e is ShouldAssertException`.
- One change per catch site (first matching catch of a try, first `is` of a condition); a site that already mentions `ShouldAssertException` is skipped (second run).
- The old type stays: assertions not yet converted (Manual) are still caught. As a result, the `FluentAssertions.Execution` using and the package stay as well.

**Out of scope**

- Derived exception types, NUnit/MSTest exceptions, `as T`, `typeof(T)`.

## Requirements

- **REQ-1**: WHEN a condition filters `e is AssertionFailedException` THE SYSTEM SHALL add `|| e is ShouldAssertException` (proofs `retry-filter`, `when-filter`).
- **REQ-2**: WHEN a `catch (AssertionFailedException)` does not use the variable THE SYSTEM SHALL add a `catch (ShouldAssertException)` with the same body (proof `catch-clause`).
- **REQ-3**: WHEN the catch variable is used, or it is an is-pattern (`e is T x`, `is not T`) THE SYSTEM SHALL produce Manual with an explanation (proof `catch-uses-exception`, unit test `Rewrite_IsPatternWithDesignation_Manual`).
- **REQ-4**: WHEN one condition mentions both the FA and the xunit exception (Polly) THE SYSTEM SHALL add Shouldly once (unit test `Rewrite_FilterOnFluentAssertionsAndXunit_AddsShouldlyOnce`).
- **REQ-5**: WHEN the site already mentions `ShouldAssertException` THE SYSTEM SHALL find nothing (unit test `Find_SiteAlreadyCatchesShouldly_FindsNothing`).

## Risks

- The FA package stays in a project with such a catch as long as the type is mentioned; the user removes the old type once all assertions are converted.

## Models

[fa-catch-assertion-exception-modellen.md](fa-catch-assertion-exception-modellen.md).

## Open questions

- None.
