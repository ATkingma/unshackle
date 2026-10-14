# Models: fa.not-equal

`x.Should().NotEqual(y)` becomes `x.ShouldNotBe(y)`. Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs`. Diagrams by hand in Mermaid (kingmadoc/d2 missing).

**Figure 1.** Classes around `NotEqualPattern`.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- NotEqualPattern
    NotEqualPattern ..> FluentAssertionsSyntax : FindInvocations, Invoke
    NotEqualPattern ..> PatternResult : Fixed / Manual
    PatternResult <|-- FixedResult
    PatternResult <|-- ManualResult
```

| Element | Path | Role |
| --- | --- | --- |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: scope, shape, result, because(-args) |
| `NotEqualPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs` | This pattern, Id `fa.not-equal` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | `FindInvocations`, `Invoke` |
| `PatternResult` etc. | `src/Unshackle.Core/Models/` | Outcome |

**Figure 2.** Sequence.

```mermaid
sequenceDiagram
    Fixer->>NotEqualPattern: Find(root, model)
    NotEqualPattern->>FluentAssertionsSyntax: FindInvocations("NotEqual")
    Fixer->>NotEqualPattern: Rewrite
    NotEqualPattern->>NotEqualPattern: RewriteBeforeArguments (dictionary?)
    NotEqualPattern->>NotEqualPattern: RewriteAssertion
    NotEqualPattern->>FluentAssertionsSyntax: Invoke("ShouldNotBe")
    NotEqualPattern-->>Fixer: Fixed / Manual
```

| Step | Place | What happens |
| --- | --- | --- |
| Find | `NotEqualPattern.Find` | All `NotEqual` calls on FA |
| Check | `RewriteBeforeArguments` | Dictionary overload → Manual |
| Rewrite | `RewriteAssertion` | `ShouldNotBe`, possibly with `IEnumerable<T>` |

**Figure 3.** Decision tree.

```mermaid
flowchart TD
    A{In AssertionScope?} -- yes --> M1[Manual: assertion-scope]
    A -- no --> B{Result used?}
    B -- yes --> M2[Manual: result-used]
    B -- no --> C{On Generic/StringCollectionAssertions?}
    C -- no --> M3[Manual: dictionary]
    C -- yes --> N{Subject proven non-null?}
    N -- no --> G[ShouldNotBeNull before it: nullable, same-type]
    N -- yes --> D{Types of subject and argument equal?}
    G --> D
    D -- yes --> F1["x.ShouldNotBe(y)"]
    D -- no --> F2["x.ShouldNotBe IEnumerable T (y)"]
```

| Question | Outcome | Proof |
| --- | --- | --- |
| AssertionScope? | Manual | `assertion-scope` |
| Result used? | Manual | `result-used` |
| Collection assertions? | Otherwise Manual | `dictionary` |
| Types equal? | `ShouldNotBe(y)` or with `IEnumerable<T>` | `same-type`, `interface-type`, `string-collection`, `because`, `because-args`, `nullable` / `different-type`, `array-literal` |

## See also

- [fa-not-equal-plan.md](fa-not-equal-plan.md)
- [fa-not-equal-verify.md](fa-not-equal-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotEqualPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotEqual/`
