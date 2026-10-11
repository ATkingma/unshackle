# Modellen: fa.direct-mapping

`BeOneOf` → `ShouldBeOneOf`, `BeSubsetOf` → `ShouldBeSubsetOf`, `OnlyHaveUniqueItems` → `ShouldBeUnique`, `HaveValue`/`NotHaveValue` → `ShouldNotBeNull`/`ShouldBeNull`; `ContainInOrder` → Manual met code. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DirectMappingPattern.cs`.
De figuren zijn tabellen: `d2` ontbrak bij het schrijven, dus er is niets gerenderd.

**Figure 1.** Klassen.

| Element | Pad | Rol |
| --- | --- | --- |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Basis: scope, vorm, resultaat, because(-args) |
| `DirectMappingPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DirectMappingPattern.cs` | Dit patroon, Id `fa.direct-mapping` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `NotNull`, `ConvertExpected`, `Invoke` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde: `Fixer` → `DirectMappingPattern.Find` (`FindInvocations`) → `FluentAssertionPattern.Rewrite` (scope, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`) → `RewriteAssertion` → `Fixed`/`Manual`.

**Figure 3.** Beslisboom (bladeren).

| Uitkomst | Vorm / reden | Proof |
| --- | --- | --- |
| Fixed | `x.ShouldBeOneOf(a, b)` | be-one-of, be-one-of-convert |
| Fixed | `ShouldBeSubsetOf` / `ShouldBeUnique` / `ShouldNotBeNull` / `ShouldBeNull` | be-subset-of, unique, have-value, not-have-value |
| Manual | collectie/comparer/key selector | be-one-of-enumerable, unique-key |
| Manual | ContainInOrder: geen equivalent, code in reden | contain-in-order |

## Zie ook

- [fa-direct-mapping-plan.md](fa-direct-mapping-plan.md) - plan
- [fa-direct-mapping-verify.md](fa-direct-mapping-verify.md) - verificatie
- `src/Unshackle.Tests/Proofs/FluentAssertions/DirectMapping/` - de proofs
