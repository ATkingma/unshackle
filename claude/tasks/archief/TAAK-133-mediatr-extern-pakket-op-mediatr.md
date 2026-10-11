# TAAK-133 — MediatR: pakketten die zelf op MediatR bouwen (Ardalis.SharedKernel) worden niet gezien

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` (ardalis CleanArchitecture 9.1)

## Probleem

`Ardalis.SharedKernel` 1.4.0 hangt af van MediatR 12.1.1 en levert `DomainEventBase : INotification`,
`ICommand<T>`/`IQuery<T>`/`ICommandHandler`, `MediatRDomainEventDispatcher`. De fix meldde 15 Fixed / 0 Manual,
schreef 23 bestanden, en de build breekt: CS0311 `ContributorDeletedEvent` (erft `DomainEventBase`) is geen
`Mediator.INotification`. Alles-of-niets beloofde dat juist niet. MediatR blijft daarbij transitief in de graaf.

## Wat, waar, hoe

- **Waar**: `MediatRFix` (vóór de patterns) of een nieuwe check in `UnsupportedPattern`.
- **Hoe**: typen in eigen code waarvan een basistype/interface uit een **andere assembly dan MediatR zelf** MediatR
  implementeert (`AllInterfaces` met `ContainingAssembly` ≠ MediatR/MediatR.Contracts en ≠ eigen compilatie) → Manual
  "<pakket> depends on MediatR; replace it or migrate it first". Eventueel ook: een PackageReference met MediatR als
  dependency melden.
- Proof kan niet (vereist tweede assembly): unittest met een `MetadataReference` uit een tweede compilatie.

## Stappen

- [x] Rode test met externe basisklasse
- [x] Detectie + Manual
- [x] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
