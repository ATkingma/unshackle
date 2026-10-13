# Verification: mediatr-generic-request

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-generic-request-plan.md`](mediatr-generic-request-plan.md) |
| **Status**    | Verified (proofs, unittests); corpusrun eShop nog niet herhaald |
| **Generated** | 2026-10-13 (met de hand) |

## Requirements

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `Proofs/MediatR/GenericRequest/closed-uses`, `_Combined/identified-command-eshop` (after compileert tegen Mediator 3.0.2 mét generator) |
| REQ-2 | Gehaald | `GenericRequest/closed-uses` |
| REQ-3 | Gehaald | `_Combined/identified-command-eshop` (`CreateOrderIdentifiedCommandHandler`, `CancelOrderIdentifiedCommandHandler`) |
| REQ-4 | Gehaald | `GenericRequestPatternTests.RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass`, `RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual` |
| REQ-5 | Gehaald | `GenericRequestPatternTests.Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration`; `DomainNotificationBase<T>` in `_Combined/generic-decorators-kgrzybek` |
| REQ-6 | Gehaald | `GenericRequest/unbound-typeof`, `open-creation`, `not-plain-argument`, `derived-generic-record` |
| REQ-7 | Gehaald | `Warning` op de Fixed-declaratie (`GenericRequestPattern.cs`) |

## Deviations

- `derived-generic-record` was eerst een `mediatr.unsupported`-proof (`Unsupported/derived-generic-message`); het generieke record valt nu onder dit patroon en blijft Manual.
- Corpusbewijs (eShop `59f1a6c` bouwt en test na de fix) staat nog open; zie `claude/tasks/CORPUSRUN-mediatr.md`.

## Zie ook

- [mediatr-generic-request-plan.md](mediatr-generic-request-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
