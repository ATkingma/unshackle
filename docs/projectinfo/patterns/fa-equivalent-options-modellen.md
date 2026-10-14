# Models: fa.equivalent-options

`BeEquivalentTo(y, o => ...)`: options are dropped, become `ShouldBe`, or Manual. Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs`. Diagrams by hand in Mermaid (kingmadoc/d2 missing).

**Figure 1.** Classes around `EquivalentOptionsPattern`.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- BeEquivalentToObjectPattern
    BeEquivalentToObjectPattern <|-- EquivalentOptionsPattern
    EquivalentOptionsPattern ..> FluentAssertionsSyntax : FindInvocations, Invoke
    EquivalentOptionsPattern ..> PatternResult : Fixed / Manual
    PatternResult <|-- FixedResult
    PatternResult <|-- ManualResult
```

| Element | Path | Role |
| --- | --- | --- |
| `BeEquivalentToObjectPattern` | `.../Patterns/BeEquivalentToObjectPattern.cs` | Base: object and leaf-collection form; `HasOptions`, `ElementType`, `IsLeaf` are protected; its `Find` skips `BeEquivalentTo` with options |
| `EquivalentOptionsPattern` | `.../Patterns/EquivalentOptionsPattern.cs` | This pattern, Id `fa.equivalent-options` |
| `FluentAssertionsSyntax` | `.../FluentAssertionsSyntax.cs` | `FindInvocations`, `Invoke` |
| `PatternResult` etc. | `src/Unshackle.Core/Models/` | Outcome |

**Figure 2.** Sequence.

```mermaid
sequenceDiagram
    Fixer->>EquivalentOptionsPattern: Find(root, model)
    EquivalentOptionsPattern->>FluentAssertionsSyntax: FindInvocations("BeEquivalentTo") + HasOptions
    Fixer->>EquivalentOptionsPattern: Rewrite (FluentAssertionPattern)
    EquivalentOptionsPattern->>EquivalentOptionsPattern: RewriteAssertion
    EquivalentOptionsPattern->>EquivalentOptionsPattern: TryGetOptionCalls, OptionReason
    alt strict order on a collection
        EquivalentOptionsPattern->>EquivalentOptionsPattern: IsValueLike, StrictOrdering
        EquivalentOptionsPattern->>FluentAssertionsSyntax: Invoke("ShouldBe")
    else options dropped
        EquivalentOptionsPattern->>BeEquivalentToObjectPattern: RewriteAssertion (without lambda)
    end
    EquivalentOptionsPattern-->>Fixer: Fixed / Manual
```

| Step | Place | What happens |
| --- | --- | --- |
| Find | `Find` | Only `BeEquivalentTo` with an `EquivalencyAssertionOptions` parameter |
| Read the lambda | `TryGetOptionCalls` | The chain calls of `o => o.A().B()` in order |
| Options | `RewriteAssertion` | Strict flag, no-ops, otherwise a reason per option |
| Value items | `IsValueLike` | Leaf, sealed record, record struct or struct with only leaf fields |

**Figure 3.** Decision tree.

```mermaid
flowchart TD
    A{Inline lambda o => o.Option?} -- no --> M0[Manual: no inline lambda: block-body, options-variable]
    A -- yes --> B{Option other than WithStrictOrdering, WithoutStrictOrdering, ExcludingMissingMembers, ThrowingOnMissingMembers?}
    B -- yes --> M1[Manual: reason per option: excluding, including, using-comparer, comparing-by-value, respecting-runtime-types, with-tracing]
    B -- no --> C{Last strict flag on and subject is a collection?}
    C -- yes --> D{Item types equal?}
    D -- no --> M2[Manual: strict-ordering-item-types]
    D -- yes --> E{Items value-comparable?}
    E -- no --> M3[Manual: strict-ordering-class, -record-unsealed, -record-computed, -record-list-member]
    E -- yes --> F1["x.ShouldBe(y) or ShouldBe(y, ignoreOrder: false, because): strict-ordering, -record, -record-struct, -struct, -because"]
    C -- no --> G[BeEquivalentToObjectPattern without options]
    G --> H{Base Fixed?}
    H -- yes --> F2["ShouldBeEquivalentTo(y) or ShouldBe(y, ignoreOrder: true): excluding-missing-members, without-strict-ordering, strict-ordering-object"]
    H -- no --> M4[Manual from the base; with ExcludingMissingMembers and other types an own reason: excluding-missing-members-other-type]
```

| Question | Outcome | Proof |
| --- | --- | --- |
| Inline lambda? | Otherwise Manual | `block-body`, `options-variable` |
| Unknown or changing option? | Manual per option | `excluding`, `excluding-two`, `including`, `using-comparer`, ... |
| Strict on a collection, value items? | `ShouldBe(y)` | `strict-ordering`, `strict-ordering-record` |
| Otherwise | Base pattern without the lambda | `without-strict-ordering`, `strict-ordering-object` |

## See also

- [fa-equivalent-options-plan.md](fa-equivalent-options-plan.md)
- [fa-equivalent-options-verify.md](fa-equivalent-options-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/`
