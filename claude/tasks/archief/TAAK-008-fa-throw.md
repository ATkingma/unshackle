# TAAK-008 — Patroon fa.throw

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `act.Should().Throw<T>()` (+ `.WithMessage`) wordt `Should.Throw<T>(act)` (+ message-check), met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 6 in `claude/tasks/patroon-queue.md`. Sample: `OrderTests.cs:73`. Voorbeeld: TAAK-007.

## Analyse

### Huidige situatie

FA 7.2.2: `Throw<T>(because, becauseArgs)` op `ActionAssertions`/`FunctionAssertions<T>` (niet op `Func<Task>`), `ThrowExactly<T>`; resultaat `ExceptionAssertions<T>` met `WithMessage(wildcard, because, becauseArgs)` (hoofdletterongevoelig, heel bericht), `WithInnerException`, `Where`, `.Which`/`.And`.

### Probleem

De subject wordt een argument (vorm verandert) en `WithMessage` heeft geen directe Shouldly-tegenhanger (`ShouldMatch` is regex).

### Nieuwe situatie

`ThrowPattern` (`fa.throw`): Find geeft de `.WithMessage(...)`-aanroep als die direct op `Throw` volgt, anders de `Throw`-aanroep. Eén statement blijft één statement.

### Verbetering

Het meest voorkomende patroon uit de sample wordt zonder handwerk omgezet.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Shouldly `Should.Throw<T>` accepteert afgeleide types (met proefprogramma vastgesteld); `Throw<T>` → Fixed, `ThrowExactly<T>` → Manual | Gelijk gedrag voor `Throw<T>`; `ThrowExactly` heeft geen equivalent |
| 2 | `Throw<T>().WithMessage(p)` → `Should.Throw<T>(act).Message.ShouldX(...)` in één statement | Geen splitsing nodig; Shouldly's `Throw` geeft de exception terug |
| 3 | Patroon alleen als string-constante; `*x*` → `ShouldContain`, `x*` → `ShouldStartWith`, `*x` → `ShouldEndWith` (allemaal `Shouldly.Case.Insensitive`), `x` → `ShouldBe(x, StringCompareShould.IgnoreCase)`; overig (`?`, `*` in het midden, alleen `*`) → Manual | FA's `WithMessage` is een hoofdletterongevoelig wildcard over het hele bericht; Shouldly's `ShouldMatch` is regex en zou stil anders gedragen |
| 4 | `because` van `Throw` → 2e argument van `Should.Throw`; van `WithMessage` → `customMessage` van de check (bij `ShouldBe` vóór `StringCompareShould`: overload `(string, string, StringCompareShould)`) | Gecontroleerd tegen Shouldly 4.3.0-overloads |
| 5 | Subject: `Action`, of `Func<T>` met T referentietype; `Func<ValueType>` en `Invoking(...)` → Manual | `Should.Throw` heeft alleen `Action`/`Func<object?>`; `Func<Task>` heeft geen `Throw` in FA |
| 6 | Resultaat gebruikt/keten (`.Which`, `.WithInnerException`, `.Where`) → Manual | Geen eenduidige vorm; `.Which` kan later |
| 7 | Manual-redenen: "is used", "Invoking", "reference type", "wildcards other than", "not a string constant", "becauseArgs", "requires the exact exception type" | `ProofTests` eist één reden per proof |
| 8 | Geen proof voor `.And.Throw` (compileert niet met FA 7.2.2) | `ProofTests` eist dat before compileert |

## Stappen

- [x] 1. Claimen en taak aanmaken
- [x] 2. Onderzoek en keuzes
- [x] 3. KingmaDoc-plan (13 REQ)
- [x] 4. `ThrowPattern` + registratie
- [x] 5. Proofs (19 cases in `Throw/`)
- [x] 6. Build/test groen (98 + 15 sample), verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/`
- `docs/projectinfo/patterns/fa-throw-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-007-fa-contain.md`
