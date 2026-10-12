# Modellen: fa.not-contain-nulls

`NotContainNulls()` en `AllBe("v")` worden een predicaat-aanroep. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotContainNullsPattern.cs`. Diagrammen met de hand in Mermaid (kingmadoc/d2 ontbreken).

**Figure 1.** Klassen rond `NotContainNullsPattern`.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- NotContainNullsPattern
    NotContainNullsPattern ..> FluentAssertionsSyntax : FindInvocations, Invoke, NotNull, IsSafeToEvaluateTwice
    NotContainNullsPattern ..> PatternResult : Fixed / Manual
    PatternResult <|-- FixedResult
    PatternResult <|-- ManualResult
```

| Element | Pad | Rol |
| --- | --- | --- |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: scope, vorm, resultaat, because(-args) |
| `NotContainNullsPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotContainNullsPattern.cs` | Dit patroon, Id `fa.not-contain-nulls` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Hulpmethodes |
| `PatternResult` e.a. | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde.

```mermaid
sequenceDiagram
    Fixer->>NotContainNullsPattern: Find(root, model)
    NotContainNullsPattern->>FluentAssertionsSyntax: FindInvocations("NotContainNulls", "AllBe")
    Fixer->>NotContainNullsPattern: Rewrite
    NotContainNullsPattern->>NotContainNullsPattern: RewriteAssertion
    NotContainNullsPattern->>NotContainNullsPattern: FreeParameterName
    NotContainNullsPattern->>FluentAssertionsSyntax: Invoke("ShouldNotContain" / "ShouldAllBe")
    NotContainNullsPattern-->>Fixer: Fixed / Manual
```

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `Find` | Alle `NotContainNulls`- en `AllBe`-aanroepen op FA |
| Naam | `FreeParameterName` | Eerste naam uit `i`, `item`, `x`, ... die niet in scope is |
| Herschrijven | `RewriteNotContainNulls` / `RewriteAllBe` | Predicaat als argument, because erachter |

**Figure 3.** Beslisboom.

```mermaid
flowchart TD
    A{In AssertionScope of resultaat gebruikt?} -- ja --> M1[Manual]
    A -- nee --> B{Subject generieke IEnumerable?}
    B -- nee --> M2[Manual: not IEnumerable]
    B -- ja --> C{Welke methode?}
    C -- NotContainNulls --> D{Selector of niet-nullable waardetype?}
    D -- ja --> M3[Manual: selector / value-type]
    D -- nee --> F1["x.ShouldNotContain(i => i == null)"]
    C -- AllBe --> E{String-collectie, constante of opgeslagen waarde?}
    E -- nee --> M4[Manual: all-be-computed]
    E -- ja --> F2["x.ShouldAllBe(i => i == v)"]
```

| Vraag | Uitkomst | Proof |
| --- | --- | --- |
| AssertionScope? | Manual | `assertion-scope`, `all-be-assertion-scope` |
| Resultaat gebruikt? | Manual | `result-used`, `all-be-result-used` |
| Selector? | Manual | `selector` |
| Waardetype? | Manual | `value-type` |
| Berekende waarde? | Manual | `all-be-computed` |
| Gewoon geval | Fixed | `simple`, `all-be-string` |

## Zie ook

- [fa-not-contain-nulls-plan.md](fa-not-contain-nulls-plan.md)
- [fa-not-contain-nulls-verify.md](fa-not-contain-nulls-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotContainNullsPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotContainNulls/`
