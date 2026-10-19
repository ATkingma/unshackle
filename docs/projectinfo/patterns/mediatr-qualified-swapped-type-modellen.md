# Models: mediatr.qualified-swapped-type

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/QualifiedSwappedTypePattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `QualifiedSwappedTypePattern` | `IPattern`: finds and rewrites qualified swapped type names |
| `MediatRFix.SwappedTypeNames` | Static list of swapped types (also `MediatRFix.SwappedTypes`) |
| `QualifiedNameSyntax`, `AliasQualifiedNameSyntax` | `MediatR.X`, `global::MediatR.X` |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find`, `IsOldNamespace`, `IsSwapped` | Qualified names with left side `MediatR`/`global::MediatR` whose symbol's metadata name is in `SwappedTypeNames` |
| Rewrite | `Rewrite` | Left side → `Mediator` (alias `global::` kept), trivia kept; `Fixed` |

## Decision tree

| Leaf | Condition | Proof |
| ---- | --------- | ----- |
| Fixed `Mediator.X` | `MediatR.IRequest<T>` | `QualifiedSwappedType/record-base` |
| Fixed `global::Mediator.X` | `global::MediatR.INotification` | `QualifiedSwappedType/record-base` |
| Not found | non-swapped type, alias, other namespace | (no finding) |
| Fixed together with a handler rewrite | `MediatR.IRequestHandler<Q, R>` base | `_Combined/qualified-request-afripay` |

## See also

- [mediatr-qualified-swapped-type-plan.md](mediatr-qualified-swapped-type-plan.md)
- [mediatr-qualified-swapped-type-verify.md](mediatr-qualified-swapped-type-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/QualifiedSwappedType/`
