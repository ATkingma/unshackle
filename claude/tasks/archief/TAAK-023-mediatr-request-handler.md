# TAAK-023 — mediatr.request-handler

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `IRequestHandler<TReq,TRes>` met `Task<TRes> Handle(...)` (MediatR 12.5.0) → `ValueTask<TRes> Handle(...)` (Mediator 3.0.2).

## Context voor de volgende agent

- Rij 1 in `claude/tasks/mediatr-queue.md`. Infrastructuur: `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`.
- Voorbeelden: `samples/SampleShop/SampleShop/Features/Orders/{ListOrders,PlaceOrder,GetOrder}Handler.cs`.
- Er is nog **geen** pattern-klasse of proof gebouwd; alleen onderzoek. Reden: zie **Blokkade**.

## Analyse

### API (gecontroleerd in de echte assembly `~/.nuget/packages/mediator.abstractions/3.0.2/lib/net8.0/Mediator.dll`)

- Mediator 3.0.2 bevat in namespace `Mediator`: `IRequestHandler\`1`, `IRequestHandler\`2`, `IRequest`, `IRequest\`1`, `IBaseRequest`, `Unit`; `Handle`geeft`ValueTask<TResponse>` terug.
- MediatR 12.5.0: `IRequestHandler<,>` in `MediatR.dll`, `IRequest<T>`/`Unit`/`INotification` in `MediatR.Contracts.dll`, beide namespace `MediatR` (`TestReferences.MediatR` laadt ze allebei).

### Beoogde herschrijving

| Geval                                                                        | Herschrijving                                               |
| ---------------------------------------------------------------------------- | ----------------------------------------------------------- |
| `async Task<T> Handle(...)`                                                  | alleen returntype → `ValueTask<T>`; `async` en body blijven |
| niet-async, elke `return Task.FromResult(x)`                                 | `ValueTask<T>` + `return ValueTask.FromResult(x)` (.NET 5+) |
| expression-bodied `=> Task.FromResult(x)`                                    | idem                                                        |
| niet-async met andere `return` (bv. `return _inner.Handle(...)`, `Task.Run`) | Manual: kan geen Task → ValueTask garanderen                |
| expliciete interface-implementatie / `Handle` niet gevonden                  | Manual                                                      |

Find: methoden `Handle` in klassen die `MediatR.IRequestHandler<,>` implementeren (semantisch via `SemanticModel`, niet op naam).

## Blokkade (opgelost door TAAK-024)

`UsingDirectives.UpdateAsync` (`src/Unshackle.Core/UsingDirectives.cs`) wisselt `using MediatR;` → `using Mediator;` alleen als
**niets** in het document nog aan `MediatR` bindt (`UsedNamespaces`, bepaald met de oude compilatie). Na deze herschrijving
bindt `IRequestHandler<A,B>` (en `IRequest<T>` in hetzelfde bestand) nog steeds aan `MediatR`, dus de using blijft staan
en `after` compileert nooit tegen Mediator. Hetzelfde geldt op projectniveau (`Fixer.cs:104`: "MediatR is still used in
this project" → pakket blijft). De aanname uit `mediatr-queue.md` ("de namespace-swap dekt gelijknamige interfaces") klopt
dus niet met de huidige code. Dit is het TAAK-020-risico "`using MediatR` verdwijnt pas als niets meer bindt".

Een pattern kan dit niet oplossen zonder contract- of Fixer-wijziging (verboden voor patroon-agents, `patroon-loop.md` stap 5).

### Voorstel infrastructuur (aparte taak, rij 0-achtig)

`IFix` krijgt een lijst gelijknamige typen die de swap dekt, bv. `IReadOnlyList<string> SwappedTypes`
(`IRequest`, `IRequest\`1`, `IRequestHandler\`1`, `IRequestHandler\`2`, `INotification`, `INotificationHandler\`1`,
`ISender`, `IPublisher`, `IMediator`, `Unit`; FA-fix: leeg). `UsingDirectives.UsedNamespaces`en de pakketcheck in`Fixer`tellen symbolen met die metadata-naam niet als "gebruik". Daarna is dit patroon klein: alleen`Handle` herschrijven.

### MediatR.Contracts / pakket verwijderen

- Typen uit `MediatR.Contracts` hebben namespace `MediatR`; de swap behandelt ze als `MediatR`-gebruik (zelfde blokkade).
- `PackageMigration.RemovedPackage = "MediatR"` haalt alleen dat pakket weg. Een project dat `MediatR.Contracts` los
  refereert (bv. een contracts-project) houdt die referentie: dan `IRequest` dubbel (MediatR én Mediator) → ambigu na de swap.
  Te noteren als Manual of als tweede te verwijderen pakket in de infrastructuurtaak.

## Keuzes

- Niet bouwen zonder werkende swap: een proof waarvan `after` niet compileert faalt in `ProofTests`; een pattern dat
  alleen `Task`→`ValueTask` doet en `using MediatR` laat staan levert gebruikers niet-compilerende code op.
- `ValueTask.FromResult(x)` boven `new(x)`: leesbaar en gelijk aan `Task.FromResult`, bestaat sinds .NET 5.

## Stappen

- [x] 1. Claim rij 1.
- [x] 2. Onderzoek API in echte assemblies, voorbeelden in samples.
- [x] 3. Blokkade opgelost door TAAK-024 (`MediatRFix.SwappedTypes`).
- [x] 4. Pattern `RequestHandlerPattern`, proofs (`async`, `from-result-expression`, `from-result-block`, `generic`, `token-name`; Manual: `other-return`, `explicit`), plan/modellen/verify, README-rij.

## Resultaat

- `ProofTests` geeft de fix-substitute nu `SwappedTypes` mee (anders blijft `using MediatR` staan); bij `*.manual.txt` leeg, zodat een manual-proof alleen toetst dat het pattern niets wijzigt (de Fixer swapt de using anders toch).
- Expliciete interface-implementatie → Manual (keuze: zeldzaam, en de interface-naam in de declaratie moet ook mee).
- Scan `dotnet run --project src/Unshackle.Interface -- scan samples/SampleShop/SampleShop.slnx --fix mediatr` (na `dotnet restore` van de sample; zonder restore: 0 findings): `mediatr.request-handler` 5 Fixed / 0 Manual (`CountQuery`, `RegisterCustomerCommand`, `GetOrderHandler`, `ListOrdersHandler`, `PlaceOrderHandler`); `project-file` 4 Fixed, 1 Manual (`SampleShop.csproj`: MediatR nog gebruikt door notification-handler/`AddMediatR`, rijen 3 en 5).
- Checks: `dotnet build` 0 warnings, `dotnet test` 318/318, sample-tests 38/38, hook groen.

## Zie ook

- `claude/tasks/mediatr-queue.md`
- `claude/tasks/archief/TAAK-020-mediatr-fix-infrastructuur.md`
- `src/Unshackle.Core/UsingDirectives.cs`
- `claude/docs/patroon-loop.md`
