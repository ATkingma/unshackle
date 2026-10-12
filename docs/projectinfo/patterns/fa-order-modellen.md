# Modellen: fa.order

`x.Should().BeInAscendingOrder(p => p.K)` wordt `x.Select(p => p.K).ShouldBeInOrder(SortDirection.Ascending)`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs`. Diagrammen met de hand in Mermaid (kingmadoc/d2 ontbreken).

**Figure 1.** Klassen rond `OrderPattern`.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- OrderPattern
    OrderPattern ..> FluentAssertionsSyntax : FindInvocations, NotNullReceiver, Invoke
    OrderPattern ..> PatternResult : Fixed / Manual
    PatternResult <|-- FixedResult
    PatternResult <|-- ManualResult
```

| Element | Pad | Rol |
| --- | --- | --- |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: scope, vorm, resultaat, because(-args) |
| `OrderPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs` | Dit patroon, Id `fa.order` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `NotNullReceiver`, `Invoke` |
| `PatternResult` e.a. | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde.

```mermaid
sequenceDiagram
    Fixer->>OrderPattern: Find(root, model)
    OrderPattern->>FluentAssertionsSyntax: FindInvocations("BeInAscendingOrder", "BeInDescendingOrder")
    Fixer->>OrderPattern: Rewrite
    OrderPattern->>OrderPattern: RewriteAssertion (overload, sleuteltype)
    OrderPattern->>FluentAssertionsSyntax: NotNullReceiver, Invoke("Select"), Invoke("ShouldBeInOrder")
    OrderPattern-->>Fixer: Fixed (+ System.Linq) / Manual
```

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `OrderPattern.Find` | Alle Be-In-Ascending/Descending-Order-aanroepen op FA |
| Overload | `RewriteAssertion` | Geen argument of selector-lambda; anders Manual |
| Herschrijven | `RewriteAssertion` | `Select` (bij selector) en `ShouldBeInOrder` met richting, comparer en because |

**Figure 3.** Beslisboom.

```mermaid
flowchart TD
    A{In AssertionScope?} -- ja --> M1[Manual: assertion-scope]
    A -- nee --> B{Resultaat gebruikt?}
    B -- ja --> M2[Manual: result-used]
    B -- nee --> C{Overload?}
    C -- comparer / comparison --> M3[Manual: comparer, comparison, selector-comparer]
    C -- selector geen lambda --> M4[Manual: expression-variable]
    C -- geen argument --> K{Elementtype string?}
    C -- selector-lambda --> K
    K -- ja --> F1["ShouldBeInOrder(dir, StringComparer.Ordinal): string-key, string-elements, string-because"]
    K -- nee --> F2["ShouldBeInOrder(dir): selector-ascending, selector-descending, no-selector, nullable"]
    F2 --> BE{because?}
    BE -- ja --> F3["ShouldBeInOrder(dir, null, because): because, because-args"]
```

| Vraag | Uitkomst | Proof |
| --- | --- | --- |
| AssertionScope? | Manual | `assertion-scope` |
| Resultaat gebruikt? | Manual | `result-used` |
| Comparer of comparison? | Manual | `comparer`, `comparison`, `selector-comparer` |
| Selector geen lambda? | Manual | `expression-variable` |
| String-sleutel? | `StringComparer.Ordinal` | `string-key`, `string-elements`, `string-because` |
| Overig | `ShouldBeInOrder(dir)` | `selector-ascending`, `selector-descending`, `no-selector`, `nullable`, `because`, `because-args` |

## Zie ook

- [fa-order-plan.md](fa-order-plan.md)
- [fa-order-verify.md](fa-order-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OrderPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/Order/`
