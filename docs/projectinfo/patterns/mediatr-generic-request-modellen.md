# Models: mediatr.generic-request

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericRequestPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `GenericRequestPattern` | `ISolutionPattern` + `ISolutionFinder`: find and rewrite the declaration with the compilations of all C# projects |
| `IsCandidate` (static) | Generic request, not a stream request; used by `UnsupportedPattern` |
| `Uses` (nested) | Outcome of `Scan`: closed type arguments, existing subclasses, the first `typeof(G<>)`, the first open creation |
| `GenericNotificationPattern` | Handles generic notifications with closed usage; this pattern only the notification that is only a base |
| `FixedResult` (with `Warning`) / `ManualResult` | Outcome |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `FindNodes` | Candidate declaration, only-subclassed notification, concrete handler with an inherited `IRequestHandler<G<X, Y>, Y>` (`InheritedHandlers`), closed `G<X, Y>` (`Use`) |
| Solution | `FindAsync` / `RewriteAsync` | Compilations of all C# projects; usage sites only with the own model |
| Scan | `Scan` | Every `G<...>` in every tree: closed, subclass, unbound, open creation |
| Declaration | `RewriteNode` | Manual checks, map types to `G`'s project, `MakeAbstract`, subclasses with passed-through constructors |
| Handler | `RewriteNode` (handler branch) | `IRequestHandler<GOfXAndY, Y>` in the base list + a bridge `Handle` to `base.Handle` |
| Usage | `RewriteUse` | `G<X, Y>` → `GOfXAndY`, the qualifier stays |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Fixed: only `abstract` | no closed usage, but concrete subclasses | `Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration`, `_Combined/generic-decorators-kgrzybek` |
| Manual "not a plain" | type argument not a plain named type | `not-plain-argument` |
| Manual "record, partial or file-local" | declared that way | `derived-generic-record` |
| Manual "becomes abstract" (typeof) | `typeof(G<>)` | `unbound-typeof` |
| Manual "open type argument; callers not followed" | `new G<TParam>` in generic code | `open-creation` |
| Manual "not visible from" | a closed type in a project out of `G`'s reach | `RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual` |
| Fixed: abstract + subclasses + renaming | closed usage (also in other projects) | `closed-uses`, `RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass` |
| Fixed: bridge `Handle` | a concrete handler inherits from an abstract generic base | `_Combined/identified-command-eshop` |

## See also

- [mediatr-generic-request-plan.md](mediatr-generic-request-plan.md)
- [mediatr-generic-request-verify.md](mediatr-generic-request-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/GenericRequest/`
