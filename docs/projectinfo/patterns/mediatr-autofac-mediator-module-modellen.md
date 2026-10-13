# Modellen: mediatr.autofac-mediator-module

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/AutofacMediatorModulePattern.cs`. Diagrammen ontbreken (d2/kingmadoc niet beschikbaar); tabellen vervangen ze.

## Klassen

| Element | Rol |
| ------- | --- |
| `AutofacMediatorModulePattern` | `IPattern`; vindt de methode-body met de scan, herschrijft het hele blok |
| `ProjectPackages.TypeofAssembly` | `"typeof().Assembly"`: telt in `ForProjectAsync` als aanroep, zodat `AddedPackage.OnlyWithCallTo` de generator plaatst |
| `InternalHandlerAccessPattern.HostsAsync` | telt een project met deze module dat de handler-assembly refereert als host |
| `AddMediatRPattern.Types` | gedeeld: alle typen van een assembly (processor-zoektocht) |

## Sequence

| Stap | Methode | Wat gebeurt er |
| ---- | ------- | -------------- |
| Vinden | `Find` → `ScanOfMediatR` | statement in een blok; keten terug tot `RegisterAssemblyTypes(typeof(T)[.GetTypeInfo()].Assembly)`, `T` uit MediatR, methode uit `Autofac*` |
| Keten | `Lifetime` | `AsImplementedInterfaces` verplicht, hoogstens één lifetime-aanroep |
| Checks | `GetTypeByMetadataName` | `AutofacRegistration` en `ServiceCollection` aanwezig |
| Behaviors | `RegisteredPipelineType`, `Processors` | pre/post zonder processors → weg; anders Manual |
| Lijsten | `IsListElement`, `RemoveElements` | `typeof(MediatR.Pipeline.*)` uit initializers, scheidingstekens kloppend |
| Rest | `IsPipelineType` | nog een `MediatR.Pipeline`-type in het blok → Manual |
| Schrijven | `Rewrite` | scan → drie statements (+ commentaar), vrije namen via `LookupSymbols`; usings `System.Linq`, `Microsoft.Extensions.DependencyInjection`, `Autofac.Extensions.DependencyInjection` |

## Beslisboom

| Blad | Voorwaarde | Bewijs |
| ---- | ---------- | ------ |
| niet gevonden | geen Autofac-`RegisterAssemblyTypes` over MediatR's assembly | geen finding |
| Manual | twee scans, andere ketenaanroep, geen Autofac.Extensions.DependencyInjection, processors, exception-behavior, ander Pipeline-gebruik | `scan-lifetime`, `with-processor` |
| Fixed | anders | `kgrzybek-module`, `_Combined/autofac-mediator-module` |

## Zie ook

- [mediatr-autofac-mediator-module-plan.md](mediatr-autofac-mediator-module-plan.md)
- [mediatr-autofac-mediator-module-verify.md](mediatr-autofac-mediator-module-verify.md)
- [mediatr-internal-handler-access-modellen.md](mediatr-internal-handler-access-modellen.md)
