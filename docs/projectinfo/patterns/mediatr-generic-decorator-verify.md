# Verification: mediatr-generic-decorator

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-generic-decorator-plan.md`](mediatr-generic-decorator-plan.md) |
| **Status**    | Verified (proofs, unittests); corpusrun kgrzybek nog niet herhaald |
| **Generated** | 2026-10-13 (met de hand) |

## Requirements

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `Proofs/MediatR/_Combined/generic-decorators-kgrzybek` (after compileert tegen Mediator 3.0.2 + Autofac) |
| REQ-2 | Gehaald | `GenericDecoratorPatternTests.Find_DecoratorWithResponse_LeavesItToTheHandlerPatterns` |
| REQ-3 | Gehaald | `MediatRFix`: `WarningsNotAsErrors = ["MSG0001", "MSG0003", "MSG0005"]` (MSG0001 sinds TAAK-212: valse melding van Mediator 3.0.2 voor een void-handler achter een eigen marker in een ander project; `MediatRFixPackageTests`) |
| REQ-4 | Gehaald | `GenericDecorator/used-elsewhere` |
| REQ-5 | Gehaald | `GenericDecorator/decorated-members`, `decorated-other-use` |
| REQ-6 | Gehaald | `GenericDecorator/notification-without-autofac`; Fixed-tak in `_Combined/generic-decorators-kgrzybek` |
| REQ-7 | Gehaald | `_Combined/generic-decorator-marker-without-using` (twee projecten, decorator-bestand zonder `using MediatR`; zonder de fix: `mediatr.unsupported` → alles Manual); `GenericDecoratorPatternTests.IsDecorator_RewrittenVoidDecoratorInFileWithoutUsingMediatR_True`, `..._UnboundHandlerWithOtherResponse_False`; corpus kgrzybek ronde 4 |

## Deviations

- Geen eigen `after`-proof in `GenericDecorator/`: de Fixed-takken hebben de hele solution nodig (registratie, Autofac-module) en staan in `_Combined`.
- Corpusbewijs (kgrzybek `91c8ef2` bouwt en test na de fix) staat nog open; zie `claude/tasks/CORPUSRUN-mediatr.md`.

## Zie ook

- [mediatr-generic-decorator-plan.md](mediatr-generic-decorator-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
