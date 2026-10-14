---
kingmadoc: 1
feature: "mediatr-generic-notification"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/GenericNotification/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/generic-notification-nadirbad.before.cs
---

# Feature: mediatr.generic-notification: generic notification → abstract base + concrete subclass per closed type

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand)    |

> Drawn up from TAAK-192 (corpus nadirbad).

## One-sentence summary

`class G<T> : INotification` becomes `abstract`, gets a `class GOfX : G<X>` with the same constructors per closed type `X` from the solution, and `new G<X>(...)` / `typeof(G<>).MakeGenericType(x)` become `new GOfX(...)` / `GTypes.Close(x)`.

## Scope (in / out)

**In**: a non-abstract, top-level generic class (or record class) with one type parameter that implements MediatR `INotification` and is not a request.
**Out (Manual)**: partial or file-local, existing subclasses, `new G<TParam>` in generic code, `typeof(G<>)` other than in `MakeGenericType(x)`, `MakeGenericType` without a class/interface constraint, a closed type not visible from `G`'s project, a name clash, only private constructors. Arity > 1 stays with `mediatr.unsupported`.

## Requirements

- **REQ-1**: WHEN `typeof(G<>).MakeGenericType(x)` occurs in the solution and `T : C` THE SYSTEM SHALL write a subclass for every concrete non-generic type with `C` in the solution and `GTypes.Close(x)` with an `if` chain (proof `make-generic-type`).
- **REQ-2**: WHEN `new G<X>(...)` or `G<X> v = new(...)` occurs THE SYSTEM SHALL write `new GOfX(...)`; parameters with a default value come along (proof `new-closed`, also `sealed` removed).
- **REQ-3**: WHEN `G` is the last type in a file without a final line ending THE SYSTEM SHALL separate the new types with a blank line (SA1513, proof `last-in-file`).
- **REQ-4**: WHEN the solution contains the nadirbad case (handler `INotificationHandler<G<X>>`, `Activator.CreateInstance(typeof(G<>).MakeGenericType(...))`) THE SYSTEM SHALL convert it with the whole fix without Manual, and it compiles against Mediator 3.0.2 with the generator (proof `_Combined/generic-notification-nadirbad`).
- **REQ-5**: IF a closed type is in a project that `G` cannot see, or `G` already has a subclass THEN Manual (unit tests `GenericNotificationPatternTests`).
- **REQ-6**: IF `new G<TParam>(...)` occurs in generic code, or `MakeGenericType` without a constraint THEN Manual (proofs `generic-code`, `no-constraint`).

## Assumptions

- Mediator 3.0.2 (probe `<probe-dir>/probe-192`): a generic concrete notification gives CS0246 in `Mediator.g.cs`; the generator skips abstract types (TAAK-132); a handler of `G<X>` gets `GOfX` via polymorphic dispatch, also when published as `INotification` after `Activator.CreateInstance` (3/3 called).
- MSG0005 (subclass without a handler) is no longer an error (TAAK-136); MediatR also publishes silently to nothing without a handler.
- Behaviour difference: `GetType()` is now `GOfX`; the finding carries a `Warning`.

## Models

[mediatr-generic-notification-modellen.md](mediatr-generic-notification-modellen.md).

## See also

- [mediatr-generic-notification-verify.md](mediatr-generic-notification-verify.md)
- `claude/tasks/archief/TAAK-192-mediatr-generieke-notification.md`
