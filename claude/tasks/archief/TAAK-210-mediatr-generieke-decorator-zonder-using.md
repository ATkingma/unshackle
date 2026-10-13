# TAAK-210 — Generieke decorator in bestand zonder `using MediatR` valt alsnog in `mediatr.unsupported`

**Status**: ✅ voltooid (2026-10-13) — 0 Manual op kgrzybek; de build na de fix breekt nog op nieuwe oorzaken → TAAK-212
**Aangemaakt**: 2026-10-13 (uit corpus-ronde 3, `claude/tasks/CORPUSRUN-mediatr.md`)
**Doel**: kgrzybek `91c8ef2` migreert volledig: 0 Manual, build ok, tests 246/287 → 246/287.

## Context voor de volgende agent

- Corpus-ronde 3 (main `9189df4`): kgrzybek 0 Fixed, 270 Manual. Enige root-Manuals: 15× `mediatr.unsupported`
  ("Mediator's source generator does not support generic messages…") op de module-decorators
  `Modules/<Module>/Infrastructure/Configuration/Processing/{Logging,UnitOfWork,Validation}CommandHandlerDecorator<T>` (5 modules × 3).
- Op dezelfde klassen vindt `mediatr.generic-decorator` (TAAK-193) ze wél als decorator. Ook `mediatr.void-request-handler` vindt ze.
- De BuildingBlocks-decorator `UnitOfWorkCommandHandlerDecorator<T> : IRequestHandler<T>` (bestand mét `using MediatR`) krijgt géén unsupported.
- De module-decorators implementeren alleen de eigen marker `ICommandHandler<T> : IRequestHandler<T>` (ander project, `Application`)
  en hun bestand heeft **geen `using MediatR;`**.

## Reproductie (gemeten)

Twee projecten: `App` (MediatR 12.2.0; `ICommand : IRequest`, `ICommandHandler<in T> : IRequestHandler<T>`, handler) en `Infra`
(Autofac 7.1.0; `LogDecorator<T> : ICommandHandler<T>` met ctor `ICommandHandler<T>`, `RegisterGenericDecorator(typeof(LogDecorator<>), typeof(ICommandHandler<>))`).

| `Infra/D.cs`                         | Resultaat `fix --dry-run`                                   |
| ------------------------------------ | ----------------------------------------------------------- |
| met `using MediatR;`                 | alles Fixed (void-request-handler, generic-decorator)        |
| zonder (`typeof(MediatR.IRequestHandler<>)` volledig gekwalificeerd) | `mediatr.unsupported` Manual op `LogDecorator<T>` → alles Manual |

Een kgrzybek-uitsnede (alleen Payments Application + Infrastructure in een eigen `.slnx`) reproduceert het ook; daar hielp alleen
`using MediatR;` toevoegen aan één decorator níet — er speelt dus mogelijk nog iets mee (eerst de kleine repro oplossen, dan opnieuw meten).

## Vermoedelijke oorzaak

`UnsupportedPattern` draait na `VoidRequestHandlerPattern`/`GenericDecoratorPattern` op het al herschreven bestand.
`GenericDecoratorPattern.Decorated` heeft een terugval voor "al herschreven in dit bestand" die de ctor-parameter als
`IRequestHandler<T, Unit>` van MediatR verwacht; zonder `using MediatR` bindt de herschreven tekst (of de marker-route) niet,
`Decorated` geeft `null` en `UnsupportedPattern` ziet een generieke request-handler → Manual.

## Stappen

- [x] 1. Repro: `Proofs/MediatR/_Combined/generic-decorator-marker-without-using` (projecten `App`, `Handlers`, `Infra`; zonder fix → `mediatr.unsupported`, alles Manual) en unittests in `GenericDecoratorPatternTests`.
- [x] 2. Oorzaak bevestigd (zoals vermoed): `GenericDecoratorPattern.RewriteVoid` schrijft `IRequestHandler<T, Unit>`; zonder `using MediatR` bindt dat pas na `using Mediator` (einde van de run), dus is het voor `UnsupportedPattern` een error-type, `Decorated` geeft `null` → Manual.
- [x] 3. Fix: `GenericDecoratorPattern.IsRewrittenVoid` herkent ook het error-type `IRequestHandler<T, Unit>` (naam, ariteit 2, eerste argument de typeparameter, tweede `Unit`).
- [x] 4. Proof als `_Combined` (de bug zit tussen twee patronen; een losse proof draait alleen één pattern). Docs: generic-decorator plan/verify REQ-7, void-request-handler plan/verify REQ-6/7, `architectuur/core.md`.
- [~] 5. Bewijs (`_results/r4/kgr`): 217 Fixed, **0 Manual** (was 270 Manual). Build na de fix faalt nog → TAAK-212.

## Extra oorzaken (gevonden bij het bewijs)

De uitsnede-observatie ("`using MediatR` toevoegen hielp niet") kwam door alles-of-niets: alle 15 decorators moeten tegelijk goed gaan. Na de fix
kwamen in de corpusrun drie nieuwe oorzaken boven, alle drie opgelost:

1. **`Directory.Packages.props` zonder central package management** (kgrzybek: `<PackageReference Include="MediatR" Version="12.2.0" />` in een
   `ItemGroup Condition="...EndsWith('BuildingBlocks.Domain')"`, geen `<PackageVersion>`): `XmlProjectFileStore` behandelde het als CPM →
   versieloze `<PackageReference>` in de projecten + `<PackageVersion>` → `NU1015` bij restore. Nu: CPM alleen bij een `<PackageVersion>` of
   `ManagePackageVersionsCentrally=true`; anders hoort het bestand bij de keten (referentie in place gewisseld) en gaat een extra pakket (generator)
   naar het project zelf i.p.v. naast een geconditioneerde referentie. Unittest `EditAsync_PackagesPropsWithoutCentralManagement_…`.
2. **`Unit`/`ValueTask<Unit>` zonder `using Mediator`**: `VoidRequestHandlerPattern` eiste geen namespace; handlers in een bestand zonder `using MediatR`
   (alleen de eigen marker) kregen `ValueTask<Unit>` zonder `using Mediator` → 92× CS0246/CS0738. Nu `Fixed(..., "Mediator", "System.Threading.Tasks")`.
3. **SA1513** (StyleCop, TWAE): `return Unit.Value;` direct na een `}`. Nu een lege regel ervoor (proof `VoidRequestHandler/block-end`).

Restant (TAAK-212): `MSG0001` "multiple handlers" (44 handlertypen), `SA1210` using-volgorde (Fixer/`UsingDirectives`), `CS1503` in
`Validation…WithResultDecorator` (`ValueTask<TResult>` naar een `Task<TResult>`-parameter), `CS0122` internal domain events in `Mediator.g.cs`.

## Zie ook

- `claude/tasks/archief/TAAK-193-mediatr-generieke-decorators-messages.md` — `mediatr.generic-decorator`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 3"
- `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`, `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericDecoratorPattern.cs`
- `claude/tasks/mediatr-queue.md`
