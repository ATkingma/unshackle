# Models: mediatr.generic-notification

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `GenericNotificationPattern` | `ISolutionPattern`; declaration with the whole solution (`RewriteAsync`), usage sites locally |
| `ClosedTypes` (nested) | Outcome of `Scan`: closed type arguments, types with the constraint, `MakeGenericType` yes/no, the first subclass |
| `MediatRSyntax.IsMediatR` | Recognises MediatR `INotification` / requests |
| `FixedResult` (with `Warning`) / `ManualResult` | Outcome |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` | Candidate declaration (`IsCandidate`), `new` with a candidate type, `typeof(G<>)` (as a `MakeGenericType` call or on its own) outside the candidate itself |
| Solution | `RewriteAsync` | Compilations of all C# projects → `RewriteDeclaration` |
| Scan | `Scan` | Bind every `G<...>` in every tree; `GetSymbolsWithName` for subclasses and types with the constraint |
| Write | `RewriteDeclaration` | `MakeAbstract`, a subclass per type (`Parameters` replaces `T`), possibly `GTypes.Close` |
| Usage | `RewriteUse` | `Retarget` keeps the qualifier (`N.G<X>` → `N.GOfX`) |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Fixed: abstract + subclasses + `Close` | `MakeGenericType` and `T : C` | `make-generic-type`, `_Combined/generic-notification-nadirbad` |
| Fixed: abstract + subclasses | only closed references | `new-closed`, `last-in-file` |
| Fixed: `new GOfX(...)` | `new G<X>` / target-typed `new` | `new-closed` |
| Manual "concrete notification" | `new G<TParam>`, never closed | `generic-code` |
| Manual "no class or interface constraint" | `MakeGenericType` without a constraint | `no-constraint` |
| Manual "not visible from" / "already has a subclass" | project boundary, an existing subclass | `GenericNotificationPatternTests` |

## See also

- [mediatr-generic-notification-plan.md](mediatr-generic-notification-plan.md)
- [mediatr-generic-notification-verify.md](mediatr-generic-notification-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/GenericNotification/`
