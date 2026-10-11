# Modellen: fa.be-of-type

`x.Should().BeOfType<T>()` en familie naar `x.ShouldBeOfType<T>()`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs`.

**Figure 1.** Klassen rond `BeOfTypePattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-of-type-modellen-1.d2 -->
![Modellen: fa.be-of-type](img/fa-be-of-type-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet scope, vorm, resultaat en argumenten en roept `RewriteAssertion` aan |
| `BeOfTypePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs` | Dit patroon, Id `fa.be-of-type`; `s_methods` koppelt de vier FA-namen aan Shouldly |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `WithWhich`, `AssertionOfWhich`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscardedOrWhich`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** `Find` geeft de invocation, of de `.Which`/`.Subject`-member access als die in een initializer staat.

<!-- kingmadoc:diagram img/fa-be-of-type-modellen-2.d2 -->
![Modellen: fa.be-of-type](img/fa-be-of-type-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeOfTypePattern.Find` | `FindInvocations` op de vier namen, via `WithWhich` de `.Which`/`.Subject`-node als die erop volgt |
| Controles | `FluentAssertionPattern.Rewrite` | scope, vorm, gebruik van het resultaat (`IsResultDiscardedOrWhich`), becauseArgs |
| Herschrijven | `BeOfTypePattern.RewriteAssertion` | Types-namespace, aantal argumenten, dan `Fixed` |
| Fixed | `PatternResult.Fixed` | `subject.ShouldBeOfType<T>([because])` (ook voor `.Which`: Shouldly geeft de cast-waarde terug) |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` en `BeOfTypePattern.RewriteAssertion`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/`.

<!-- kingmadoc:diagram img/fa-be-of-type-modellen-3.d2 -->
![Modellen: fa.be-of-type](img/fa-be-of-type-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm `x.Should().M(..)`? | is not in the form x.Should() | geen proof; `.And.BeOfType` gaat via `fa.and-chain` |
| Resultaat weggegooid of via `.Which`/`.Subject`? | is used other than through .Which/.Subject | `and` (`which-chain`, `which-multiple` zijn Fixed, TAAK-096) |
| Geen benoemde argumenten; becauseArgs alleen met constante `because`? | named arguments, or becauseArgs whose because is no constant format string | geen proof (`because-args` is Fixed, TAAK-097) |
| Niet uit `FluentAssertions.Types`? | on a Type asserts the type itself | `type-subject` |
| Juiste argumenten? | unexpected arguments | geen proof |

Fixed: `simple`, `because`, `because-args` (`string.Format(...)`), `type-argument`, `which`, `which-chain` (`obj.ShouldBeOfType<Dto>().Name`), `which-multiple`, `nullable`.

## Zie ook

- [fa-be-of-type-plan.md](fa-be-of-type-plan.md) - plan
- [fa-be-of-type-verify.md](fa-be-of-type-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/` - de proofs
- [../README.md](../README.md)
