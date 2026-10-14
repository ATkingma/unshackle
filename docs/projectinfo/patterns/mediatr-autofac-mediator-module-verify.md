# Verification: mediatr-autofac-mediator-module

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-autofac-mediator-module-plan.md`](mediatr-autofac-mediator-module-plan.md) |
| **Status**    | Verified (proofs, unittests, probe kgrzybek) |
| **Generated** | 2026-10-13 (by hand) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/AutofacMediatorModule/kgrzybek-module` (after compiles against Mediator 3.0.2 + Autofac.Extensions.DependencyInjection) |
| REQ-2 | Met | `AutofacMediatorModule/scan-lifetime` (onbekende keten → Manual); lifetimes in `kgrzybek-module` |
| REQ-3 | Met | `AutofacMediatorModule/with-processor` (Manual); `MediatR.Pipeline` types removed in `kgrzybek-module` |
| REQ-4 | Met | `_Combined/autofac-mediator-module` (Application + Infrastructure, `InternalsVisibleTo`) |
| REQ-5 | Met | `SwappedTypesTests.FixAsync_AutofacScanOfMediatRAssembly_AddsSourceGenerator` |
| REQ-6 | Met | Manual-tak in `AutofacMediatorModulePattern` |
| REQ-7 | Met | `_Combined/host-references-host` (after compiles per project with the generator, also without a CS0436 warning); `MediatorHostsTests` (`ReferencedHost_GeneratesTypesAsInternal`, `ReferencingHost_ListsAssembliesWithoutOtherHost`); kgrzybek r6 builds (TAAK-213) |
| REQ-8 | Met | `MediatorHostsTests.SettingsAsync_HostGrantsInternalsVisibleToOtherHost_IsManual` |
| REQ-9 | Met | shared helper tested in `AddMediatRPatternTests.RewriteAsync_SolutionWithArchitectureTests_WarnsAboutMediatorNamespace`; kgrzybek scan: 4× Fixed with a warning (TAAK-240) |

## Deviations

- Behavioural equality measured in the probe of TAAK-196 (`<probe-dir>/probe-196`, cleaned up): notification 1×, request with an internal ctor, contravariant handler, `TaskWhenAllPublisher`.
- REQ-6 has no own proof; the reason is in the code and was seen in the probe.

## See also

- [mediatr-autofac-mediator-module-plan.md](mediatr-autofac-mediator-module-plan.md)
- `claude/tasks/archief/TAAK-196-mediatr-autofac-mediator-module.md`
