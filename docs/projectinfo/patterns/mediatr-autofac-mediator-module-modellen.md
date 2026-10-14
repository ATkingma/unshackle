# Models: mediatr.autofac-mediator-module

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/AutofacMediatorModulePattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `AutofacMediatorModulePattern` | `IPattern`; finds the method body with the scan, rewrites the whole block |
| `ProjectPackages.TypeofAssembly` | `"typeof().Assembly"`: counts as a call in `ForProjectAsync`, so that `AddedPackage.OnlyWithCallTo` places the generator |
| `InternalHandlerAccessPattern.HostsAsync` | counts a project with this module that references the handler assembly as a host |
| `AddMediatRPattern.Types` | shared: all types of an assembly (processor search) |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` → `ScanOfMediatR` | a statement in a block; the chain back to `RegisterAssemblyTypes(typeof(T)[.GetTypeInfo()].Assembly)`, `T` from MediatR, a method from `Autofac*` |
| Chain | `Lifetime` | `AsImplementedInterfaces` required, at most one lifetime call |
| Checks | `GetTypeByMetadataName` | `AutofacRegistration` and `ServiceCollection` present |
| Behaviors | `RegisteredPipelineType`, `Processors` | pre/post without processors → removed; otherwise Manual |
| Lists | `IsListElement`, `RemoveElements` | `typeof(MediatR.Pipeline.*)` out of initializers, separators kept correct |
| Rest | `IsPipelineType` | another `MediatR.Pipeline` type in the block → Manual |
| Write | `Rewrite` | scan → three statements (+ comment), free names via `LookupSymbols`; usings `System.Linq`, `Microsoft.Extensions.DependencyInjection`, `Autofac.Extensions.DependencyInjection` |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| not found | no Autofac `RegisterAssemblyTypes` over MediatR's assembly | no finding |
| Manual | two scans, another chain call, no Autofac.Extensions.DependencyInjection, processors, an exception behavior, other Pipeline usage | `scan-lifetime`, `with-processor` |
| Fixed | otherwise | `kgrzybek-module`, `_Combined/autofac-mediator-module` |

## See also

- [mediatr-autofac-mediator-module-plan.md](mediatr-autofac-mediator-module-plan.md)
- [mediatr-autofac-mediator-module-verify.md](mediatr-autofac-mediator-module-verify.md)
- [mediatr-internal-handler-access-modellen.md](mediatr-internal-handler-access-modellen.md)
