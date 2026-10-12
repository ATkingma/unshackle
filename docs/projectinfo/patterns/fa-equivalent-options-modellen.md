# Modellen: fa.equivalent-options

`BeEquivalentTo(y, o => ...)`: opties vervallen, worden `ShouldBe` of Manual. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs`. Diagrammen met de hand in Mermaid (kingmadoc/d2 ontbreken).

**Figure 1.** Klassen rond `EquivalentOptionsPattern`.

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

| Element | Pad | Rol |
| --- | --- | --- |
| `BeEquivalentToObjectPattern` | `.../Patterns/BeEquivalentToObjectPattern.cs` | Basis: object- en leaf-collectievorm; `HasOptions`, `ElementType`, `IsLeaf` zijn protected; zijn `Find` slaat `BeEquivalentTo` met options over |
| `EquivalentOptionsPattern` | `.../Patterns/EquivalentOptionsPattern.cs` | Dit patroon, Id `fa.equivalent-options` |
| `FluentAssertionsSyntax` | `.../FluentAssertionsSyntax.cs` | `FindInvocations`, `Invoke` |
| `PatternResult` e.a. | `src/Unshackle.Core/Models/` | Uitkomst |

**Figure 2.** Volgorde.

```mermaid
sequenceDiagram
    Fixer->>EquivalentOptionsPattern: Find(root, model)
    EquivalentOptionsPattern->>FluentAssertionsSyntax: FindInvocations("BeEquivalentTo") + HasOptions
    Fixer->>EquivalentOptionsPattern: Rewrite (FluentAssertionPattern)
    EquivalentOptionsPattern->>EquivalentOptionsPattern: RewriteAssertion
    EquivalentOptionsPattern->>EquivalentOptionsPattern: TryGetOptionCalls, OptionReason
    alt strikte volgorde op collectie
        EquivalentOptionsPattern->>EquivalentOptionsPattern: IsValueLike, StrictOrdering
        EquivalentOptionsPattern->>FluentAssertionsSyntax: Invoke("ShouldBe")
    else opties vervallen
        EquivalentOptionsPattern->>BeEquivalentToObjectPattern: RewriteAssertion (zonder lambda)
    end
    EquivalentOptionsPattern-->>Fixer: Fixed / Manual
```

| Stap | Plek | Wat gebeurt er |
| --- | --- | --- |
| Find | `Find` | Alleen `BeEquivalentTo` met een `EquivalencyAssertionOptions`-parameter |
| Lambda lezen | `TryGetOptionCalls` | De ketenaanroepen van `o => o.A().B()` in volgorde |
| Opties | `RewriteAssertion` | Strikt-vlag, no-ops, anders een reden per optie |
| Waarde-items | `IsValueLike` | Leaf, sealed record, record struct of struct met alleen leaf-velden |

**Figure 3.** Beslisboom.

```mermaid
flowchart TD
    A{Inline lambda o => o.Optie?} -- nee --> M0[Manual: no inline lambda: block-body, options-variable]
    A -- ja --> B{Optie buiten WithStrictOrdering, WithoutStrictOrdering, ExcludingMissingMembers, ThrowingOnMissingMembers?}
    B -- ja --> M1[Manual: reden per optie: excluding, including, using-comparer, comparing-by-value, respecting-runtime-types, with-tracing]
    B -- nee --> C{Laatste strikt-vlag aan en subject is collectie?}
    C -- ja --> D{Itemtypes gelijk?}
    D -- nee --> M2[Manual: strict-ordering-item-types]
    D -- ja --> E{Items waarde-vergelijkbaar?}
    E -- nee --> M3[Manual: strict-ordering-class, -record-unsealed, -record-computed, -record-list-member]
    E -- ja --> F1["x.ShouldBe(y) of ShouldBe(y, ignoreOrder: false, because): strict-ordering, -record, -record-struct, -struct, -because"]
    C -- nee --> G[BeEquivalentToObjectPattern zonder opties]
    G --> H{Basis Fixed?}
    H -- ja --> F2["ShouldBeEquivalentTo(y) of ShouldBe(y, ignoreOrder: true): excluding-missing-members, without-strict-ordering, strict-ordering-object"]
    H -- nee --> M4[Manual uit basis; bij ExcludingMissingMembers en andere types eigen reden: excluding-missing-members-other-type]
```

| Vraag | Uitkomst | Proof |
| --- | --- | --- |
| Inline lambda? | Anders Manual | `block-body`, `options-variable` |
| Onbekende of veranderende optie? | Manual per optie | `excluding`, `excluding-two`, `including`, `using-comparer`, ... |
| Strikt op collectie, waarde-items? | `ShouldBe(y)` | `strict-ordering`, `strict-ordering-record` |
| Anders | Basispatroon zonder lambda | `without-strict-ordering`, `strict-ordering-object` |

## Zie ook

- [fa-equivalent-options-plan.md](fa-equivalent-options-plan.md)
- [fa-equivalent-options-verify.md](fa-equivalent-options-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/`
