# Verification: mediatr-autofac-mediator-module

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-autofac-mediator-module-plan.md`](mediatr-autofac-mediator-module-plan.md) |
| **Status**    | Verified (proofs, unittests, probe kgrzybek) |
| **Generated** | 2026-10-13 (met de hand) |

## Requirements

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `Proofs/MediatR/AutofacMediatorModule/kgrzybek-module` (after compileert tegen Mediator 3.0.2 + Autofac.Extensions.DependencyInjection) |
| REQ-2 | Gehaald | `AutofacMediatorModule/scan-lifetime` (onbekende keten → Manual); lifetimes in `kgrzybek-module` |
| REQ-3 | Gehaald | `AutofacMediatorModule/with-processor` (Manual); `MediatR.Pipeline`-typen weg in `kgrzybek-module` |
| REQ-4 | Gehaald | `_Combined/autofac-mediator-module` (Application + Infrastructure, `InternalsVisibleTo`) |
| REQ-5 | Gehaald | `SwappedTypesTests.FixAsync_AutofacScanOfMediatRAssembly_AddsSourceGenerator` |
| REQ-6 | Gehaald | Manual-tak in `AutofacMediatorModulePattern` |
| REQ-7 | Gehaald | `_Combined/host-references-host` (after compileert per project met generator, ook zonder CS0436-waarschuwing); `MediatorHostsTests` (`ReferencedHost_GeneratesTypesAsInternal`, `ReferencingHost_ListsAssembliesWithoutOtherHost`); kgrzybek r6 bouwt (TAAK-213) |
| REQ-8 | Gehaald | `MediatorHostsTests.SettingsAsync_HostGrantsInternalsVisibleToOtherHost_IsManual` |
| REQ-9 | Gehaald | gedeelde helper getest in `AddMediatRPatternTests.RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace`; scan kgrzybek: 4× Fixed met waarschuwing (TAAK-240) |

## Deviations

- Gedragsgelijkheid gemeten in de probe van TAAK-196 (`<probe-dir>/probe-196`, opgeruimd): notification 1×, request met internal ctor, contravariante handler, `TaskWhenAllPublisher`.
- REQ-6 heeft geen eigen proof; de reden staat in de code en is in de probe gezien.

## Zie ook

- [mediatr-autofac-mediator-module-plan.md](mediatr-autofac-mediator-module-plan.md)
- `claude/tasks/archief/TAAK-196-mediatr-autofac-mediator-module.md`
