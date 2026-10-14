# TAAK-193 — Migrate generic decorators, handlers and requests automatically (kgrzybek, eShop)

**Status**: done (corpus repeat kgrzybek/eShop open, see `CORPUSRUN-mediatr.md`)
**Created**: 2026-10-12
**Goal**: migrate the 32 generic `mediatr.unsupported` Manuals of kgrzybek and `IdentifiedCommand<T, R>` of dotnet/eShop without Manual, with the same runtime behaviour.

## Context for the next agent

- Corpus: `claude/tasks/CORPUSRUN-mediatr.md` ("Repeat after TAAK-180..184", rightful Manuals). kgrzybek `91c8ef2`, eShop `59f1a6c`.
- kgrzybek: per module six decorators (`Logging/UnitOfWork/ValidationCommandHandler[WithResult]Decorator<T[, TResult]> : ICommandHandler<T[, TResult]>`,
  ctor parameter `ICommandHandler<T> decorated`), plus `BuildingBlocks/.../UnitOfWorkCommandHandlerDecorator<T> : IRequestHandler<T>` (registered nowhere)
  and `DomainNotificationBase<T> : IDomainEventNotification<T>` (only the base of concrete notifications). Registration: Autofac
  `builder.RegisterGenericDecorator(typeof(D<>), typeof(ICommandHandler<>))` in `ProcessingModule`.
- eShop: `IdentifiedCommand<T, R> : IRequest<R>`, abstract `IdentifiedCommandHandler<T, R> : IRequestHandler<IdentifiedCommand<T, R>, R>` with
  7 concrete subclasses (`CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>`), `new IdentifiedCommand<X, bool>(...)`
  in `OrdersApi`/integration handlers/tests, `IdentifiedCommandValidator : AbstractValidator<IdentifiedCommand<CreateOrderCommand, bool>>` + DI registration.
- Overlap: TAAK-192 (`GenericNotificationPattern`, notifications with closed usage), TAAK-196 (`AutofacMediatorModulePattern`: handlers stay Autofac's, decorators keep working).
- Probes: `<probe-dir>/probe-193/p1` (Mediator 3.0.2), `p2` (MediatR 12.2 + Autofac 7.1), `p3` (open generic decorator under Mediator).

## Analysis

### Current situation

`UnsupportedPattern` gives Manual on every non-abstract generic type that implements `IRequest*`/`INotification`/`IRequestHandler*`; through `AllOrNothing` kgrzybek and eShop stay entirely MediatR.

### Problem (measured, probe)

- Open generic request handler (decorator) `D<T> : IRequestHandler<T>`: the generator only gives **MSG0003** (warning, "handlers cannot be generic") and skips it; under `TreatWarningsAsErrors` (kgrzybek, eShop) an error.
- Mediator resolves handlers and behaviors via DI (`GetRequiredService<IRequestHandler<TReq, TRes>>`, `GetServices<INotificationHandler<T>>`): a container decorator therefore keeps working.
- Open generic **notification** decorator `D<T> : INotificationHandler<T>`: the generator registers it as a handler of **every** notification → self-recursion (the probe hung). Today not even Manual.
- Generic request `G<T, R> : IRequest<R>`: the generator only finds `G<T, R>` (MSG0005), no dispatch of `G<X, bool>` (MissingMessageHandlerException). A request handler for a base type is **not** used for a derived message (unlike notifications).

### New situation

- Request decorators stay decorators (only Task → ValueTask by the existing handler patterns); MSG0003 goes into `WarningsNotAsErrors` next to MSG0005.
- Notification decorators: only if MediatR is registered in Autofac by hand (TAAK-196 leaves out Mediator's handler registrations) and every reference is a `RegisterGenericDecorator`; otherwise Manual with a reason.
- Generic request: `G` abstract + per closed type `GOfXAndY : G<X, Y>` (ctors passed through), all closed references `G<X, Y>` → `GOfXAndY`, concrete handlers via an abstract generic base get `IRequestHandler<GOfXAndY, Y>` + a `Handle(GOfXAndY, ct) => base.Handle(...)` bridge.
- A generic message that is only the base of concrete subclasses (`DomainNotificationBase<T>`): only `abstract`.

### Improvement

kgrzybek: 32 generic Manuals gone (only what TAAK-196 covers remains); eShop: `IdentifiedCommand` migrates; the silent notification-decorator hang becomes visible.

## Choices

| Choice | Reason |
| ------ | ------ |
| The decorator stays a container decorator, not → `IPipelineBehavior` | Mediator resolves the handler via DI (probe), so Autofac applies the decorators exactly as with MediatR (order: last registered = outermost, probe p2). A behavior would reverse the order relative to existing behaviors and would have to imitate Autofac's service semantics (a decorator on `ICommandHandler<>` also works when resolving `IRequestHandler<>`). |
| Decorator = an open generic class whose request is its own type parameter and that has a ctor parameter of a handler interface over that same parameter | An open generic handler **without** a decorated parameter can be the only handler of a message; Mediator does not find it (MSG0003) → stays Manual. |
| MSG0003 in `WarningsNotAsErrors` | The generator only skips the class; under TWAE the build would break while the behaviour is the same (same reasoning as MSG0005, TAAK-136). |
| Notification decorator only Fixed with Autofac registration of MediatR | With `AddMediator` the generator registers it as a handler of all notifications (recursion); TAAK-196 leaves those registrations out. |
| Generic request: subclass per closed type, references renamed | Mediator dispatches requests exactly by type; validators/DI keys (`IValidator<G<X, Y>>`) must come along, otherwise validation silently disappears. |
| Name `{G}Of{X}And{Y}` (special types with their CLR name: `Boolean`) | Predictable, equal to TAAK-192 (`{G}Of{X}`); clash → Manual. |
| `FixedResult.Warning` at the declaration | `message.GetType()` is now the subclass (eShop logs `GetGenericTypeName()`): the log text changes, behaviour does not. |

## Steps

- [x] 1. Probes (p1-p3) and choices
- [x] 2. `UnsupportedPattern` + `MediatRFix` (MSG0003), `GenericRequestPattern`
- [x] 3. Proofs (separate + `_Combined` kgrzybek/eShop) and tests
- [x] 4. Docs plan/models/verify, README row, INDEX, proofs-doc
- [x] 5. Build + tests
- [x] 6. Wrap up, archive, queue

## Risks & edge cases

| Risk | Mitigation |
| ---- | ---------- |
| MediatR 12.3+ `RegisterGenericHandlers = true` registers open generic handlers itself | The decorator criterion requires a decorated parameter; such a handler is not used by MediatR as the only handler. |
| Closed reference `G<X>` in an override signature | Manual (the override would break). |
| Closed type not visible from `G`'s project | Manual. |

## Changed files

- `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs`, `GenericRequestPattern.cs` (new), `UnsupportedPattern.cs`, `MediatRFix.cs` (MSG0003)
- Proofs `Proofs/MediatR/GenericDecorator/`, `GenericRequest/`, `_Combined/generic-decorators-kgrzybek`, `_Combined/identified-command-eshop`
- `src/Unshackle.Tests/Core/GenericDecoratorPatternTests.cs`, `GenericRequestPatternTests.cs`
- Docs `docs/projectinfo/patterns/mediatr-generic-{decorator,request}-{plan,modellen,verify}.md`, README rows

## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/archief/TAAK-192-mediatr-generieke-notification.md`
- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
