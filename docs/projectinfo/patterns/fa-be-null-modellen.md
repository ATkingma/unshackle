# Modellen: fa.be-null

x.Should().BeNull() naar x.ShouldBeNull(). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs`.

**Figure 1.** Klassen rond `BeNullPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-null-modellen-1.d2 -->
![Modellen: fa.be-null](img/fa-be-null-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `BeNullPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs` | Dit patroon, Id `fa.be-null` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `BeNull([because])`; `NotBeNull([because])` |
| «Shouldly» aangemaakt | - | `ShouldBeNull([because])`; `ShouldNotBeNull([because])` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `Rewrite` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-be-null-modellen-2.d2 -->
![Modellen: fa.be-null](img/fa-be-null-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeNullPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `BeNull`, `NotBeNull` waarvan het symbool in een FluentAssertions-namespace ligt |
| Controles | `BeNullPattern.Rewrite` | `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `Rewrite`: 3 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-be-null-modellen-3.d2 -->
![Modellen: fa.be-null](img/fa-be-null-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Vorm x.Should().BeNull()? | BeNull()/NotBeNull() is not in the form x.Should()... | `and-chain` |
| Resultaat weggegooid? | The result of BeNull()/NotBeNull() is used | `chain`, `value-used` |
| Geen becauseArgs of benoemde argumenten? | becauseArgs of benoemde argumenten | `because-args` |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| BeNull | `x.ShouldBeNull([because])` | `be-null`, `lambda`, `collection`, `nullable-value`, `because` |
| NotBeNull | `x.ShouldNotBeNull([because])` | `not-be-null` |

## Zie ook

- [fa-be-null-plan.md](fa-be-null-plan.md) - plan
- [fa-be-null-verify.md](fa-be-null-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
