# Models: fa.contain-equivalent-of

`ContainEquivalentOf(item)` becomes `ShouldContain(item)` for value items. Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs`. Diagrams written by hand in Mermaid (kingmadoc/d2 unavailable).

**Figure 1.** Classes around `ContainEquivalentOfPattern`.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- BeEquivalentToObjectPattern
    BeEquivalentToObjectPattern <|-- ContainEquivalentOfPattern
    BeEquivalentToObjectPattern <|-- EquivalentOptionsPattern
    ContainEquivalentOfPattern ..> FluentAssertionsSyntax : FindInvocations, Invoke, NotNull
    ContainEquivalentOfPattern ..> PatternResult : Fixed / Manual
    PatternResult <|-- FixedResult
    PatternResult <|-- ManualResult
```

| Element | Path | Role |
| --- | --- | --- |
| `BeEquivalentToObjectPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` | Provides `ElementType`, `IsValueLike`, `IsLeaf` |
| `ContainEquivalentOfPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs` | This pattern, Id `fa.contain-equivalent-of` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Helper methods |
| `PatternResult` etc. | `src/Unshackle.Core/Models/` | Outcome |

**Figure 2.** Order.

```mermaid
sequenceDiagram
    Fixer->>ContainEquivalentOfPattern: Find(root, model)
    ContainEquivalentOfPattern->>FluentAssertionsSyntax: FindInvocations("ContainEquivalentOf", "NotContainEquivalentOf")
    Fixer->>ContainEquivalentOfPattern: Rewrite
    ContainEquivalentOfPattern->>ContainEquivalentOfPattern: RewriteAssertion
    ContainEquivalentOfPattern->>BeEquivalentToObjectPattern: ElementType, IsValueLike
    ContainEquivalentOfPattern->>FluentAssertionsSyntax: NotNull, Invoke("ShouldContain" / "ShouldNotContain")
    ContainEquivalentOfPattern-->>Fixer: Fixed / Manual
```

| Step | Location | What happens |
| --- | --- | --- |
| Find | `Find` | All `ContainEquivalentOf`/`NotContainEquivalentOf` on FA |
| Type check | `RewriteAssertion` | Element and expectation type via `ElementType` and `GetTypeInfo` |
| Value item | `IsValueLike` | Leaf, sealed record, record struct, struct with leaf members |
| Rewrite | `FluentAssertionsSyntax.Invoke` | `ShouldContain(item, because)` with `NotNull` on the subject |

**Figure 3.** Decision tree.

```mermaid
flowchart TD
    A{AssertionScope or result used?} -- yes --> M1[Manual]
    A -- no --> B{More than one value, options?}
    B -- yes --> M2[Manual: options]
    B -- no --> C{Subject a generic IEnumerable?}
    C -- no --> M3[Manual: not IEnumerable]
    C -- yes --> D{Expectation type equal to element type?}
    D -- no --> M4[Manual: different-type]
    D -- yes --> E{Element value-like?}
    E -- no --> M5[Manual: class, dictionary, record with collection]
    E -- yes --> F["x.ShouldContain(item) / ShouldNotContain(item)"]
```

| Question | Outcome | Proof |
| --- | --- | --- |
| AssertionScope? | Manual | `assertion-scope` |
| Result used? | Manual | `which` |
| Options? | Manual | `options` |
| Different expectation type? | Manual | `different-type` |
| Not a value item? | Manual | `class`, `record-unsealed`, `record-list-member`, `dictionary` |
| Plain case | Fixed | `simple`, `strings`, `sealed-record`, `record-struct`, `because`, `nullable-subject` |

## See also

- [fa-contain-equivalent-of-plan.md](fa-contain-equivalent-of-plan.md)
- [fa-contain-equivalent-of-verify.md](fa-contain-equivalent-of-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/`
