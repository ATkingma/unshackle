# Modellen: fa.have-count

x.Should().HaveCount(n) naar x.Count.ShouldBe(n). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs`.

**Figure 1.** Klassen rond `HaveCountPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-have-count-modellen-1.d2 -->
![Modellen: fa.have-count](img/fa-have-count-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `HaveCountPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` | Dit patroon, Id `fa.have-count`; privé: `Member(expression, name)`, `IsSafeReceiver(subject)`, `Implements(type, names)`, `HasIntProperty(type, name)` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `HaveCount(int expected[, because])`; `HaveCount(Expression predicate)` |
| «Shouldly» aangemaakt | - | `Length / Count / Count() + ShouldBe(n[, because])`; `using System.Linq (bij Count())` |
| Herkende typen | - | `IArrayTypeSymbol`; `IDictionary / IReadOnlyDictionary`; `IEnumerable<T>`; `publieke int Count of Length` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `Rewrite` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-have-count-modellen-2.d2 -->
![Modellen: fa.have-count](img/fa-have-count-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `HaveCountPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `HaveCount` waarvan het symbool in een FluentAssertions-namespace ligt |
| Controles | `HaveCountPattern.Rewrite` | `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `GetSymbolInfo(assertion)`; `TryGetArguments(assertion, model)`; `GetTypeInfo(subject)`; `Implements / HasIntProperty / IsSafeReceiver` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node plus namespace `System.Linq` bij `Count()`; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `Rewrite`: 8 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-have-count-modellen-3.d2 -->
![Modellen: fa.have-count](img/fa-have-count-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Vorm x.Should().HaveCount(n)? | HaveCount(...) is not in the form x.Should().HaveCount(n) | geen proof, bewaakt in code |
| Resultaat weggegooid? | The result of HaveCount(...) is used | `chain` |
| Subject zonder ?. (conditional access)? | The subject uses ?. | geen proof, bewaakt in code |
| Eerste parameter is int (geen predicate)? | Predicate: rewrite by hand | `predicate` |
| Geen becauseArgs of benoemde argumenten? | becauseArgs of benoemde argumenten | `because-args` |
| Type van het subject bekend? | The type of the subject is unknown | geen proof, bewaakt in code |
| Geen IDictionary / IReadOnlyDictionary? | Dictionary: check whether Count or Keys is meant | `dictionary` |
| Selector: niet-generieke IEnumerable | Non-generic IEnumerable: use Cast<T>().Count() | geen proof, bewaakt in code |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| array, of alleen publieke int Length | `x.Length.ShouldBe(n)` | `array` |
| publieke int Count | `x.Count.ShouldBe(n[, because]); haakjes om niet-veilige receiver` | `list`, `list-because`, `collection-interfaces`, `receiver-parens` |
| alleen IEnumerable<T> | `x.Count().ShouldBe(n) + using System.Linq` | `enumerable`, `enumerable-because`, `enumerable-existing-using` |

## Zie ook

- [fa-have-count-plan.md](fa-have-count-plan.md) - plan
- [fa-have-count-verify.md](fa-have-count-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
