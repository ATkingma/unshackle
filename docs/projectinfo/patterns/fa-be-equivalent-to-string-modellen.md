# Modellen: fa.be-equivalent-to-string

s.Should().BeEquivalentTo("x") naar s.ShouldBe("x", StringCompareShould.IgnoreCase). Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs`.

**Figure 1.** Klassen rond `BeEquivalentToStringPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-equivalent-to-string-modellen-1.d2 -->
![Modellen: fa.be-equivalent-to-string](img/fa-be-equivalent-to-string-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `BeEquivalentToStringPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs` | Dit patroon, Id `fa.be-equivalent-to-string` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `Invoke` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet de gedeelde controles, `BeEquivalentToStringPattern.RewriteAssertion` de rest |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node(s) en namespaces, of `Manual` met reden |
| «FluentAssertions» herkend | - | `StringAssertions.BeEquivalentTo(expected[, because])`; `StringAssertions.NotBeEquivalentTo(...)` |
| «Shouldly» aangemaakt | - | `ShouldBe(expected[, because], StringCompareShould.IgnoreCase)` |
| Herkende typen | - | `StringAssertions (alleen string-overloads)` |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `FluentAssertionPattern.Rewrite` → `RewriteAssertion` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-be-equivalent-to-string-modellen-2.d2 -->
![Modellen: fa.be-equivalent-to-string](img/fa-be-equivalent-to-string-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeEquivalentToStringPattern.Find` | `FluentAssertionsSyntax.FindInvocations` zoekt aanroepen `BeEquivalentTo`, `NotBeEquivalentTo` waarvan het symbool in een FluentAssertions-namespace ligt ; alleen `StringAssertions` blijft over |
| Controles | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)` (becauseArgs → `string.Format`) |
| Herschrijven | `BeEquivalentToStringPattern.RewriteAssertion` | `NotBeEquivalentTo` → Manual; precies één waarde; `ShouldBe(.., IgnoreCase)` |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` → `RewriteAssertion`: 4 vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/` die het bewijst (`<naam>.before.cs` met `.after.cs` of `.manual.txt`).

<!-- kingmadoc:diagram img/fa-be-equivalent-to-string-modellen-3.d2 -->
![Modellen: fa.be-equivalent-to-string](img/fa-be-equivalent-to-string-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Vorm x.Should().BeEquivalentTo(...)? | BeEquivalentTo(...) is not in the form x.Should()... | `and-chain` |
| Resultaat weggegooid? | The result of BeEquivalentTo(...) is used | `chain`, `value-used` |
| Argumenten splitsbaar (becauseArgs alleen met constante because)? | named arguments, or becauseArgs whose because is no constant format string | geen proof (`because-args` is sinds TAAK-097 Fixed) |
| BeEquivalentTo, niet NotBeEquivalentTo? | NotBeEquivalentTo has no Shouldly equivalent | `not-equivalent` |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| nee | `s.ShouldBe("x", StringCompareShould.IgnoreCase)` | `simple`, `lambda`, `edge-interpolated` |
| ja | `s.ShouldBe("x", because, StringCompareShould.IgnoreCase)`; becauseArgs → `string.Format(CultureInfo.InvariantCulture, because, args)` | `because`, `because-args` |

## Zie ook

- [fa-be-equivalent-to-string-plan.md](fa-be-equivalent-to-string-plan.md) - plan
- [fa-be-equivalent-to-string-verify.md](fa-be-equivalent-to-string-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
