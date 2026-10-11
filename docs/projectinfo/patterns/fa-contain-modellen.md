# Modellen: fa.contain

x.Should().Contain(item) naar x.ShouldContain(item). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs`.

**Figure 1.** Klassen rond `ContainPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-contain-modellen-1.d2 -->
![Modellen: fa.contain](img/fa-contain-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet de gedeelde controles en roept `RewriteBeforeArguments` en `RewriteAssertion` aan |
| `ContainPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs` | Dit patroon, Id `fa.contain`; privé: `IsGenericEnumerable(type)` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `Contain(T item | Expression predicate | string)`; `NotContain(...)` |
| «Shouldly» aangemaakt | - | `ShouldContain(item | predicate)`; `ShouldNotContain(...)`; `ShouldContain(text, Case.Sensitive)` |
| Herkende typen | - | `string`; `IEnumerable<T>`; `IDictionary / IReadOnlyDictionary (Manual)` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `FluentAssertionPattern.Rewrite` doet de gedeelde controles en roept `ContainPattern.RewriteBeforeArguments` (dictionary) en `ContainPattern.RewriteAssertion` aan, die `Fixed` of `Manual` leveren. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-contain-modellen-2.d2 -->
![Modellen: fa.contain](img/fa-contain-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `ContainPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `Contain`, `NotContain` waarvan het symbool in een FluentAssertions-namespace ligt |
| Controles | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope(node, model)`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; daarna `TryGetArguments(assertion, model)` |
| Dictionary | `ContainPattern.RewriteBeforeArguments` | `GetTypeInfo(subject)`: dictionary geeft Manual vóór de argumentsplitsing |
| Herschrijven | `ContainPattern.RewriteAssertion` | `GetSymbolInfo(assertion)`; `Parameters[0].Type`; `GetTypeInfo(subject)`; `NotNull(subject)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` en de hooks: 8 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-contain-modellen-3.d2 -->
![Modellen: fa.contain](img/fa-contain-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope (scope die `fa.assertion-scope` niet omzet) | - |
| Vorm x.Should().Contain(...)? | Contain(...) is not in the form x.Should()... | `and-chain` |
| Resultaat weggegooid? | The result of Contain(...) is used | `chain`, `value-used` |
| Subject is geen dictionary? | Dictionary: Contain(...) takes key/value pairs; use ShouldContainKey/ShouldContainKeyAndValue by hand | `dictionary` |
| Geen benoemde argumenten; becauseArgs alleen met constante `because`? | named arguments, or becauseArgs whose because is no constant format string | - (`because-args` is Fixed, TAAK-097) |
| Eén waarde-argument (geen occurrence-constraint)? | an occurrence constraint: no Shouldly equivalent | `occurrence` |
| Parameter is item of predicaat (niet meerdere items)? | checks several items at once | `items` |
| Subject is string of IEnumerable<T>? | The subject is not a string or a generic IEnumerable<T> | geen proof, bewaakt in code |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| string | `x.ShouldContain(v, Shouldly.Case.Sensitive[, because])` | `string`, `edge-interpolated`, `nullable-string` (`x.ShouldNotBeNull().ShouldContain(...)`) |
| IEnumerable<T>, item | `x.ShouldContain(item[, because])` | `list`, `enumerable`, `lambda`, `because`, `because-args` (`string.Format(...)`), `edge-generic-list` |
| IEnumerable<T>, predicaat | `x.ShouldContain(predicate)` | `predicate` |
| NotContain | `x.ShouldNotContain(...)` | `not-contain` |

## Zie ook

- [fa-contain-plan.md](fa-contain-plan.md) - plan
- [fa-contain-verify.md](fa-contain-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
