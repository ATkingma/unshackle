# Modellen: fa.be-same-as

`x.Should().BeSameAs(y)` wordt `x.ShouldBeSameAs(y)`; `NotBeSameAs` wordt `ShouldNotBeSameAs`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeSameAsPattern.cs`.

**Figure 1.** Klassen rond `BeSameAsPattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-same-as-modellen-1.d2 -->
![Modellen: fa.be-same-as](img/fa-be-same-as-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet de gedeelde controles en roept `RewriteAssertion` aan |
| `BeSameAsPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeSameAsPattern.cs` | Dit patroon, Id `fa.be-same-as` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde controles |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-be-same-as-modellen-2.d2 -->
![Modellen: fa.be-same-as](img/fa-be-same-as-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeSameAsPattern.Find` | `FindInvocations` op de FA-namen |
| Controles | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| Herschrijven | `BeSameAsPattern.RewriteAssertion` | aantal waarden, `Invoke(subject, "Should" + naam, y, because)` |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` en `BeSameAsPattern.RewriteAssertion`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeSameAs/`.

<!-- kingmadoc:diagram img/fa-be-same-as-modellen-3.d2 -->
![Modellen: fa.be-same-as](img/fa-be-same-as-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm x.Should().M(..)? | is not in the form x.Should() | `geen proof; .And via fa.and-chain` |
| Resultaat weggegooid? | is used | `result-used` |
| Geen benoemde argumenten; becauseArgs alleen met constante `because`? | named arguments, or becauseArgs whose because is no constant format string | geen proof (`because-args` is Fixed, TAAK-097) |
| Precies één waarde-argument? | unexpected argument list | `geen proof` |
| Fixed | `x.ShouldBeSameAs / ShouldNotBeSameAs(y[, because])` | simple, not, because, because-args, nullable |

## Zie ook

- [fa-be-same-as-plan.md](fa-be-same-as-plan.md) - plan
- [fa-be-same-as-verify.md](fa-be-same-as-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeSameAsPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeSameAs/` - de proofs
- [../README.md](../README.md)
