# Modellen: fa.contain-single-which

`x.Should().ContainSingle()` wordt `x.ShouldHaveSingleItem()`, `ContainSingle(pred)` wordt `x.ShouldContain(pred, 1)` en `var y = ….ContainSingle(pred).Which` wordt `var y = x.Where(pred).ShouldHaveSingleItem()`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs`.

**Figure 1.** Klassen rond `ContainSinglePattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-contain-single-which-modellen-1.d2 -->
![Modellen: fa.contain-single-which](img/fa-contain-single-which-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `ContainSinglePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs` | Dit patroon, Id `fa.contain-single-which` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde controles |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-contain-single-which-modellen-2.d2 -->
![Modellen: fa.contain-single-which](img/fa-contain-single-which-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `ContainSinglePattern.Find` | `FindInvocations` op de FA-namen |
| Controles | `ContainSinglePattern.Rewrite` | scope, vorm, gebruik van het resultaat, type, argumenten |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom van `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/`.

<!-- kingmadoc:diagram img/fa-contain-single-which-modellen-3.d2 -->
![Modellen: fa.contain-single-which](img/fa-contain-single-which-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm x.Should().ContainSingle(..)? | is not in the form x.Should() | `geen proof; .And via fa.and-chain` |
| Resultaat weggegooid of var y = ….Which? | is used beyond var x = ….Which | `which-chain` |
| Geen becauseArgs? | becauseArgs | `because-args` |
| Subject is IEnumerable<T>? | non-generic collection | `geen proof` |
| Geen predicate, geen .Which, of lambda-predicate? | Expression variable | `expression-variable` |
| Fixed | `x.ShouldHaveSingleItem() / x.ShouldContain(pred, 1) / x.Where(pred).ShouldHaveSingleItem()` | simple, predicate, enumerable, because, which, nullable |

## Zie ook

- [fa-contain-single-which-plan.md](fa-contain-single-which-plan.md) - plan
- [fa-contain-single-which-verify.md](fa-contain-single-which-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/` - de proofs
- [../README.md](../README.md)
