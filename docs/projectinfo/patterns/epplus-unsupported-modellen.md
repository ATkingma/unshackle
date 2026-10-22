# Models: epplus.unsupported

Facts from `src/Unshackle.Core/Fixes/EPPlus/Patterns/UnsupportedPattern.cs` and `EPPlusApi.cs`.

## Figure 1: class diagram

```mermaid
classDiagram
    class IPattern
    class UnsupportedPattern { Find; Rewrite; UnmigratedCode; PublicApiCode }
    class EPPlusApi { Name(name, model); Row(api); Mentions(type) }
    class LicenseContextPattern
    class PackagePattern
    IPattern <|.. UnsupportedPattern
    UnsupportedPattern ..> EPPlusApi
    UnsupportedPattern ..> LicenseContextPattern : spans already reported
    UnsupportedPattern ..> PackagePattern : spans already reported
```

## Figure 2: sequence

```mermaid
sequenceDiagram
    Fixer->>LicenseContextPattern: rewrite (fresh model)
    Fixer->>PackagePattern: rewrite (fresh model)
    Fixer->>UnsupportedPattern: Find(root of the result)
    UnsupportedPattern->>EPPlusApi: Name(each simple name)
    UnsupportedPattern-->>Fixer: one member per EPPlus use
    Fixer->>UnsupportedPattern: Rewrite(member)
    UnsupportedPattern-->>Fixer: Manual [PublicApiExposesOldLibrary] or [UnmigratedEPPlusApi]
```

## Figure 3: decision tree

```mermaid
flowchart TD
    A[member with an EPPlus name] --> B{inside a licence or package finding?}
    B -- yes --> S[skipped]
    B -- no --> C{visible outside, signature names EPPlus, packable?}
    C -- yes --> E6[Manual PublicApiExposesOldLibrary]
    C -- no --> U[Manual UnmigratedEPPlusApi: APIs with rows]
```

| Question | Leaf | Proof / test |
| -------- | ---- | ------------ |
| cells and values in an internal method | Unmigrated, rows 3/4/5 | `cells` |
| chart | Unmigrated, no row planned | `chart` |
| public signature, library | E6 | `public-api` |
| internal signature / test project | Unmigrated | `internal-api`, `UnsupportedPattern_PublicApiInTestProject_NotE6` |
| unresolved 5+ member | Unmigrated, "(unresolved, EPPlus 5+)" | `UnsupportedPattern_UnresolvedEPPlus5Member_NamedWithItsReceiver` |
| package left Manual | skipped | `Fix_SaveAsStream_ManualAndSolutionUntouched` |

## See also

- [plan](epplus-unsupported-plan.md), [verify](epplus-unsupported-verify.md)
