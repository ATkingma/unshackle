# Modellen: fa.throw

act.Should().Throw<T>() naar Should.Throw<T>(act). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs`.

**Figure 1.** Klassen rond `ThrowPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-throw-modellen-1.d2 -->
![Modellen: fa.throw](img/fa-throw-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `ThrowPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs` | Dit patroon, Id `fa.throw` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `IsFluentAssertions`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `Throw<T>()`; `ThrowExactly<T>()`; `WithMessage(pattern)` |
| «Shouldly» aangemaakt | - | `Should.Throw<T>(act[, because])`; `thrown.Message.ShouldContain / ShouldStartWith / ShouldEndWith / ShouldBe` |
| Herkende typen | - | `Action`; `Func<T> met referentietype`; `Invoking(...) (Manual)` |
| `ThrowMessage` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowMessage.cs` | Gedeeld met `ThrowAsyncPattern`: vertaalt de `WithMessage`-wildcard naar `Message.ShouldX(...)` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `Rewrite` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-throw-modellen-2.d2 -->
![Modellen: fa.throw](img/fa-throw-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `ThrowPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `Throw`, `ThrowExactly` waarvan het symbool in een FluentAssertions-namespace ligt ; een direct volgende `WithMessage` wordt de node |
| Controles | `ThrowPattern.Rewrite` | `GetSubject(throwCall)`; `IsResultDiscarded(outer, model)`; `IsFluentAssertions(GetSymbolInfo(subject))`; `GetTypeInfo(subject)`; `TryGetArguments(throwCall, model)`; `Arguments([subject], because)`; `GetConstantValue(pattern)`; `Check(result, pattern, because)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `Rewrite`: 10 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-throw-modellen-3.d2 -->
![Modellen: fa.throw](img/fa-throw-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Throw<T>, niet ThrowExactly? | ThrowExactly<T> requires the exact exception type | `throw-exactly` |
| Precies één typeargument? | Throw without exactly one type argument | geen proof, bewaakt in code |
| Vorm x.Should().Throw<T>()? | Throw<T>(...) is not in the form x.Should().Throw<T>() | geen proof, bewaakt in code |
| Resultaat weggegooid? | The result of Throw<T>() is used (.Which, .And, ...) | `chain`, `value-used` |
| Subject niet gebouwd met Invoking(...)? | The subject is built with Invoking(...) | `invoking` |
| Subject is Action of Func naar referentietype? | The subject is not an Action or a Func returning a reference type | `value-func` |
| Geen becauseArgs, geen waarde-argumenten op Throw? | becauseArgs of benoemde argumenten | `because-args` |
| Geen becauseArgs, één waarde op WithMessage? | WithMessage(...) with becauseArgs | `message-because-args` |
| Patroon is een constante string? | pattern is not a string constant | `message-variable` |
| Alleen een * vooraan of achteraan? | wildcards other than a leading or trailing * | `message-wildcard` |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| geen WithMessage | `Should.Throw<T>(act[, because])` | `simple`, `because`, `func`, `lambda` |
| WithMessage *lit* | `Should.Throw<T>(act).Message.ShouldContain(lit, Case.Insensitive)` | `message-contains` |
| WithMessage lit* | `...Message.ShouldStartWith(lit, Case.Insensitive)` | `message-starts` |
| WithMessage *lit | `...Message.ShouldEndWith(lit, Case.Insensitive)` | `message-ends` |
| WithMessage lit | `...Message.ShouldBe(lit[, because], StringCompareShould.IgnoreCase)` | `message-exact`, `message-exact-because`, `message-because` |

## Zie ook

- [fa-throw-plan.md](fa-throw-plan.md) - plan
- [fa-throw-verify.md](fa-throw-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
