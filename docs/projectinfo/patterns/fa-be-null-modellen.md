# Modellen: fa.be-null

x.Should().BeNull() naar x.ShouldBeNull(). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs`.

**Figure 1.** Klassen rond `BeNullPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-null-modellen-1.d2 -->
![Modellen: fa.be-null](img/fa-be-null-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet de gedeelde controles en roept `RewriteAssertion(AssertionContext)` aan |
| `BeNullPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs` | Dit patroon, Id `fa.be-null`; alleen `Find` en `RewriteAssertion` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `Invoke` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `BeNull([because])`; `NotBeNull([because])` |
| «Shouldly» aangemaakt | - | `ShouldBeNull([because])`; `ShouldNotBeNull([because])` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `FluentAssertionPattern.Rewrite` doet de controles via `FluentAssertionsSyntax` en roept daarna `BeNullPattern.RewriteAssertion` aan, dat `Fixed` levert. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-be-null-modellen-2.d2 -->
![Modellen: fa.be-null](img/fa-be-null-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeNullPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `BeNull`, `NotBeNull` waarvan het symbool in een FluentAssertions-namespace ligt |
| Controles | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope(node, model)`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)` |
| Herschrijven | `BeNullPattern.RewriteAssertion` | `Invoke(subject, ShouldBeNull/ShouldNotBeNull, because)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` en `RewriteAssertion`: 4 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-be-null-modellen-3.d2 -->
![Modellen: fa.be-null](img/fa-be-null-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope (scope die `fa.assertion-scope` niet omzet) | - |
| Vorm x.Should().BeNull()? | BeNull()/NotBeNull() is not in the form x.Should()... | `and-chain` |
| Resultaat weggegooid? | The result of BeNull()/NotBeNull() is used | `chain`, `value-used`, `edge-not-null-and` |
| Geen benoemde argumenten; becauseArgs alleen met constante `because`? | named arguments, or becauseArgs whose because is no constant format string | - (`because-args` is Fixed, TAAK-097) |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| BeNull | `x.ShouldBeNull([because])` | `be-null`, `lambda`, `collection`, `nullable-value`, `because`, `edge-expression-bodied`, `edge-null-conditional` |
| NotBeNull | `x.ShouldNotBeNull([because])` | `not-be-null`, `edge-null-forgiving` |
| becauseArgs met constante `because` | `because` wordt `string.Format(CultureInfo.InvariantCulture, because, args)` | `because-args` |

## Zie ook

- [fa-be-null-plan.md](fa-be-null-plan.md) - plan
- [fa-be-null-verify.md](fa-be-null-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
