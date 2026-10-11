# Modellen: fa.be-of-type

`x.Should().BeOfType<T>()` en familie naar `x.ShouldBeOfType<T>()`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs`.

**Figure 1.** Klassen rond `BeOfTypePattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-of-type-modellen-1.d2 -->
![Modellen: fa.be-of-type](img/fa-be-of-type-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `BeOfTypePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs` | Dit patroon, Id `fa.be-of-type`; `s_methods` koppelt de vier FA-namen aan Shouldly |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** `Find` geeft de invocation, of de `.Which`/`.Subject`-member access als die in een initializer staat.

<!-- kingmadoc:diagram img/fa-be-of-type-modellen-2.d2 -->
![Modellen: fa.be-of-type](img/fa-be-of-type-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeOfTypePattern.Find` | `FindInvocations` op de vier namen |
| Controles | `BeOfTypePattern.Rewrite` | scope, vorm, gebruik van het resultaat, Types-namespace, argumenten |
| Fixed | `PatternResult.Fixed` | `subject.ShouldBeOfType<T>([because])` (ook voor `.Which`: Shouldly geeft de cast-waarde terug) |

**Figure 3.** Beslisboom van `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/`.

<!-- kingmadoc:diagram img/fa-be-of-type-modellen-3.d2 -->
![Modellen: fa.be-of-type](img/fa-be-of-type-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm `x.Should().M(..)`? | is not in the form x.Should() | geen proof; `.And.BeOfType` gaat via `fa.and-chain` |
| Resultaat weggegooid of `var v = ….Which`? | is used beyond `var x = ….Which` | `which-chain`, `and`, `which-multiple` |
| Niet uit `FluentAssertions.Types`? | on a Type asserts the type itself | `type-subject` |
| Geen becauseArgs? | becauseArgs | `because-args` |
| Juiste argumenten? | unexpected arguments | geen proof |

## Zie ook

- [fa-be-of-type-plan.md](fa-be-of-type-plan.md) - plan
- [fa-be-of-type-verify.md](fa-be-of-type-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeOfTypePattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeOfType/` - de proofs
- [../README.md](../README.md)
