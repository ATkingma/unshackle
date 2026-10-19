---
kingmadoc: 1
feature: "mediatr-qualified-swapped-type"
status: approved
requirements: [REQ-1, REQ-2]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/QualifiedSwappedTypePattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/QualifiedSwappedType/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/
---

# Feature: mediatr.qualified-swapped-type: swapped types written with their namespace

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-19 (by hand, kingmadoc missing) |

> Drafted from TAAK-288 (afripay corpus `after`: 20 CS0246 on `MediatR.IRequest<T>` in files without `using MediatR;`).

## One-sentence summary

A type that `MediatRFix.SwappedTypes` swaps by using, but that is written with its namespace (`MediatR.IRequest<T>`, `global::MediatR.INotification`), gets `Mediator` as its namespace part.

## Scope (in / out)

**In scope**: `QualifiedNameSyntax` whose left side is `MediatR` or `global::MediatR` and whose symbol is a swapped MediatR type.

**Out of scope**: qualified names of non-swapped MediatR types (other patterns or the `unmatched-use` safety net report those); aliases (`using M = MediatR;`).

## Requirements

- **REQ-1**: WHEN a swapped MediatR type is written as `MediatR.X` or `global::MediatR.X` THE SYSTEM SHALL rewrite the namespace part to `Mediator` (proof `QualifiedSwappedType/record-base`).
- **REQ-2**: WHEN such a name is the base of a handler that another pattern rewrites THE SYSTEM SHALL still produce compiling code (the pattern runs last; proof `_Combined/qualified-request-afripay`).

## Assumptions

- The swapped types have the same name and arity in Mediator 3.0.2 (checked by `MediatRFixTests`/`SwappedTypesTests`); `MediatRFix.SwappedTypeNames` is the single list.
- Before this pattern, such a file had no finding and no `using MediatR;`: the project dropped the package and the build broke silently (CS0246).

## Models

[mediatr-qualified-swapped-type-modellen.md](mediatr-qualified-swapped-type-modellen.md).

## See also

- [mediatr-qualified-swapped-type-verify.md](mediatr-qualified-swapped-type-verify.md)
- `claude/tasks/archief/TAAK-288-mediatr-requesthandlerdelegate-in-tests.md`
