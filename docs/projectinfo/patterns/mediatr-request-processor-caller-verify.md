# Verification: mediatr-request-processor-caller

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-request-processor-caller-plan.md`](mediatr-request-processor-caller-plan.md) |
| **Status**    | Verified (proofs); corpus evidence on jason not yet run |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/_Combined/request-processor-callers` (`new`, `var`, target-typed `new`, `IRequest<int>` and `IRequest`; `after` compiles against Mediator 3.0.2 with generator) |
| REQ-2 | Met | same, pre- and post-processor |
| REQ-3 | Met | `RequestProcessorCaller/result-used`, `through-interface` |
| REQ-4 | Met | `RequestProcessorCaller/unknown-response` |

## Deviations

- The jason corpus is not in this environment (`<corpus>/corpus-mediatr/jason` is missing); the `_Combined` proof replicates `RequestLoggerTests`.
- References outside an `AddMediatR` lambda that name a generic class with `typeof(P<>)` are Manual (open argument).
- `Process` calls across project boundaries follow from the solution's semantic models; there is no separate multi-project proof.

## See also

- [mediatr-request-processor-caller-plan.md](mediatr-request-processor-caller-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
