# Modellen: fa.have-length

`s.Should().HaveLength(n)` wordt `s.Length.ShouldBe(n)`; nullable subject `s.ShouldNotBeNull().Length.ShouldBe(n)`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs`.

**Figure 1.** Klassen rond `HaveLengthPattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-have-length-modellen-1.d2 -->
![Modellen: fa.have-length](img/fa-have-length-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet scope, vorm, resultaat en because(-args), dan `RewriteAssertion` |
| `HaveLengthPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs` | Dit patroon, Id `fa.have-length` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde controles |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-have-length-modellen-2.d2 -->
![Modellen: fa.have-length](img/fa-have-length-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `HaveLengthPattern.Find` | `FindInvocations` op de FA-namen |
| Controles | `FluentAssertionPattern.Rewrite` → `HaveLengthPattern.RewriteAssertion` | scope, vorm, gebruik van het resultaat, argumenten (`TryGetArguments`); dan `?.`, type, `NotNullReceiver` |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` + `RewriteAssertion`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/`.

<!-- kingmadoc:diagram img/fa-have-length-modellen-3.d2 -->
![Modellen: fa.have-length](img/fa-have-length-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm s.Should().HaveLength(..)? | is not in the form s.Should() | `geen proof; .And via fa.and-chain` |
| Resultaat weggegooid? | is used | `geen proof` |
| Geen benoemde argumenten; because constant formatteerbaar? | named arguments, or becauseArgs whose because is no constant format string | `geen proof` |
| Geen ?. in het subject? | uses ?. | `geen proof` |
| Op StringAssertions? | on a stream | `stream` |
| Fixed | `s[.ShouldNotBeNull()].Length.ShouldBe(n[, because])` | simple, because, because-args, nullable, receiver-parens |

## Zie ook

- [fa-have-length-plan.md](fa-have-length-plan.md) - plan
- [fa-have-length-verify.md](fa-have-length-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/` - de proofs
- [../README.md](../README.md)
