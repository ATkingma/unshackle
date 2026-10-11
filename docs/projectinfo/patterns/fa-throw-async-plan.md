---
kingmadoc: 1
feature: "fa-throw-async"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/
---
# Feature: fa.throw-async: await act.Should().ThrowAsync<T>() (+ WithMessage) wordt await Should.ThrowAsync<T>(act) (+ Message-check)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-012, voortbouwend op `fa.throw` (TAAK-008).

## One-sentence summary

fa.throw-async: `await act.Should().ThrowAsync<T>()` wordt `await Should.ThrowAsync<T>(act)`; een direct volgende `.WithMessage(patroon)` wordt `(await Should.ThrowAsync<T>(act)).Message.ShouldContain/StartWith/EndWith/Be(...)`.

## Scope (in / out)

**In scope**

- Een als statement geawaite `ThrowAsync<T>()` op een `Func<Task>` of `Func<Task<T>>` (T referentietype), met optionele `because` zonder `becauseArgs`.
- Een direct volgende `.WithMessage(p[, because])` met een constante string `p` zonder andere wildcards dan een voorloop- en/of afsluitende `*`.
- `Manual(reden)` voor `ThrowExactlyAsync`, niet direct geawaite aanroepen, gebruikt resultaat, `Awaiting(...)`/`Invoking(...)`, andere subjecttypes en niet-vertaalbare patronen.

**Out of scope**

- `Func<ValueTask>` (FA 7.2.2 heeft er geen `Should()` voor), `NotThrowAsync`, `ThrowWithinAsync`, `.WithInnerException`, `.Where` (blijven Manual).
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `await act.Should().ThrowAsync<T>()` als statement op een `Func<Task>` staat THE SYSTEM SHALL het vervangen door `await Should.ThrowAsync<T>(act)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/simple.*.cs`).
- **REQ-2**: WHEN het subject een `Func<Task<T>>` met T een referentietype is THE SYSTEM SHALL `await Should.ThrowAsync<T>(func)` geven (proof `.../ThrowAsync/func-task-reference.*.cs`).
- **REQ-3**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als tweede argument van `Should.ThrowAsync` overnemen (proof `.../because.*.cs`).
- **REQ-4**: WHEN `.WithMessage("*x*")` direct op `ThrowAsync<T>()` volgt THE SYSTEM SHALL `(await Should.ThrowAsync<T>(act)).Message.ShouldContain("x", Shouldly.Case.Insensitive)` geven (proof `.../message-contains.*.cs`).
- **REQ-5**: WHEN het patroon `x*` of `*x` is THE SYSTEM SHALL `ShouldStartWith` resp. `ShouldEndWith` met `Case.Insensitive` gebruiken (proofs `message-starts.*`, `message-ends.*`).
- **REQ-6**: WHEN het patroon geen wildcards bevat THE SYSTEM SHALL `ShouldBe(x, Shouldly.StringCompareShould.IgnoreCase)` geven (proof `message-exact.*`).
- **REQ-7**: WHEN `WithMessage` een `because` heeft THE SYSTEM SHALL die als `customMessage` van de message-check overnemen; bij `ShouldBe` vóór de `StringCompareShould` (proofs `message-because.*`, `message-exact-because.*`).
- **REQ-8**: IF `ThrowExactlyAsync<T>` wordt gebruikt THEN THE SYSTEM SHALL Manual geven (proof `throw-exactly.*`).
- **REQ-9**: IF `becauseArgs` worden gebruikt (op `ThrowAsync` of `WithMessage`) THEN THE SYSTEM SHALL Manual geven (proofs `because-args.*`, `message-because-args.*`).
- **REQ-10**: IF het patroon een `*`/`?` midden in de tekst bevat, of geen string-constante is, THEN THE SYSTEM SHALL Manual geven (proofs `message-wildcard.*`, `message-variable.*`).
- **REQ-11**: IF de aanroep niet direct geawaite wordt (geen `await`, of gevolgd door `.Where`/…) THEN THE SYSTEM SHALL Manual geven (proofs `no-await.*`, `where.*`).
- **REQ-12**: IF de uitkomst van de await wordt gebruikt (variabele) THEN THE SYSTEM SHALL Manual geven (proof `value-used.*`).
- **REQ-13**: IF het subject met `Awaiting(...)` is gebouwd, of een `Func<Task<ValueType>>` is THEN THE SYSTEM SHALL Manual geven (proofs `awaiting.*`, `value-func.*`).

## Assumptions

- Shouldly 4.3.0 heeft `Should.ThrowAsync<T>(Func<Task>, string? customMessage)`; `Func<Task<T>>` met T referentietype converteert via covariantie. Net als `Throw<T>` accepteert het afgeleide exceptions (vastgesteld met een proefprogramma).
- FA's `WithMessage` op de taak is een extensiemethode (`ExceptionAssertionsExtensions`) die het hele bericht hoofdletterongevoelig met een wildcard vergelijkt.
- De `Fixer` beheert `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- Foutmelding krijgt "Additional Info:" in plaats van FA's "because"-voorvoegsel: gedocumenteerd verschil.
- Het statement wordt als geheel vervangen (await + keten); afwijkende opmaak kan veranderen.
- `Func<Task<int>>` valt onder Manual omdat er geen implicit conversie naar `Func<Task>` bestaat.

## Modellen

Klassen-, sequence- en beslisdiagram: [fa-throw-async-modellen.md](fa-throw-async-modellen.md).

## Open questions

- Geen.

## Zie ook

- `claude/docs/INDEX.md` — index voor agents
- `claude/docs/patroon-loop.md` — hoe patronen gemaakt en gedocumenteerd worden
- `docs/projectinfo/patterns/fa-throw-plan.md` — het synchrone patroon
