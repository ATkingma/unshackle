# Modellen: fa.assertion-scope

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs` en `FluentAssertionsSyntax.IsInsideAssertionScope`.

## Figuur 1: klassen

**Figure 1.** `AssertionScopePattern`, zijn contract en de gedeelde check die alle FA-patronen gebruiken. Sinds TAAK-100 draait het patroon als eerste en zet een gewone scope om naar `ShouldSatisfyAllConditions`.

<!-- kingmadoc:diagram img/fa-assertion-scope-modellen-1.d2 -->
![Figuur 1: klassen](img/fa-assertion-scope-modellen-1.png)

| Element | Rol |
| ------- | --- |
| `AssertionScopePattern` | Vindt elke `new AssertionScope(...)` (`Find`: het `using`-statement, het blok van een `using var`, anders de creatie); `Rewrite` geeft `Fixed` via `IsPlain` + `TryConvert`, anders `Manual` |
| `FluentAssertionsSyntax` | `IsAssertionScope(type)`, `IsInsideAssertionScope(node, model)`, `InsideAssertionScopeReason`; `IsFluentAssertions` en `IsSafeToEvaluateTwice` voor `TryConvert` |
| Andere FA-patronen | Roepen `IsInsideAssertionScope` als eerste stap aan (`FluentAssertionPattern.Rewrite`, `AndChainPattern.Rewrite`, ...); herschrijven daarna de lambda's van een omgezette scope |
| `PatternResult` | `Fixed(x.ShouldSatisfyAllConditions(...))` of `Manual(reden)` |
| `AssertionScope` | `FluentAssertions.Execution.AssertionScope` (sealed) |

## Figuur 2: sequence

**Figure 2.** Een omgezette scope (`Fixed`), en een scope die blijft staan met een assertion erbinnen (beide `Manual`).

<!-- kingmadoc:diagram img/fa-assertion-scope-modellen-2.d2 -->
![Figuur 2: sequence](img/fa-assertion-scope-modellen-2.png)

| Stap | Methode in de code | Wat gebeurt er |
| ---- | ------------------ | -------------- |
| Vinden | `AssertionScopePattern.Find` | Alle `BaseObjectCreationExpressionSyntax` met type `AssertionScope` |
| Scope | `AssertionScopePattern.Rewrite` → `IsPlain`, `TryConvert` | Zonder argumenten en alleen FA-assertion-statements (geen `await`, ref-locals, scope-variabele): `x.ShouldSatisfyAllConditions(() => { a; }, ...)` met als `x` het eerste veilige subject; anders `Manual` met advies |
| Assertion | `FluentAssertionPattern.Rewrite` (of `AndChainPattern.Rewrite`) → `IsInsideAssertionScope` | Voorouders aflopen: `using`-body of blok met eerdere `using var` |
| Type | `SemanticModel.GetTypeInfo` | Declaratie- of expressietype moet `AssertionScope` zijn |

## Figuur 3: beslisboom

**Figure 3.** Vragen van `AssertionScopePattern.Rewrite`, en daarna van `IsInsideAssertionScope` voor assertions in een scope die blijft staan.

<!-- kingmadoc:diagram img/fa-assertion-scope-modellen-3.d2 -->
![Figuur 3: beslisboom](img/fa-assertion-scope-modellen-3.png)

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| Manual "Inside an AssertionScope" | body van een niet-omgezette `using (new AssertionScope())`, ook genest (proofs draaien alleen het ShouldBe-patroon) | `ShouldBe/assertion-scope-using-block`, `-nested` |
| Manual "Inside an AssertionScope" | eerder in een omringend blok `using var` scope | `ShouldBe/assertion-scope-using-var` |
| Normaal patroon | geen scope-voorouder | unittest `false`-gevallen |
| Fixed `ShouldSatisfyAllConditions` | gewone scope met alleen FA-assertion-statements | `_Combined/scope-using-block`, `_Combined/scope-using-var` |
| Manual "Shouldly has no scope" | context-string, `Discard()`/scope-variabele, ander statement, `await` | `AssertionScope/context`, `discard`, `other-statement`, `await` |

## Zie ook

- [fa-assertion-scope-plan.md](fa-assertion-scope-plan.md)
- [fa-assertion-scope-verify.md](fa-assertion-scope-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AssertionScope/`
