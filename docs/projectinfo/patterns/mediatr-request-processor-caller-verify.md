# Verification: mediatr-request-processor-caller

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-request-processor-caller-plan.md`](mediatr-request-processor-caller-plan.md) |
| **Status**    | Verified (proofs); corpusbewijs op jason nog niet gedraaid |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

## Requirements

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `Proofs/MediatR/_Combined/request-processor-callers` (`new`, `var`, doeltype-`new`, `IRequest<int>` en `IRequest`; `after` compileert tegen Mediator 3.0.2 mét generator) |
| REQ-2 | Gehaald | idem, pre- en post-processor |
| REQ-3 | Gehaald | `RequestProcessorCaller/result-used`, `through-interface` |
| REQ-4 | Gehaald | `RequestProcessorCaller/unknown-response` |

## Deviations

- Het jason-corpus staat niet in deze omgeving (`<corpus>/corpus-mediatr/jason` ontbreekt); de `_Combined`-proof bootst `RequestLoggerTests` na.
- Verwijzingen buiten een `AddMediatR`-lambda die een generieke klasse met `typeof(P<>)` noemen zijn Manual (open argument).
- `Process`-aanroepen over projectgrenzen volgen uit de semantische modellen van de solution; er is geen aparte meerprojectproof.

## Zie ook

- [mediatr-request-processor-caller-plan.md](mediatr-request-processor-caller-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
