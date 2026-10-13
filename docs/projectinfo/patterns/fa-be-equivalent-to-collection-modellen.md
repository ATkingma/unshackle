# Modellen: BeEquivalentTo op een collectie

`BeEquivalentTo` op een collectie wordt `ShouldBe(..., ignoreOrder: true)`. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` en `ParamsArraySyntax.cs`. Diagrammen met de hand in Mermaid (kingmadoc/d2 ontbreken).

**Figure 1.** Klassen.

```mermaid
classDiagram
    IPattern <|.. FluentAssertionPattern
    FluentAssertionPattern <|-- BeEquivalentToObjectPattern
    BeEquivalentToObjectPattern ..> ParamsArraySyntax : Create
    EqualSequencePattern ..> ParamsArraySyntax : Create
    BeEquivalentToObjectPattern ..> FluentAssertionsSyntax : Invoke
    BeEquivalentToObjectPattern ..> PatternResult : Fixed / Manual
```

| Element | Pad | Rol |
| --- | --- | --- |
| `BeEquivalentToObjectPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` | `RewriteBeforeArguments` (params) en `RewriteAssertion` (collectie); `IsValueLike`, `ElementType` |
| `ParamsArraySyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/ParamsArraySyntax.cs` | Bouwt `new T[] { ... }` met behoud van regels en commentaar |
| `EqualSequencePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EqualSequencePattern.cs` | Gebruikt dezelfde helper |

**Figure 2.** Volgorde voor de params-vorm.

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

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Params? | `RewriteBeforeArguments` | Alleen de uitgebreide vorm (`ArgumentKind.ParamArray`) |
| Waarde-item | `IsValueLike` | Leaf, sealed record, record struct, struct met leaf-leden |
| Array | `ParamsArraySyntax.Create` | `new string[] { "a", "b" }`, meerregelig met komma en commentaar |

**Figure 3.** Beslisboom.

```mermaid
flowchart TD
    A{NotBeEquivalentTo?} -- ja --> M1[Manual: no ShouldNotBeEquivalentTo]
    A -- nee --> B{Uitgebreide params-vorm?}
    B -- ja --> C{Elementtype gelijk en waarde-item?}
    C -- ja --> F1["x.ShouldBe(new string[] {...}, ignoreOrder: true)"]
    C -- nee --> M2[Manual: params items]
    B -- nee --> D{Subject en verwachting beide collecties?}
    D -- ja --> E{Zelfde elementtype en waarde-item?}
    E -- ja --> F2["x.ShouldBe(expected, ignoreOrder: true, because)"]
    E -- nee --> M3[Manual: other items / no leaf, record or struct]
    D -- nee --> G[Bestaande objectvergelijking]
```

| Vraag | Uitkomst | Proof |
| --- | --- | --- |
| Params, strings | Fixed | `params-strings`, `edge-collection-params`, `params-multi-line` |
| Array als argument | Fixed | `params-array` |
| Waarde-items | Fixed | `collection-ints`, `collection-record`, `collection-struct` |
| Ander elementtype | Manual | `collection-element-types` |
| Class, niet-sealed record, genest | Manual | `collection`, `collection-record-unsealed`, `collection-record-nested` |

## Zie ook

- [fa-be-equivalent-to-collection-plan.md](fa-be-equivalent-to-collection-plan.md)
- [fa-be-equivalent-to-collection-verify.md](fa-be-equivalent-to-collection-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`
