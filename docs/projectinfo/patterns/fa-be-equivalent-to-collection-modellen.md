# Models: BeEquivalentTo on a collection

`BeEquivalentTo` on a collection becomes `ShouldBe(..., ignoreOrder: true)`. Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` and `ParamsArraySyntax.cs`. Diagrams by hand in Mermaid (kingmadoc/d2 missing).

**Figure 1.** Classes.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- BeEquivalentToObjectPattern
    BeEquivalentToObjectPattern ..> ParamsArraySyntax : Create
    EqualSequencePattern ..> ParamsArraySyntax : Create
    BeEquivalentToObjectPattern ..> FluentAssertionsSyntax : Invoke
    BeEquivalentToObjectPattern ..> PatternResult : Fixed / Manual
```

| Element | Path | Role |
| --- | --- | --- |
| `BeEquivalentToObjectPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` | `RewriteBeforeArguments` (params) and `RewriteAssertion` (collection); `IsValueLike`, `ElementType` |
| `ParamsArraySyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/ParamsArraySyntax.cs` | Builds `new T[] { ... }` keeping lines and comments |
| `EqualSequencePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs` | Uses the same helper |

**Figure 2.** Sequence for the params form.

```mermaid
sequenceDiagram
    Fixer->>BeEquivalentToObjectPattern: Rewrite(node)
    BeEquivalentToObjectPattern->>BeEquivalentToObjectPattern: RewriteBeforeArguments
    BeEquivalentToObjectPattern->>SemanticModel: GetOperation (ParamArray?)
    BeEquivalentToObjectPattern->>BeEquivalentToObjectPattern: ElementType, IsValueLike
    BeEquivalentToObjectPattern->>ParamsArraySyntax: Create
    BeEquivalentToObjectPattern->>FluentAssertionsSyntax: Invoke("ShouldBe", array, ignoreOrder)
    BeEquivalentToObjectPattern-->>Fixer: Fixed / Manual
```

| Step | Place | What happens |
| --- | --- | --- |
| Params? | `RewriteBeforeArguments` | Only the expanded form (`ArgumentKind.ParamArray`) |
| Value item | `IsValueLike` | Leaf, sealed record, record struct, struct with leaf members |
| Array | `ParamsArraySyntax.Create` | `new string[] { "a", "b" }`, multi-line with comma and comments |

**Figure 3.** Decision tree.

```mermaid
flowchart TD
    A{NotBeEquivalentTo?} -- yes --> M1[Manual: no ShouldNotBeEquivalentTo]
    A -- no --> B{Expanded params form?}
    B -- yes --> C{Element type equal and value item?}
    C -- yes --> F1["x.ShouldBe(new string[] {...}, ignoreOrder: true)"]
    C -- no --> M2[Manual: params items]
    B -- no --> D{Subject and expectation both collections?}
    D -- yes --> E{Same element type and value item?}
    E -- yes --> F2["x.ShouldBe(expected, ignoreOrder: true, because)"]
    E -- no --> M3[Manual: other items / no leaf, record or struct]
    D -- no --> G[Existing object comparison]
```

| Question | Outcome | Proof |
| --- | --- | --- |
| Params, strings | Fixed | `params-strings`, `edge-collection-params`, `params-multi-line` |
| Array as the argument | Fixed | `params-array` |
| Value items | Fixed | `collection-ints`, `collection-record`, `collection-struct` |
| Other element type | Manual | `collection-element-types` |
| Class, non-sealed record, nested | Manual | `collection`, `collection-record-unsealed`, `collection-record-nested` |

## See also

- [fa-be-equivalent-to-collection-plan.md](fa-be-equivalent-to-collection-plan.md)
- [fa-be-equivalent-to-collection-verify.md](fa-be-equivalent-to-collection-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`
