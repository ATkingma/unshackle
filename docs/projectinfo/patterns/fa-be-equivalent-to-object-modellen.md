# Modellen: fa.be-equivalent-to-object

obj.Should().BeEquivalentTo(other) naar obj.ShouldBeEquivalentTo(other), alleen voor het zelfde gewone type met eenvoudige leden; een collectie van leaves wordt `ShouldBe(other, ignoreOrder: true)` (TAAK-092); al het andere is Manual. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`.

**Figure 1.** Klassen rond `BeEquivalentToObjectPattern`: wat hij implementeert, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings-afhandeling (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-equivalent-to-object-modellen-1.d2 -->
![Modellen: fa.be-equivalent-to-object](img/fa-be-equivalent-to-object-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `BeEquivalentToObjectPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` | Dit patroon, Id `fa.be-equivalent-to-object`; `Unsupported` loopt over het type en zijn publieke leden |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde bouwstenen: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `Invoke` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet de gedeelde controles, roept `RewriteBeforeArguments` (NotBeEquivalentTo, params) en dan `RewriteAssertion` aan |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst: `Fixed` met nieuwe node (bij een niet-sealed class met `Warning`), of `Manual` met reden |
| «FluentAssertions» herkend | - | `BeEquivalentTo(expected[, because])`, met `options =>`, en `NotBeEquivalentTo` |
| «Shouldly» aangemaakt | - | `ShouldBeEquivalentTo(expected[, because])`; voor een collectie van leaves `ShouldBe(expected, ignoreOrder: true[, because])` |
| Uitgesloten typen | - | `StringAssertions` (doet `fa.be-equivalent-to-string`) |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan; `FluentAssertionPattern.Rewrite` → `RewriteAssertion` doet de controles via `FluentAssertionsSyntax` en `SemanticModel` en levert `Fixed` of `Manual`. Weggelaten: het vervangen van de node en de usings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-be-equivalent-to-object-modellen-2.d2 -->
![Modellen: fa.be-equivalent-to-object](img/fa-be-equivalent-to-object-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeEquivalentToObjectPattern.Find` | `FindInvocations` zoekt `BeEquivalentTo`, `NotBeEquivalentTo` in FluentAssertions; `StringAssertions` valt af |
| Vorm en argumenten | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `RewriteBeforeArguments`, `TryGetArguments` (becauseArgs → `string.Format`) |
| Typen | `RewriteAssertion` | `GetTypeInfo` van subject en verwachting; `ElementType` + `IsLeaf` voor een collectie; `SymbolEqualityComparer` |
| Type-onderzoek | `Unsupported` | Collectie, gewoon type, `Equals`-override, cyclus, publieke leden, leden recursief |
| Manual | `PatternResult.Manual` | Eerste mislukte controle geeft `ManualResult` met reden (zie figuur 3) |
| Fixed | `PatternResult.Fixed` / `FixedResult` met `Warning` | Nieuwe node; trivia en usings regelt de `Fixer` |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` → `RewriteAssertion`: de vragen in de volgorde van de code, elk blad noemt de proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/` die het bewijst.

<!-- kingmadoc:diagram img/fa-be-equivalent-to-object-modellen-3.d2 -->
![Modellen: fa.be-equivalent-to-object](img/fa-be-equivalent-to-object-modellen-3.png)

| Vraag of selector | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Vorm x.Should().BeEquivalentTo(...)? | is not in the form | `and-chain` |
| Resultaat weggegooid? | The result of ... is used | `value-used` |
| Niet NotBeEquivalentTo, geen `params`-overload? (`RewriteBeforeArguments`) | no Shouldly equivalent / (params items) is a collection assertion | `not-equivalent`, `edge-collection-params` |
| Argumenten splitsbaar (becauseArgs alleen met constante because)? | named arguments, or becauseArgs whose because is no constant format string | geen proof (`because-args` is sinds TAAK-097 Fixed) |
| Precies één waarde? | options => ... configuration | `options` |
| Type bekend? | The type of the subject or the expectation is unknown | geen proof (`BeEquivalentTo(null)` is ambigu) |
| Collectie van leaves met hetzelfde elementtype? | ja: Fixed `ShouldBe(.., ignoreOrder: true)`; nee: verder | zie hieronder |
| Zelfde compile-time type? | Different compile-time types | `anonymous`, `different-types`, `base-vs-derived`, `collection-element-types` |
| Gewoon type, zonder collectie, Equals-override, cyclus? | is a collection / not a plain class / overrides Equals / cyclic / no public members | `collection`, `collection-member`, `object-subject`, `object-member`, `equals-override`, `equals-member`, `cyclic`, `no-members` |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| collectie van leaves (TAAK-092) | `a.ShouldBe(b, ignoreOrder: true[, because])` | `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `collection-because-args`, `edge-array-literal` |
| geen because | `a.ShouldBeEquivalentTo(b)` | `simple`, `new-expectation`, `record`, `nested`, `lambda` |
| because / becauseArgs | `a.ShouldBeEquivalentTo(b, because)`; becauseArgs → `string.Format(CultureInfo.InvariantCulture, because, args)` | `because`, `because-args` |
| sealed type of beide `new` | geen `Warning`; anders `Warning` over afgeleide runtime-typen | `sealed`, `both-new` |

## Zie ook

- [fa-be-equivalent-to-object-plan.md](fa-be-equivalent-to-object-plan.md) - plan
- [fa-be-equivalent-to-object-verify.md](fa-be-equivalent-to-object-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/` - de proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generieke beslissing van een pattern
- [../README.md](../README.md)
