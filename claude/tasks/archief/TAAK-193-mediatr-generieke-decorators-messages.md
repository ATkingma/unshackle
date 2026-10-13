# TAAK-193 — Generieke decorators, handlers en requests automatisch migreren (kgrzybek, eShop)

**Status**: klaar (corpus-herhaling kgrzybek/eShop open, zie `CORPUSRUN-mediatr.md`)
**Aangemaakt**: 2026-10-12
**Doel**: de 32 generieke `mediatr.unsupported`-Manuals van kgrzybek en `IdentifiedCommand<T, R>` van dotnet/eShop migreren zonder Manual, met hetzelfde runtimegedrag.

## Context voor de volgende agent

- Corpus: `claude/tasks/CORPUSRUN-mediatr.md` ("Herhaling na TAAK-180..184", terechte Manuals). kgrzybek `91c8ef2`, eShop `59f1a6c`.
- kgrzybek: per module zes decorators (`Logging/UnitOfWork/ValidationCommandHandler[WithResult]Decorator<T[, TResult]> : ICommandHandler<T[, TResult]>`,
  ctor-parameter `ICommandHandler<T> decorated`), plus `BuildingBlocks/.../UnitOfWorkCommandHandlerDecorator<T> : IRequestHandler<T>` (nergens geregistreerd)
  en `DomainNotificationBase<T> : IDomainEventNotification<T>` (alleen basis van concrete notifications). Registratie: Autofac
  `builder.RegisterGenericDecorator(typeof(D<>), typeof(ICommandHandler<>))` in `ProcessingModule`.
- eShop: `IdentifiedCommand<T, R> : IRequest<R>`, abstracte `IdentifiedCommandHandler<T, R> : IRequestHandler<IdentifiedCommand<T, R>, R>` met
  7 concrete subklassen (`CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>`), `new IdentifiedCommand<X, bool>(...)`
  in `OrdersApi`/integratie-handlers/tests, `IdentifiedCommandValidator : AbstractValidator<IdentifiedCommand<CreateOrderCommand, bool>>` + DI-registratie.
- Raakvlak: TAAK-192 (`GenericNotificationPattern`, notifications met gesloten gebruik), TAAK-196 (`AutofacMediatorModulePattern`: handlers blijven Autofac's, decorators blijven werken).
- Probes: `<probe-dir>/probe-193/p1` (Mediator 3.0.2), `p2` (MediatR 12.2 + Autofac 7.1), `p3` (open generieke decorator onder Mediator).

## Analyse

### Huidige situatie

`UnsupportedPattern` geeft Manual op elk niet-abstract generiek type dat `IRequest*`/`INotification`/`IRequestHandler*` implementeert; door `AllOrNothing` blijft kgrzybek en eShop heel MediatR.

### Probleem (gemeten, probe)

- Open generieke request-handler (decorator) `D<T> : IRequestHandler<T>`: generator geeft alleen **MSG0003** (warning, "handlers cannot be generic") en slaat hem over; onder `TreatWarningsAsErrors` (kgrzybek, eShop) een error.
- Mediator lost handlers en behaviors via DI op (`GetRequiredService<IRequestHandler<TReq, TRes>>`, `GetServices<INotificationHandler<T>>`): een container-decorator blijft dus werken.
- Open generieke **notification**-decorator `D<T> : INotificationHandler<T>`: de generator registreert hem als handler van élke notification → zelf-recursie (probe bleef hangen). Vandaag niet eens Manual.
- Generieke request `G<T, R> : IRequest<R>`: generator vindt alleen `G<T, R>` (MSG0005), geen dispatch van `G<X, bool>` (MissingMessageHandlerException). Een request-handler voor een basistype wordt níet gebruikt voor een afgeleid message (anders dan bij notifications).

### Nieuwe situatie

- Request-decorators blijven decorators (alleen Task → ValueTask door de bestaande handler-patterns); MSG0003 komt in `WarningsNotAsErrors` naast MSG0005.
- Notification-decorators: alleen als MediatR in Autofac met de hand geregistreerd is (TAAK-196 laat Mediator's handlerregistraties weg) en elke verwijzing een `RegisterGenericDecorator` is; anders Manual met reden.
- Generieke request: `G` abstract + per gesloten type `GOfXAndY : G<X, Y>` (ctors door), alle gesloten verwijzingen `G<X, Y>` → `GOfXAndY`, concrete handlers via een abstracte generieke basis krijgen `IRequestHandler<GOfXAndY, Y>` + een `Handle(GOfXAndY, ct) => base.Handle(...)`-brug.
- Generiek message dat alleen basis van concrete subklassen is (`DomainNotificationBase<T>`): alleen `abstract`.

### Verbetering

kgrzybek: 32 generieke Manuals weg (resteert alleen wat TAAK-196 dekt); eShop: `IdentifiedCommand` migreert; de stille notification-decorator-hang wordt zichtbaar.

## Keuzes

| Keuze | Reden |
| ----- | ----- |
| Decorator blijft container-decorator, niet → `IPipelineBehavior` | Mediator lost de handler via DI op (probe), dus Autofac past de decorators exact toe zoals bij MediatR (volgorde: laatst geregistreerd = buitenste, probe p2). Een behavior zou de volgorde omkeren t.o.v. bestaande behaviors en Autofac's service-semantiek (decorator op `ICommandHandler<>` werkt ook bij resolve van `IRequestHandler<>`) moeten nabootsen. |
| Decorator = open generieke klasse waarvan het request een eigen typeparameter is én een ctor-parameter van een handler-interface over diezelfde parameter heeft | Een open generieke handler zónder decorated-parameter kan de enige handler van een message zijn; die vindt Mediator niet (MSG0003) → blijft Manual. |
| MSG0003 in `WarningsNotAsErrors` | Generator slaat de klasse alleen over; onder TWAE zou de build breken terwijl het gedrag gelijk is (zelfde redenering als MSG0005, TAAK-136). |
| Notification-decorator alleen Fixed bij Autofac-registratie van MediatR | Met `AddMediator` registreert de generator hem als handler van alle notifications (recursie); TAAK-196 laat die registraties weg. |
| Generieke request: subklasse per gesloten type, verwijzingen hernoemd | Mediator dispatcht requests exact op type; validators/DI-sleutels (`IValidator<G<X, Y>>`) moeten mee, anders valt validatie stil weg. |
| Naam `{G}Of{X}And{Y}` (speciale typen met CLR-naam: `Boolean`) | Voorspelbaar, gelijk aan TAAK-192 (`{G}Of{X}`); botsing → Manual. |
| `FixedResult.Warning` bij de declaratie | `message.GetType()` is nu de subklasse (eShop logt `GetGenericTypeName()`): logtekst verandert, gedrag niet. |

## Stappen

- [x] 1. Probes (p1-p3) en keuzes
- [x] 2. `UnsupportedPattern` + `MediatRFix` (MSG0003), `GenericRequestPattern`
- [x] 3. Proofs (los + `_Combined` kgrzybek/eShop) en tests
- [x] 4. Docs plan/modellen/verify, README-rij, INDEX, proofs-doc
- [x] 5. Build + tests
- [x] 6. Afronden, archief, wachtrij

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| MediatR 12.3+ `RegisterGenericHandlers = true` registreert open generieke handlers zelf | Decorator-criterium vereist een decorated-parameter; zo'n handler wordt door MediatR niet als enige handler gebruikt. |
| Gesloten verwijzing `G<X>` in een override-signatuur | Manual (override zou breken). |
| Gesloten type niet zichtbaar vanuit het project van `G` | Manual. |

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs`, `GenericRequestPattern.cs` (nieuw), `UnsupportedPattern.cs`, `MediatRFix.cs` (MSG0003)
- Proofs `Proofs/MediatR/GenericDecorator/`, `GenericRequest/`, `_Combined/generic-decorators-kgrzybek`, `_Combined/identified-command-eshop`
- `src/Unshackle.Tests/Core/GenericDecoratorPatternTests.cs`, `GenericRequestPatternTests.cs`
- Docs `docs/projectinfo/patterns/mediatr-generic-{decorator,request}-{plan,modellen,verify}.md`, README-rijen

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/archief/TAAK-192-mediatr-generieke-notification.md`
- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
