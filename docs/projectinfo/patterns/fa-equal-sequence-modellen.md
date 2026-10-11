# Modellen: fa.equal-sequence

`x.Should().Equal(y)` wordt `x.ShouldBe(y)`; `Equal(1, 2)` wordt `x.ShouldBe(new T[] { 1, 2 })`; met `because` `x.ShouldBe(y, ignoreOrder: false, because)`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs`.

**Figure 1.** Klassen rond `EqualSequencePattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-equal-sequence-modellen-1.d2 -->
![Modellen: fa.equal-sequence](img/fa-equal-sequence-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `EqualSequencePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs` | Dit patroon, Id `fa.equal-sequence` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde controles |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-equal-sequence-modellen-2.d2 -->
![Modellen: fa.equal-sequence](img/fa-equal-sequence-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `EqualSequencePattern.Find` | `FindInvocations` op de FA-namen |
| Controles | `EqualSequencePattern.Rewrite` | scope, vorm, gebruik van het resultaat, type, argumenten |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom van `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/`.

<!-- kingmadoc:diagram img/fa-equal-sequence-modellen-3.d2 -->
![Modellen: fa.equal-sequence](img/fa-equal-sequence-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm x.Should().Equal(..)? | is not in the form x.Should() | `geen proof; .And via fa.and-chain` |
| Resultaat weggegooid? | is used | `and` |
| Op Generic/StringCollectionAssertions? | dictionary compares without order | `dictionary` |
| Equal (niet NotEqual)? | Shouldly has no ShouldNotBe for sequences | `not-equal` |
| Geen predicate? | with a predicate | `predicate` |
| Geen becauseArgs? | becauseArgs | `because-args` |
| Fixed | `x.ShouldBe(new T[] { a, b }) (losse params)` | params |
| Fixed | `x.ShouldBe(y[, ignoreOrder: false, because])` | simple, params-array, because, nullable |

## Zie ook

- [fa-equal-sequence-plan.md](fa-equal-sequence-plan.md) - plan
- [fa-equal-sequence-verify.md](fa-equal-sequence-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/EqualSequence/` - de proofs
- [../README.md](../README.md)
