# Modellen: fa.string-start-end

`s.Should().StartWith(x)` en familie worden `s.ShouldStartWith(x, Shouldly.Case.Sensitive)`; `NotBeNullOrEmpty()` e.d. worden `ShouldNotBeNullOrEmpty()` e.d. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/StringStartEndPattern.cs`.

**Figure 1.** Klassen rond `StringStartEndPattern`. Weggelaten: `Fixer`, `IFix` en de usings (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-string-start-end-modellen-1.d2 -->
![Modellen: fa.string-start-end](img/fa-string-start-end-modellen-1.png)

| Element | Pad | Rol |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract |
| `StringStartEndPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/StringStartEndPattern.cs` | Dit patroon, Id `fa.string-start-end` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Gedeelde controles |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde van `Find` en `Rewrite`.

<!-- kingmadoc:diagram img/fa-string-start-end-modellen-2.d2 -->
![Modellen: fa.string-start-end](img/fa-string-start-end-modellen-2.png)

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `StringStartEndPattern.Find` | `FindInvocations` op de FA-namen |
| Controles | `StringStartEndPattern.Rewrite` | scope, vorm, gebruik van het resultaat, type, argumenten |
| Uitkomst | `PatternResult.Fixed` / `Manual` | Shouldly-aanroep of reden |

**Figure 3.** Beslisboom van `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/StringStartEnd/`.

<!-- kingmadoc:diagram img/fa-string-start-end-modellen-3.d2 -->
![Modellen: fa.string-start-end](img/fa-string-start-end-modellen-3.png)

| Vraag | Bij nee: Manual-reden | Proof |
| --- | --- | --- |
| Buiten een AssertionScope? | Inside an AssertionScope | `assertion-scope` |
| Vorm s.Should().M(..)? | is not in the form s.Should() | `and` |
| Resultaat weggegooid? | is used | `geen eigen proof; .And wordt eerst gesplitst` |
| Op StringAssertions? | non-string subject | `collection` |
| Geen becauseArgs? | becauseArgs | `because-args` |
| NullOr-methode, of één verwachte waarde? | equivalency options | `geen proof` |
| Fixed | `s.ShouldStartWith(x, Shouldly.Case.Sensitive[, because]) e.d.` | simple, equivalent-of, because, null-or, nullable, edge-member-subject |

## Zie ook

- [fa-string-start-end-plan.md](fa-string-start-end-plan.md) - plan
- [fa-string-start-end-verify.md](fa-string-start-end-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/StringStartEndPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/StringStartEnd/` - de proofs
- [../README.md](../README.md)
