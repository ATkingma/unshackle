# Modellen: fa.match

`s.Should().MatchRegex(r)` wordt `s.ShouldMatch(r)`, `NotMatchRegex` → `ShouldNotMatch`; wildcard-`Match`/`NotMatch` met een constante wordt de regex `(?s)\A…$`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/MatchPattern.cs`.

**Figure 1.** Klassen: wat het patroon erft, gebruikt en aanmaakt. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-match-modellen-1.d2 -->
![Modellen: fa.match](img/fa-match-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` en `Rewrite` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` doet scope, vorm, resultaat en because(-args), dan `RewriteAssertion` |
| `MatchPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/MatchPattern.cs` | Dit patroon, Id `fa.match`; `RewriteAssertion`; `s_methods` (FA-naam → Shouldly-naam) |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `NotNull`, `Invoke` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |
| «FluentAssertions» herkend | - | `MatchRegex`/`NotMatchRegex(string)`, `Match`/`NotMatch(wildcard)` op een string; `Regex`-overloads, occurrence en predicate → Manual |
| «Shouldly» aangemaakt | - | `s[.ShouldNotBeNull()].ShouldMatch(r[, because])` / `ShouldNotMatch`; wildcard als `@"(?s)\A…$"` |

**Figure 2.** `Fixer` roept `Find` en per node `FluentAssertionPattern.Rewrite` aan; die doet de gedeelde controles en roept de eigen stappen van het patroon aan. Weggelaten: het vervangen van de node door de `Fixer`.

<!-- kingmadoc:diagram img/fa-match-modellen-2.d2 -->
![Modellen: fa.match](img/fa-match-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `MatchPattern.Find` | `FindInvocations` op `Match`, `NotMatch`, `MatchRegex`, `NotMatchRegex` |
| Gedeelde controles | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` (becauseArgs met constante because → `string.Format(CultureInfo.InvariantCulture, ...)`) |
| Eigen controles | `MatchPattern.RewriteAssertion` | subject is string; één niet-lege stringconstante; wildcard → regex; `NotNull` |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom in de volgorde van de code; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/Match/`.

<!-- kingmadoc:diagram img/fa-match-modellen-3.d2 -->
![Modellen: fa.match](img/fa-match-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | geen proof |
| Vorm s.Should().Match..(..)? | is not in the form x.Should() | geen proof |
| Resultaat weggegooid? | The result of ... is used | geen proof |
| Geen benoemde argumenten; because constant formatteerbaar? | named arguments, or becauseArgs whose because is no constant format string | geen proof |
| Subject is een string? | on a non-string subject | predicate |
| Eén niet-lege stringconstante? | without one non-empty string constant | regex-object, variable, empty |

| Voorwaarde | Fixed-vorm | Proofs |
| --- | --- | --- |
| `MatchRegex` / `NotMatchRegex` | `s.ShouldMatch(r[, because])` / `ShouldNotMatch`, patroon ongewijzigd | `regex`, `not-regex`, `because`, `nullable` |
| `Match` / `NotMatch` (wildcard) | `s.ShouldMatch(@"(?s)\A…$")` / `ShouldNotMatch` | `wildcard`, `not-wildcard` |

## Zie ook

- [fa-match-plan.md](fa-match-plan.md) - plan
- [fa-match-verify.md](fa-match-verify.md) - verificatie
- `src/Unshackle.Tests/Proofs/FluentAssertions/Match/` - de proofs
