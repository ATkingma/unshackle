# Modellen: fa.contain-equivalent-of

`ContainEquivalentOf(item)` wordt `ShouldContain(item)` voor waarde-items. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs`. Diagrammen met de hand in Mermaid (kingmadoc/d2 ontbreken).

**Figure 1.** Klassen rond `ContainEquivalentOfPattern`.

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

| Element | Pad | Rol |
| --- | --- | --- |
| `BeEquivalentToObjectPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` | Levert `ElementType`, `IsValueLike`, `IsLeaf` |
| `ContainEquivalentOfPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs` | Dit patroon, Id `fa.contain-equivalent-of` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Hulpmethodes |
| `PatternResult` e.a. | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde.

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

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `Find` | Alle `ContainEquivalentOf`/`NotContainEquivalentOf` op FA |
| Typecheck | `RewriteAssertion` | Element- en verwachtingstype via `ElementType` en `GetTypeInfo` |
| Waarde-item | `IsValueLike` | Leaf, sealed record, record struct, struct met leaf-leden |
| Herschrijven | `FluentAssertionsSyntax.Invoke` | `ShouldContain(item, because)` met `NotNull` op het subject |

**Figure 3.** Beslisboom.

```mermaid
flowchart TD
    A{AssertionScope of resultaat gebruikt?} -- ja --> M1[Manual]
    A -- nee --> B{Meer dan een waarde, options?}
    B -- ja --> M2[Manual: options]
    B -- nee --> C{Subject een generieke IEnumerable?}
    C -- nee --> M3[Manual: not IEnumerable]
    C -- ja --> D{Verwachtingstype gelijk aan elementtype?}
    D -- nee --> M4[Manual: different-type]
    D -- ja --> E{Element waarde-achtig?}
    E -- nee --> M5[Manual: class, dictionary, record met collectie]
    E -- ja --> F["x.ShouldContain(item) / ShouldNotContain(item)"]
```

| Vraag | Uitkomst | Proof |
| --- | --- | --- |
| AssertionScope? | Manual | `assertion-scope` |
| Resultaat gebruikt? | Manual | `which` |
| Options? | Manual | `options` |
| Ander verwachtingstype? | Manual | `different-type` |
| Geen waarde-item? | Manual | `class`, `record-unsealed`, `record-list-member`, `dictionary` |
| Gewoon geval | Fixed | `simple`, `strings`, `sealed-record`, `record-struct`, `because`, `nullable-subject` |

## Zie ook

- [fa-contain-equivalent-of-plan.md](fa-contain-equivalent-of-plan.md)
- [fa-contain-equivalent-of-verify.md](fa-contain-equivalent-of-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/`
