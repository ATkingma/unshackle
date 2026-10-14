# Verification: mediatr-generic-request

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-generic-request-plan.md`](mediatr-generic-request-plan.md) |
| **Status**    | Verified (proofs, unit tests); eShop corpus run not yet repeated |
| **Generated** | 2026-10-13 (by hand) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/GenericRequest/closed-uses`, `_Combined/identified-command-eshop` (after compiles against Mediator 3.0.2 with the generator) |
| REQ-2 | Met | `GenericRequest/closed-uses` |
| REQ-3 | Met | `_Combined/identified-command-eshop` (`CreateOrderIdentifiedCommandHandler`, `CancelOrderIdentifiedCommandHandler`) |
| REQ-4 | Met | `GenericRequestPatternTests.RewriteAsync_ClosedTypeOnlyUsedInOtherProject_WritesItsSubclass`, `RewriteAsync_ClosedTypeDeclaredInOtherProject_Manual` |
| REQ-5 | Met | `GenericRequestPatternTests.Find_OnlyBaseOfConcreteSubclass_FindsTheDeclaration`; `DomainNotificationBase<T>` in `_Combined/generic-decorators-kgrzybek` |
| REQ-6 | Met | `GenericRequest/unbound-typeof`, `open-creation`, `not-plain-argument`, `derived-generic-record` |
| REQ-7 | Met | `Warning` on the Fixed declaration (`GenericRequestPattern.cs`) |

## Deviations

- `derived-generic-record` was first a `mediatr.unsupported` proof (`Unsupported/derived-generic-message`); the generic record now falls under this pattern and stays Manual.
- Corpus proof (eShop `59f1a6c` builds and tests after the fix) is still open; see `claude/tasks/CORPUSRUN-mediatr.md`.

## See also

- [mediatr-generic-request-plan.md](mediatr-generic-request-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
