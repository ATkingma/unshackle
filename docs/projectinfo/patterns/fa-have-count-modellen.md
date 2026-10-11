# Modellen: fa.have-count

x.Should().HaveCount(n) naar x.Count.ShouldBe(n). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs`.

**Figure 1.** Klassen rond `HaveCountPattern`: wat hij erft, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-have-count-modellen-1.d2 -->
![Modellen: fa.have-count](img/fa-have-count-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet scope, vorm, resultaat en because(-args), dan `RewriteAssertion` |
| `HaveCountPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` | Dit patroon, Id `fa.have-count`; `RewriteAssertion`; privé: `Member(expression, name)`, `Implements(type, names)`, `HasIntProperty(type, name)` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `NotNullReceiver` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `HaveCount(int expected[, because])`; `HaveCountGreaterThan`/`…OrEqualTo`/`HaveCountLessThan`/`…OrEqualTo` (ook `…GreaterOrEqualTo`/`…LessOrEqualTo`); `HaveCount(Expression predicate)` |
| «Shouldly» aangemaakt | - | `Length / Count / Count()` + `ShouldBe` / `ShouldBeGreaterThan(OrEqualTo)` / `ShouldBeLessThan(OrEqualTo)` `(n[, because])`; `using System.Linq` (bij `Count()`) |
| Herkende typen | - | `IArrayTypeSymbol`; `IDictionary / IReadOnlyDictionary`; `IEnumerable<T>`; `publieke int Count of Length` |

**Figure 2.** `Fixer` roept `Find` en per node `FluentAssertionPattern.Rewrite` aan; die doet de gedeelde controles en roept `HaveCountPattern.RewriteAssertion` aan, dat `Fixed` of `Manual` levert. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-have-count-modellen-2.d2 -->
![Modellen: fa.have-count](img/fa-have-count-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `HaveCountPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt `HaveCount` en de zes `HaveCount…Than…`-namen waarvan het symbool in een FluentAssertions-namespace ligt |
| Gedeelde controles | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)` (becauseArgs met constante because wordt `string.Format(CultureInfo.InvariantCulture, ...)`) |
| Eigen controles | `HaveCountPattern.RewriteAssertion` | `?.`; `GetSymbolInfo(assertion)` (eerste parameter int); `GetTypeInfo(subject)`; `Implements / HasIntProperty`; `NotNullReceiver` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node plus namespace `System.Linq` bij `Count()`; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` + `HaveCountPattern.RewriteAssertion`: 8 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`). `<Should>` is `ShouldBe` of de vergelijking bij `HaveCount…Than…`.

<!-- kingmadoc:diagram img/fa-have-count-modellen-3.d2 -->
![Modellen: fa.have-count](img/fa-have-count-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | geen proof, bewaakt in code |
| Vorm x.Should().HaveCount(n)? | HaveCount(...) is not in the form x.Should().HaveCount(...) | geen proof, bewaakt in code |
| Resultaat weggegooid? | The result of HaveCount(...) is used | `chain` |
| Geen benoemde argumenten; because constant formatteerbaar? | named arguments, or becauseArgs whose because is no constant format string | geen proof (becauseArgs met constante because is Fixed: `because-args`, `count-comparison-because-args`) |
| Subject zonder ?. (conditional access)? | The subject uses ?. | geen proof, bewaakt in code |
| Eerste parameter is int (geen predicate)? | Predicate: rewrite by hand | `predicate` |
| Type van het subject bekend? | The type of the subject is unknown | geen proof, bewaakt in code |
| Geen IDictionary / IReadOnlyDictionary? | Dictionary: check whether Count or Keys is meant | `dictionary` |
| Selector: niet-generieke IEnumerable | Non-generic IEnumerable: use Cast<T>().Count() | geen proof, bewaakt in code |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| array, of alleen publieke int Length | `x.Length.<Should>(n)` | `array` |
| publieke int Count | `x.Count.<Should>(n[, because])`; haakjes en `ShouldNotBeNull()` via `NotNullReceiver` | `list`, `list-because`, `collection-interfaces`, `receiver-parens`, `nullable`, `count-comparisons` |
| alleen IEnumerable<T> | `x.Count().<Should>(n)` + `using System.Linq` | `enumerable`, `enumerable-because`, `enumerable-existing-using` |

## Zie ook

- [fa-have-count-plan.md](fa-have-count-plan.md) - plan
- [fa-have-count-verify.md](fa-have-count-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
