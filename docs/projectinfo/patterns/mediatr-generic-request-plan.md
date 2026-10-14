---
kingmadoc: 1
feature: "mediatr-generic-request"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericRequestPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/GenericRequest/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/identified-command-eshop.before.cs
---

# Feature: mediatr.generic-request: generic request → abstract base + concrete subclass per closed type

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (by hand)    |

> Drawn up from TAAK-193 (dotnet/eShop `IdentifiedCommand<T, R>`, kgrzybek `DomainNotificationBase<T>`).

## One-sentence summary

`class G<T, R> : IRequest<R>` becomes `abstract`, gets a `class GOfXAndY : G<X, Y>` with the same constructors per closed type from the solution,
every closed `G<X, Y>` (handlers, `new`, validators, DI keys) becomes `GOfXAndY`, and a concrete handler that inherits `IRequestHandler<G<X, Y>, Y>` from an
abstract generic base gets `IRequestHandler<GOfXAndY, Y>` with a `Handle` that calls the base.

## Scope (in / out)

**In**: a non-abstract, non-static, top-level generic class in the source that is a MediatR request (not a stream request); a generic request or
generic notification that is only the base of concrete subclasses (only becomes `abstract`).

**Out (Manual)**: record, partial or file-local; `typeof(G<>)`; `new G<TParam>` in generic code; a type argument that is not a plain named type;
a closed reference in an override signature; a closed type that already has a subclass; a name clash; a closed type not visible from `G`'s project;
only private constructors or constructors with `ref`/`params`/optional parameters; an inherited `Handle` that is explicit, overridden or not public.

## Requirements

- **REQ-1**: WHEN closed `G<X, Y>` occur in the solution THE SYSTEM SHALL make `G` abstract and write `{G}Of{X}And{Y}` per closed type (CLR name for special types: `Boolean`), constructors passed through (proof `closed-uses`, `_Combined/identified-command-eshop`).
- **REQ-2**: THE SYSTEM SHALL rename every closed reference outside `G` and outside the base list of an existing subclass, keeping the qualifier (proof `closed-uses`).
- **REQ-3**: WHEN a concrete handler inherits `IRequestHandler<G<X, Y>, Y>` THE SYSTEM SHALL add `IRequestHandler<GOfXAndY, Y>` and `Handle(GOfXAndY, ct) => base.Handle(...)` (proof `_Combined/identified-command-eshop`).
- **REQ-4**: WHEN a closed type is only used in another project THE SYSTEM SHALL still write its subclass next to `G`; if the type itself is in a project that does not see `G` THEN Manual (unit tests `RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass`, `RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual`).
- **REQ-5**: WHEN a generic message is only the base of concrete subclasses THE SYSTEM SHALL only make it `abstract` (unit test `Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration`, `_Combined/generic-decorators-kgrzybek`).
- **REQ-6**: IF `typeof(G<>)`, open creation, a non-plain type argument or a record THEN Manual (proofs `unbound-typeof`, `open-creation`, `not-plain-argument`, `derived-generic-record`).
- **REQ-7**: THE SYSTEM SHALL give a `Warning` at the declaration: `GetType()` is now the subclass (log texts change, behaviour does not).

## Assumptions

- Measured (probe p1, TAAK-193): the generator of Mediator 3.0.2 only sees `G<T, R>` (MSG0005) and does not dispatch `G<X, bool>` (MissingMessageHandlerException);
  a handler of a base type is **not** used for a derived request (unlike notifications), so the handler must implement the subtype itself.
- Validators and DI keys such as `IValidator<G<X, Y>>` must be renamed too, otherwise validation silently disappears.

## Models

[mediatr-generic-request-modellen.md](mediatr-generic-request-modellen.md).

## See also

- [mediatr-generic-request-verify.md](mediatr-generic-request-verify.md)
- [mediatr-generic-notification-plan.md](mediatr-generic-notification-plan.md) — same approach for notifications (TAAK-192)
- `claude/tasks/archief/TAAK-193-mediatr-generieke-decorators-messages.md`
