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
- **Uitbreiding ThrowExactly**: `ThrowExactlyAsync<T>` wordt niet meer Manual: `fa.throw-exactly` vindt het en laat deze `Rewrite` `(await Should.ThrowAsync<T>(act)).ShouldBeOfType<T>()` bouwen (proofs `ThrowExactly/async.*`, `async-message.*`). Vervangt REQ-8; er is geen `ThrowAsync/throw-exactly.*`-proof.
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan (op `ThrowAsync` of `WithMessage`) THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als `customMessage` geven (proofs `because-args.*`, `message-because-args.*`, nu `.after.cs`); named arguments of een niet-constante `because` blijven Manual. Vervangt REQ-9.
- **Uitbreiding Awaiting**: `x.Awaiting(s => ...)`-subjects vindt `fa.invoking` (`InvokingPattern`), dat deze `Rewrite` aanroept: stabiele referentie-receiver plus lambda wordt `await Should.ThrowAsync<T>(async () => ...)` (proofs `Invoking/awaiting.*`, `awaiting-exactly.*`); anders Manual. Vervangt het Awaiting-deel van REQ-13; er is geen `ThrowAsync/awaiting.*`-proof.
- **Uitbreiding ConfigureAwait**: WHEN `.ConfigureAwait(...)` tussen de aanroep en de `await` staat THE SYSTEM SHALL die op `Should.ThrowAsync` houden (proof `edge-configure-await.*`).
- **Uitbreiding AggregateException**: IF de actie zichtbaar blokkeert (`.Result`, `Wait`, ...) en T geen `AggregateException` accepteert THEN THE SYSTEM SHALL Manual geven (proof `aggregate-result.*`); een geawaite taak blijft Fixed (proof `aggregate-awaited.*`). Niet bij ThrowExactlyAsync.
- **Uitbreiding regeleinden**: WHEN het `WithMessage`-patroon zonder wildcards een regeleinde bevat THE SYSTEM SHALL `ShouldBe(lit, StringCompareShould.IgnoreCase | StringCompareShould.IgnoreLineEndings)` geven (proof `message-line-break.*`); met een `*` erbij Manual (proof `message-contains-line-break.*`).
- **Uitbreiding TAAK-100**: IF de assertie in een niet omgezette `AssertionScope` staat THEN THE SYSTEM SHALL Manual "Inside an AssertionScope" geven (geen eigen proof; zie `ThrowExactly/assertion-scope`).
- **Uitbreiding TAAK-170**: dezelfde ketenomzetting als `fa.throw` (`ThrowChains`) op `await Should.ThrowAsync<T>(…)`; proofs `ThrowAsync/result-*`, `where`, `value-used`.
- **Uitbreiding TAAK-221**: IF `T` `OperationCanceledException` is of daarvan afgeleid, en er volgt een stap (`WithMessage`/`WithInnerException`/`Where`) of het is `ThrowExactlyAsync` (behalve exact `TaskCanceledException`) THEN THE SYSTEM SHALL Manual geven: eindigt de task geannuleerd, dan geeft Shouldly's `ThrowAsync` een nieuwe `TaskCanceledException` (zonder inner exception) terug (probe Shouldly 4.3.0). `.And.CancellationToken` blijft Fixed (de nieuwe exception krijgt het token van de task). Proofs `ThrowAsync/cancelled-inner`, `ThrowExactly/async-cancelled` (Manual), `ThrowExactly/async-task-canceled`, `ThrowAsync/result-local-and` (Fixed).
- **Uitbreiding TAAK-220 (async)**: IF de actie een lambda `x => x.WaitAsync(..)` is (wacht op een elders gemaakte task) en `T` geen `AggregateException` is THEN THE SYSTEM SHALL Manual geven: is die task gefaald met een geneste `AggregateException`, dan vlakt FA die af en Shouldly niet (probe). Proof `Invoking/awaiting-wait-async`.

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
