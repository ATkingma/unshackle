# Modellen: fa.be-approximately

`x.Should().BeApproximately(y, tol)` en `t.Should().BeCloseTo(u, span)` worden `x.ShouldBe(y, tol)`; `NotBeCloseTo` wordt `ShouldNotBe(u, span)`; `NotBeApproximately` wordt `System.Math.Abs(x - y).ShouldBeGreaterThan(tol)`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs`.

**Figure 1.** Klassen rond `BeApproximatelyPattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-approximately-modellen-1.d2 -->
![Modellen: fa.be-approximately](img/fa-be-approximately-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `BeApproximatelyPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs` | Dit patroon, Id `fa.be-approximately` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde controles en helpers (`TryGetArguments`, `IsSafeReceiver`, `Invoke`) |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet de gedeelde controles en roept `RewriteAssertion` aan |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde van `Find` en `FluentAssertionPattern.Rewrite` → `BeApproximatelyPattern.RewriteAssertion`.

<!-- kingmadoc:diagram img/fa-be-approximately-modellen-2.d2 -->
![Modellen: fa.be-approximately](img/fa-be-approximately-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `BeApproximatelyPattern.Find` | `FindInvocations` op de FA-namen |
| Controles | `FluentAssertionPattern.Rewrite` | scope, vorm, gebruik van het resultaat, argumenten (becauseArgs → `string.Format`) |
| Herschrijven | `BeApproximatelyPattern.RewriteAssertion` | type subject en expected, `Not…` op nullable |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom van `FluentAssertionPattern.Rewrite` → `RewriteAssertion`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeApproximately/`.

<!-- kingmadoc:diagram img/fa-be-approximately-modellen-3.d2 -->
![Modellen: fa.be-approximately](img/fa-be-approximately-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm x.Should().M(..)? | is not in the form x.Should() | `geen proof; .And via fa.and-chain` |
| Resultaat weggegooid? | is used | `geen proof` |
| Argumenten splitsbaar (geen named args; becauseArgs met constante because)? | named arguments, or becauseArgs whose because is no constant format string | geen proof (`because-args` is sinds TAAK-097 Fixed) |
| Subject double/float/decimal/DateTime(Offset)/TimeSpan? | tolerance only for ... | `int-close-to` |
| Expected heeft hetzelfde type? | nullable or differently typed expected | `geen proof` |
| Niet Not… op nullable? | nullable subject | `nullable-not` |
| Fixed | `System.Math.Abs(x - y).ShouldBeGreaterThan(tol) (NotBeApproximately)` | not |
| Fixed | `x[.ShouldNotBeNull()].ShouldBe / ShouldNotBe(y, tol|span[, because])`; becauseArgs → `string.Format(CultureInfo.InvariantCulture, because, args)` | simple, because, because-args, close-to, nullable |

## Zie ook

- [fa-be-approximately-plan.md](fa-be-approximately-plan.md) - plan
- [fa-be-approximately-verify.md](fa-be-approximately-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeApproximately/` - de proofs
- [../README.md](../README.md)
