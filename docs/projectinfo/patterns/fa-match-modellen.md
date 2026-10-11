# Modellen: fa.match

`s.Should().MatchRegex(r)` wordt `s.ShouldMatch(r)`, `NotMatchRegex` → `ShouldNotMatch`; wildcard-`Match`/`NotMatch` met een constante wordt de regex `(?s)\A…$`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/MatchPattern.cs`.
De figuren zijn tabellen: `d2` ontbrak bij het schrijven, dus er is niets gerenderd.

**Figure 1.** Klassen.

| Element | Pad | Rol |
| --- | --- | --- |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Basis: scope, vorm, resultaat, because(-args) |
| `MatchPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/MatchPattern.cs` | Dit patroon, Id `fa.match` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `NotNull`, `ConvertExpected`, `Invoke` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde: `Fixer` → `MatchPattern.Find` (`FindInvocations`) → `FluentAssertionPattern.Rewrite` (scope, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`) → `RewriteAssertion` → `Fixed`/`Manual`.

**Figure 3.** Beslisboom (bladeren).

| Uitkomst | Vorm / reden | Proof |
| --- | --- | --- |
| Fixed | `s.ShouldMatch(r[, because])` / `ShouldNotMatch` | regex, not-regex, because, wildcard, not-wildcard, nullable |
| Manual | geen niet-lege stringconstante | regex-object, variable, empty |
| Manual | subject geen string | predicate |

## Zie ook

- [fa-match-plan.md](fa-match-plan.md) - plan
- [fa-match-verify.md](fa-match-verify.md) - verificatie
- `src/Unshackle.Tests/Proofs/FluentAssertions/Match/` - de proofs
