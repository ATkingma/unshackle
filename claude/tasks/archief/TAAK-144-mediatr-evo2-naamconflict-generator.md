# TAAK-144 — MediatR: evo2 bouwt niet na fix (CS0104, CS0122/CS0436, CS0535)

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/archief/TAAK-141-mediatr-assembly-parameter-discard.md` (evo2 na `_ = assembly;`)

## Analyse per foutklasse (evo2 = evolutionary-architecture hfst. 2, verse clone)

| Fout        | Oorzaak                                                                                                                                                                                                                                                                           | Veilig automatisch?                                                                            |
| ----------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| CS0104 (4)  | `ContractsModule.cs` heeft `using Application;` met een eigen `ICommand`/`ICommand<T>`; de nieuwe `using Mediator;` brengt `Mediator.ICommand`/`ICommand<T>` mee                                                                                                                  | **Ja**: verwijzingen naar het eigen type krijgen hun volledige naam (`global::Ns.ICommand<T>`) |
| CS0535 (4)  | Volgfout van CS0104 (signatuur met ambigu type)                                                                                                                                                                                                                                   | lost op met CS0104                                                                             |
| CS0436 (70) | `Mediator.SourceGenerator` in 5 projecten (Common.Infrastructure, Contracts.Infrastructure, Offers.Api, Passes.Api, Common.IntegrationTests); elk schrijft een publieke `Mediator.Mediator`/`MediatorOptions`/`AssemblyReference`, die botsen met die uit een gerefereerd project | **Nee**                                                                                        |
| CS0122 (12) | De generator in Passes.Api ziet de `internal` handlers van Contracts.Application (gerefereerd) en schrijft code die ze aanroept                                                                                                                                                   | **Nee**                                                                                        |

Waarom de generator niet "alleen in het juiste project" kan: evo2 registreert MediatR per module. `Common.Infrastructure.MediatorModule.AddMediator(services, assembly)` is een **publieke** methode die Offers en Passes met hun eigen assembly aanroepen; Contracts.Infrastructure scant `typeof(IContractsModule).Assembly` (Contracts.Application) met **internal** handlers. Mediator heeft één gegenereerde mediator per host, die alleen publieke handlers uit gerefereerde projecten kan aanroepen. Ook `MediatorOptions.Assemblies` / `GenerateTypesAsInternal` helpen niet: meerdere `AddMediator`-aanroepen in één container geven één `ISender` (de eerste `TryAdd`), die de handlers van de andere modules niet kent → fout bij run time. Dat is een ontwerpkeuze voor de gebruiker (handlers publiek maken en één `AddMediator` in de host), dus Manual.

## Gekozen

1. `AddMediatRPattern` → Manual (en door `AllOrNothing` blijft de solution onaangeroerd) als:
    - een weggevallen `RegisterServicesFrom...` een parameter krijgt van een methode die buiten de assembly zichtbaar is (de handlers zitten bij de aanroeper; proof `edge-assemblies-public`). Een `internal` methode zoals in evo1 houdt `_ = assembly;` (TAAK-141; proof `edge-assemblies` nu `internal`).
    - de gescande assembly (via `typeof(T)`, `<T>` of een lokale variabele daarmee) een ander project is met een niet-publieke handler (CS0122; `AddMediatRPatternTests`). Publieke handlers in een ander project (eShopOnWeb, ardalis) blijven Fixed.
2. `UsingDirectives.UpdateAsync`: komt een toegevoegde namespace binnen, dan krijgt elke verwijzing naar een eigen type met dezelfde naam en ariteit als een type in die namespace de volledige naam. De typen staan in `IFix.AddedTypes` (Mediator 3.0.2 + de drie gegenereerde; `SwappedTypesTests` vergelijkt met de echte assembly). Proof `RequestHandler/edge-own-command-name`.

## Stappen

- [x] Unittests `AddMediatRPatternTests` (publieke methode, internal/public handler in ander project), `SwappedTypesTests.AddedTypes_...`
- [x] Proofs `AddMediatR/edge-assemblies-public` (manual), `AddMediatR/edge-assemblies` (internal), `RequestHandler/edge-own-command-name`
- [x] Pattern-docs `mediatr-add-mediatr-plan.md` / `-verify.md`
- [x] Bewijs met `claude/tools/corpus-regression.sh` op verse clone (`/mnt/dikkeschijf/unshackle-corpus/src/evo`, uitvoer `/mnt/dikkeschijf/unshackle-corpus/t144/out/`):
      evo2 status `ok`: 2 Manual-redenen (publieke `MediatorModule.AddMediator`, internal `PrepareContractCommandHandler`), solution ongewijzigd, build groen, 39/39 → 39/39.
      evo1 status `ok`: 8 Fixed, build groen, 45/45 → 45/45, 0 regressies (flaky: tijdsafhankelijke validator-testnamen, ook vóór de fix).

## Niet gedaan

- Een `global using Mediator` (via `SwapGlobalUsings`) wordt niet op naamconflicten gecontroleerd (shortcut in code).
- `OnlyWithCallTo = ["AddMediator", ...]` matcht ook een eigen methode met die naam (evo2's `MediatorModule.AddMediator`), waardoor Offers/Passes de generator kregen. Met de Manual hierboven onschadelijk voor evo2; los op te pakken als het elders opduikt.

## Zie ook

- `claude/tasks/archief/TAAK-141-mediatr-assembly-parameter-discard.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
