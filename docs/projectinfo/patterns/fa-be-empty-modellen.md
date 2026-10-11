# Modellen: fa.be-empty

x.Should().BeEmpty() naar x.ShouldBeEmpty(). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs`.

**Figure 1.** Klassen rond `BeEmptyPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-empty-modellen-1.d2 -->
![Modellen: fa.be-empty](img/fa-be-empty-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `BeEmptyPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs` | Dit patroon, Id `fa.be-empty` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `BeEmpty([because])`; `NotBeEmpty([because])` |
| «Shouldly» aangemaakt | - | `ShouldBeEmpty([because])`; `ShouldNotBeEmpty([because])` |
| Herkende typen | - | `string`; `IEnumerable<T> (ook dictionaries)` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `Rewrite` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-be-empty-modellen-2.d2 -->
![Modellen: fa.be-empty](img/fa-be-empty-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeEmptyPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `BeEmpty`, `NotBeEmpty` waarvan het symbool in een FluentAssertions-namespace ligt |
| Controles | `BeEmptyPattern.Rewrite` | `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `Rewrite`: 3 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-be-empty-modellen-3.d2 -->
![Modellen: fa.be-empty](img/fa-be-empty-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Vorm x.Should().BeEmpty()? | BeEmpty()/NotBeEmpty() is not in the form x.Should()... | `and-chain` |
| Resultaat weggegooid? | The result of BeEmpty()/NotBeEmpty() is used | `chain`, `value-used` |
| Geen becauseArgs of benoemde argumenten? | becauseArgs of benoemde argumenten | `because-args` |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| BeEmpty | `x.ShouldBeEmpty([because]) (string, lijst, enumerable, dictionary)` | `be-empty-list`, `string`, `enumerable`, `dictionary`, `because`, `lambda` |
| NotBeEmpty | `x.ShouldNotBeEmpty([because])` | `not-be-empty-list` |

## Zie ook

- [fa-be-empty-plan.md](fa-be-empty-plan.md) - plan
- [fa-be-empty-verify.md](fa-be-empty-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEmpty/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
