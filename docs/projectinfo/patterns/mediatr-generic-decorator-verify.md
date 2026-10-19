# Verification: mediatr-generic-decorator

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-generic-decorator-plan.md`](mediatr-generic-decorator-plan.md) |
| **Status**    | Verified (proofs, unit tests); kgrzybek corpus run not yet repeated |
| **Generated** | 2026-10-13 (by hand) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/_Combined/generic-decorators-kgrzybek` (after compiles against Mediator 3.0.2 + Autofac) |
| REQ-2 | Met | `GenericDecoratorPatternTests.Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns` |
| REQ-3 | Met | `MediatRFix`: `WarningsNotAsErrors = ["MSG0001", "MSG0003", "MSG0005"]` (MSG0001 since TAAK-212: a false report from Mediator 3.0.2 for a void handler behind an own marker in another project; `MediatRFixPackageTests`) |
| REQ-4 | Met | `GenericDecorator/used-elsewhere` |
| REQ-5 | Met | `GenericDecorator/decorated-members`, `decorated-other-use` |
| REQ-6 | Met | `GenericDecorator/notification-without-autofac`; Fixed-tak in `_Combined/generic-decorators-kgrzybek` |
| REQ-7 | Met | `_Combined/generic-decorator-marker-without-using` (two projects, decorator file without `using MediatR`; without the fix: `mediatr.unsupported` → everything Manual); `GenericDecoratorPatternTests.IsDecorator_RewrittenVoidDecoratorInFileWithoutUsingMediatR_True`, `..._UnboundHandlerWithOtherResponse_False`; corpus kgrzybek round 4 |
| REQ-8 | Met | `GenericDecorator/scrutor-decorate` (an earlier `typeof` use, then `Decorate(typeof(INotificationHandler<>), typeof(D<>))`); fullcart `scan` (TAAK-293): the reason names `Decorate(...)` at `DependencyInjection.cs:38` |
| REQ-9 | Met | `GenericDecoratorPatternTests.RewriteAsync_DecorateCall_NamesScrutorOnlyWhenItResolvesThereOrNotAtAll` (Scrutor namespace → named, own namespace → plain reason, unresolved → named); proof `scrutor-decorate` now resolves to a stand-in in `namespace Scrutor`; fullcart `scan` (TAAK-294, real Scrutor 4.2.2, whose `Decorate` is in `Microsoft.Extensions.DependencyInjection` of assembly `Scrutor`): reason unchanged |

## Deviations

- No own `after` proof in `GenericDecorator/`: the Fixed branches need the whole solution (registration, Autofac module) and are in `_Combined`.
- Corpus proof (kgrzybek `91c8ef2` builds and tests after the fix) is still open; see `claude/tasks/CORPUSRUN-mediatr.md`.

## See also

- [mediatr-generic-decorator-plan.md](mediatr-generic-decorator-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
