# Modellen: fa.not-throw

act.Should().NotThrow() naar Should.NotThrow(act). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs`.

**Figure 1.** Klassen rond `NotThrowPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-not-throw-modellen-1.d2 -->
![Modellen: fa.not-throw](img/fa-not-throw-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `NotThrowPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs` | Dit patroon, Id `fa.not-throw` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `IsFluentAssertions`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `NotThrow([because])`; `NotThrow<T>()`; `NotThrowAfter(...)` |
| «Shouldly» aangemaakt | - | `Should.NotThrow(act[, because])` |
| Herkende typen | - | `Action`; `Func<T>`; `Invoking(...) (Manual)` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `Rewrite` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-not-throw-modellen-2.d2 -->
![Modellen: fa.not-throw](img/fa-not-throw-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `NotThrowPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `NotThrow`, `NotThrowAfter` waarvan het symbool in een FluentAssertions-namespace ligt |
| Controles | `NotThrowPattern.Rewrite` | `naam`; `GetSubject(call)`; `IsResultDiscarded(call, model)`; `IsFluentAssertions(GetSymbolInfo(subject))`; `GetTypeInfo(subject)`; `TryGetArguments(call, model)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `Rewrite`: 7 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-not-throw-modellen-3.d2 -->
![Modellen: fa.not-throw](img/fa-not-throw-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Niet NotThrowAfter? | NotThrowAfter waits and polls | `after` |
| Niet generiek NotThrow<T>()? | NotThrow<T>() only forbids exception type T | `generic` |
| Vorm x.Should().NotThrow()? | NotThrow() is not in the form x.Should().NotThrow() | geen proof, bewaakt in code |
| Resultaat weggegooid? | The result of NotThrow() is used | `result-used`, `value-used` |
| Subject niet gebouwd met Invoking(...)? | The subject is built with Invoking(...) | `invoking` |
| Subject is een parameterloze Action of Func? | The subject is not a parameterless Action or Func | geen proof, bewaakt in code |
| Geen becauseArgs, geen waarde-argumenten? | becauseArgs of benoemde argumenten | `because-args` |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| nee | `Should.NotThrow(act)` | `simple`, `func`, `value-func` |
| ja | `Should.NotThrow(act, because)` | `because` |

## Zie ook

- [fa-not-throw-plan.md](fa-not-throw-plan.md) - plan
- [fa-not-throw-verify.md](fa-not-throw-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
