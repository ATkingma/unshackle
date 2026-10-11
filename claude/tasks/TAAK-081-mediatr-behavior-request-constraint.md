# TAAK-081 — mediatr.pipeline-behavior: overbodige `IMessage` naast `IRequest<TResponse>`

**Status**: open
**Aangemaakt**: 2026-10-11
**Gevonden in**: corpus-onderzoek (`docs/projectinfo/architectuur/corpus-mediatr-automapper.md`)

## Probleem

`where TRequest : IRequest<TResponse>` (eShop, ardalis) wordt `where TRequest : IRequest<TResponse>, IMessage`.
Compileert, maar is ruis: Mediator's `IRequest<T>` erft al van `IMessage`.

## Wat, waar, hoe

- **Waar**: constraint-logica in het patroon `mediatr.pipeline-behavior` (`src/Unshackle.Core/Fixes/MediatR/Patterns/`).
- **Hoe**: geen `IMessage` toevoegen als een bestaande constraint na de swap al `IMessage` implementeert (`IRequest<>`, `ICommand<>`, `IQuery<>`, `INotification`).
- Let op: in Mediator geldt de behavior dan alleen voor `IRequest`-berichten (niet voor `ICommand`/`IQuery`); bij MediatR was dat alles. Overweeg een rapportregel.

## Stappen

- [ ] `src/Unshackle.Tests/Proofs/_Pending/MediatR/PipelineBehavior/edge-request-constraint.*` groen maken en terugzetten
- [ ] `dotnet build` + `dotnet test` groen

## Zie ook

- `claude/tasks/mediatr-queue.md` (rij 10)
