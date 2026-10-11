---
kingmadoc: 1
feature: "fa-assertion-scope"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/AssertionScope/
---
# Feature: fa.assertion-scope: AssertionScope als Manual melden, assertions erbinnen nooit stil herschrijven

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-031.

## One-sentence summary

fa.assertion-scope: `new AssertionScope(...)` wordt `Manual` met advies `ShouldSatisfyAllConditions` (sinds TAAK-100: een gewone scope met alleen assertions wordt `ShouldSatisfyAllConditions`, zie REQ-7), en elke FA-assertion binnen zo'n scope wordt `Manual` in plaats van `Fixed`, omdat Shouldly bij de eerste fout stopt.

## Scope (in / out)

**In scope**

- Gedeelde check `FluentAssertionsSyntax.IsInsideAssertionScope`: body van `using (new AssertionScope(...))` en statements ná `using var scope = new AssertionScope();` in hetzelfde blok of dieper; ook geneste scopes en `new()` met doeltype.
- Alle FA-patronen vragen de check als eerste stap in `Rewrite`.
- Patroon `fa.assertion-scope`: elke `new AssertionScope(...)` (ook met context-string) → `Manual`.

**Out of scope**

- ~~Automatisch herschrijven naar `ShouldSatisfyAllConditions`~~: sinds TAAK-100 wel voor een gewone scope met alleen FA-assertion-statements; scopes met andere statements, `Discard()` of `AddReportable` blijven Manual.
- Subklassen: `AssertionScope` is `sealed` in FA 7.2.2 (CS0509), dus niet mogelijk.

## Requirements

- **REQ-1** (vervangen door REQ-7/REQ-8): WHEN `using (new AssertionScope())` gebruikt wordt THE SYSTEM SHALL de scope als Manual melden (proof `AssertionScope/using-block`).
- **REQ-2** (vervangen door REQ-7/REQ-8): WHEN `using var scope = new AssertionScope()` of `using AssertionScope scope = new()` gebruikt wordt THE SYSTEM SHALL de scope als Manual melden (proofs `AssertionScope/using-var`, `AssertionScope/target-typed`).
- **REQ-3**: WHEN de scope een context-string krijgt THE SYSTEM SHALL de scope als Manual melden (proof `AssertionScope/context`).
- **REQ-4**: IF een FA-assertion in de body van een scope-`using` staat (ook genest dieper) THEN THE SYSTEM SHALL Manual geven met "Inside an AssertionScope" (proofs `ShouldBe/assertion-scope-using-block`, `ShouldBe/assertion-scope-nested`; unittest `IsInsideAssertionScope_DependsOnEnclosingScope`).
- **REQ-5**: IF een FA-assertion na `using var` met een scope in hetzelfde blok of dieper staat THEN THE SYSTEM SHALL Manual geven (proof `ShouldBe/assertion-scope-using-var`; unittest).
- **REQ-6**: WHEN een assertion vóór de `using var`, na het scope-blok, of in een andere `using` staat THE SYSTEM SHALL het patroon gewoon laten herschrijven (unittest `IsInsideAssertionScope_DependsOnEnclosingScope`, `false`-gevallen).
- **REQ-7** (Uitbreiding TAAK-100): WHEN `using (new AssertionScope()) { ... }` of één `using var s = new AssertionScope();` (variabele verder ongebruikt) zonder argumenten alleen FA-assertion-statements bevat, zonder `await` en ref-locals, THE SYSTEM SHALL de scope vervangen door `x.ShouldSatisfyAllConditions(() => { a; }, () => { b; });` (`x` = eerste subject dat veilig twee keer te evalueren is), waarna de assertion-patronen de lambda's herschrijven (proofs `_Combined/scope-using-block`, `_Combined/scope-using-var`). Probe: beide verzamelen alle fouten.
- **REQ-8** (Uitbreiding TAAK-100): IF de scope een context-string heeft, de scope-variabele gebruikt wordt (`Discard()`), er een ander statement in staat of een `await` THEN THE SYSTEM SHALL Manual geven met advies `ShouldSatisfyAllConditions`, en de assertions erin blijven Manual "Inside an AssertionScope" (proofs `AssertionScope/context`, `discard`, `other-statement`, `await`).

## Assumptions

- Type wordt via het semantisch model herkend (`FluentAssertions.Execution.AssertionScope`), niet op naam.
- Een lambda binnen de scope die later draait valt syntactisch binnen de scope: Manual is dan te voorzichtig, maar veilig.

## Risks

- Meer Manual-meldingen in projecten die veel scopes gebruiken; dat is bewust (geen stille gedragswijziging).

## Modellen

Klassendiagram, sequence en beslisboom: [fa-assertion-scope-modellen.md](fa-assertion-scope-modellen.md).

## Open questions

- Geen.
