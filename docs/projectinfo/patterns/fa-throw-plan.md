---
kingmadoc: 1
feature: "fa-throw"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10, REQ-11, REQ-12, REQ-13]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/Throw/
---
# Feature: fa.throw: act.Should().Throw<T>() (+ WithMessage) wordt Should.Throw<T>(act) (+ Message-check)

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-008. Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.throw: `act.Should().Throw<T>()` wordt `Should.Throw<T>(act)`; een direct volgende `.WithMessage(patroon)` wordt een `.Message.ShouldContain/StartWith/EndWith/Be(...)` op hetzelfde statement.

## Scope (in / out)

**In scope**

- Losse `Throw<T>()` op een `Action` of `Func<T>` (T referentietype), met optionele `because` zonder `becauseArgs`.
- Een direct volgende `.WithMessage(p[, because])` met een constante string `p` zonder andere wildcards dan een voorloop- en/of afsluitende `*`.
- `Manual(reden)` voor `ThrowExactly`, ketens, gebruikt resultaat, `Invoking(...)`, andere subjecttypes en niet-vertaalbare patronen.

**Out of scope**

- `ThrowAsync` (rij 7), `NotThrow` (rij 8), `.WithInnerException`, `.Where`, `.WithParameterName` (blijven Manual).
- Beheer van `using`-directives: de `Fixer` doet dat via `IFix`.

## Requirements

- **REQ-1**: WHEN `act.Should().Throw<T>()` als statement op een `Action` staat THE SYSTEM SHALL het vervangen door `Should.Throw<T>(act)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/simple.*.cs`).
- **REQ-2**: WHEN het subject een `Func<T>` met T een referentietype is THE SYSTEM SHALL `Should.Throw<T>(func)` geven (proof `.../Throw/func.*.cs`).
- **REQ-3**: WHEN een `because` zonder `becauseArgs` staat THE SYSTEM SHALL die als tweede argument (`customMessage`) van `Should.Throw` overnemen (proof `.../Throw/because.*.cs`).
- **REQ-4**: WHEN de assertion de body van een void-lambda is THE SYSTEM SHALL het herschrijven (proof `.../Throw/lambda.*.cs`).
- **REQ-5**: WHEN `.WithMessage("*x*")` direct op `Throw<T>()` volgt THE SYSTEM SHALL `Should.Throw<T>(act).Message.ShouldContain("x", Shouldly.Case.Insensitive)` geven (proof `.../Throw/message-contains.*.cs`).
- **REQ-6**: WHEN het patroon `x*` of `*x` is THE SYSTEM SHALL `ShouldStartWith` resp. `ShouldEndWith` met `Case.Insensitive` gebruiken (proofs `message-starts.*`, `message-ends.*`).
- **REQ-7**: WHEN het patroon geen wildcards bevat THE SYSTEM SHALL `ShouldBe(x, Shouldly.StringCompareShould.IgnoreCase)` geven (proof `message-exact.*`).
- **REQ-8**: WHEN `WithMessage` een `because` heeft THE SYSTEM SHALL die als `customMessage` van de message-check overnemen; bij `ShouldBe` vóór de `StringCompareShould` (proofs `message-because.*`, `message-exact-because.*`).
- **REQ-9**: IF `ThrowExactly<T>` wordt gebruikt THEN THE SYSTEM SHALL Manual geven, omdat Shouldly's `Throw<T>` ook afgeleide typen accepteert (proof `throw-exactly.*`).
- **REQ-10**: IF `becauseArgs` worden gebruikt (op `Throw` of `WithMessage`) THEN THE SYSTEM SHALL Manual geven (proofs `because-args.*`, `message-because-args.*`).
- **REQ-11**: IF het patroon een `*`/`?` midden in de tekst of een `?` bevat, of geen string-constante is, THEN THE SYSTEM SHALL Manual geven (proofs `message-wildcard.*`, `message-variable.*`).
- **REQ-12**: IF het resultaat wordt gebruikt of verder geketend (`.Which`, `.WithInnerException`, variabele) THEN THE SYSTEM SHALL Manual geven (proofs `chain.*`, `value-used.*`); ook als de assertion zelf een vervolg is (`x.Should().A().And.Throw<T>()`, geen proof: dit compileert niet met FA 7.2.2).
- **REQ-13**: IF het subject met `Invoking(...)` is gebouwd, of een `Func<ValueType>` is THEN THE SYSTEM SHALL Manual geven (proofs `invoking.*`, `value-func.*`).
- **Uitbreiding ThrowExactly**: `ThrowExactly<T>` wordt niet meer Manual: `ThrowExactlyPattern` (`fa.throw-exactly`) vindt het en laat deze `Rewrite` `Should.Throw<T>(act).ShouldBeOfType<T>()` bouwen (proofs in `ThrowExactly/`). Vervangt REQ-9; er is geen `Throw/throw-exactly.*`-proof.
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan (op `Throw` of `WithMessage`) THE SYSTEM SHALL `string.Format(CultureInfo.InvariantCulture, because, args)` als `customMessage` geven (proofs `because-args.*`, `message-because-args.*`, nu `.after.cs`); named arguments of een niet-constante `because` blijven Manual. Vervangt REQ-10.
- **Uitbreiding TAAK-096**: WHEN `.Which` direct op `Throw<T>()` volgt THE SYSTEM SHALL `.Which` laten vervallen, want `Should.Throw<T>` geeft de exception terug (proofs `value-used.*`, `edge-which-message.*`, nu Fixed); `.WithInnerException`, `.And`, `.Where` blijven Manual (proofs `chain.*`, `edge-inner-exception.*`). Vervangt het `.Which`-deel van REQ-12.
- **Uitbreiding Invoking**: `x.Invoking(s => ...)`-subjects vindt `fa.invoking` (`InvokingPattern`), dat deze `Rewrite` aanroept: stabiele referentie-receiver plus lambda wordt `Should.Throw<T>(() => ...)` (proofs `Invoking/simple`, `func`, `throw-exactly`, ...); anders Manual (`Invoking/side-effect-receiver`, `value-type`, `method-group`). Een ander FA-gebouwd subject blijft Manual. Vervangt het Invoking-deel van REQ-13; er is geen `Throw/invoking.*`-proof.
- **Uitbreiding AggregateException**: IF de actie zichtbaar blokkeert op een taak of `Parallel.*` gebruikt (`Wait`, `Result`, `WaitAll`, ...) en T geen `AggregateException` accepteert THEN THE SYSTEM SHALL Manual geven, omdat FA `AggregateException` uitpakt en Shouldly niet (proofs `aggregate-wait.*`, `aggregate-method-group.*`); `GetAwaiter().GetResult()` of een verwachte `AggregateException` blijft Fixed (proofs `aggregate-get-result.*`, `aggregate-expected.*`). Geldt niet voor ThrowExactly.
- **Uitbreiding regeleinden**: WHEN het `WithMessage`-patroon zonder wildcards een regeleinde bevat THE SYSTEM SHALL `ShouldBe(lit, StringCompareShould.IgnoreCase | StringCompareShould.IgnoreLineEndings)` geven (proofs `message-line-break.*`, `edge-multiline-message.*`); met een `*` erbij Manual (proof `message-contains-line-break.*`).
- **Uitbreiding TAAK-100**: IF de assertie in een niet omgezette `AssertionScope` staat THEN THE SYSTEM SHALL Manual "Inside an AssertionScope" geven (proofs `ThrowExactly/assertion-scope.*`, `Invoking/assertion-scope.*`).
- **Uitbreiding TAAK-170**: de hele `Throw<T>()`-keten wordt omgezet via `ThrowChains`: `.And`/`.Which` → de teruggegeven exception, `.WithMessage`/`.WithParameterName` → checks op `Message`/`ParamName`, `.Where(x => p)` → `(p[x := e]).ShouldBeTrue()`, `.WithInnerException<TI>()` → `e.InnerException.ShouldNotBeNull().ShouldBeAssignableTo<TI>()`, `.WithInnerExceptionExactly<TI>()` → `ShouldBeOfType<TI>()`. Een check die niet de laatste stap is splitst het statement (`var exception = …;`); `var ex = <keten>;` blijft als elk gebruik van `ex` zelf een keten is. Proofs `Throw/result-*`, `chain`, `edge-inner-exception`; doorgegeven of geretourneerd resultaat blijft Manual (`result-passed`, `result-local-passed`). Vervangt REQ-12.
- **Uitbreiding TAAK-220**: IF de actie blokkeert op een task (`Wait`/`.Result`/`WaitAll`/`Parallel.*`), ook in een methode uit de eigen solution die zij aanroept (twee niveaus diep), en `T` is geen `AggregateException` (of afgeleid) THEN THE SYSTEM SHALL Manual geven; is `T` een basis van `AggregateException` (`Exception`), dan alleen als de keten de exception gebruikt (`.And`/`.Which`/stap/lokale): FA geeft de uitgepakte inner exception, Shouldly de `AggregateException` (probe). Proofs `Throw/aggregate-helper-method`, `aggregate-exception-and` (Manual), `aggregate-exception-plain` (Fixed); `Invoking/aggregate-parameter-and`, `aggregate-parameter-message` (lambda-parameter als receiver).
- **Uitbreiding TAAK-222**: IF `T` een assertion-exception is (`XunitException`, `AssertionFailedException`, `AssertionException`, `AssertFailedException` of afgeleid) en de actie een methode uit de eigen solution aanroept met een FA-assertion THEN THE SYSTEM SHALL de `Throw` Manual geven, en ook de FA-assertions in die helper (`IsInsideExpectedFailure`, alle patronen), zodat test en helper samen FA blijven. Proofs `Throw/assertion-helper`, `BeTrueFalse/assertion-helper`; unittests `FluentAssertionsSyntaxTests` (helper in een ander bestand).

## Assumptions

- Shouldly 4.3.0 `Should.Throw<T>(Action|Func<object?>, string? customMessage)` accepteert afgeleide exceptions, net als FA's `Throw<T>` (vastgesteld met een proefprogramma).
- FA's `WithMessage` vergelijkt het hele bericht met een hoofdletterongevoelig wildcardpatroon; de vertalingen met `Case.Insensitive` doen hetzelfde.
- De `Fixer` beheert `using Shouldly;`/`using FluentAssertions;` via `IFix`.

## Risks

- Foutmelding krijgt "Additional Info:" in plaats van FA's "because"-voorvoegsel: gedocumenteerd verschil.
- Dit pattern vervangt een heel statement-expressie (subject wordt argument); bij uitvoer met afwijkende opmaak (meerregelig) kan de spacing veranderen.
- `Func<int>` valt onder Manual omdat Shouldly `Func<object?>` verwacht.

## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-throw-modellen.md](fa-throw-modellen.md).

## Open questions

- Geen.
