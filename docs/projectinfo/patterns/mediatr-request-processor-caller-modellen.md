# Models: mediatr.request-processor-caller

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorCallerPattern.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `RequestProcessorCallerPattern` | `IPattern`; finds processor references and `Process` calls and rewrites them |
| `MediatRSyntax` | `ResponseOf` (shared with `RequestProcessorPattern`), `DeclaredInterface`, `IsMediatR`, `CommaList` |
| `CallAnalysis` | Outcome of the analysis of one `Process`: problem, pre or post, response type |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` | `GenericName` of a pre-processor class with an open message (outside its own class and `AddMediatR`), `Process` name and associated `ArgumentList` |
| Reference | `RewriteReference` | `MediatRSyntax.ResponseOf` of the type argument, add second argument |
| Call | `Analyze` | Is it an `await x.Process(...);` statement with positional arguments? |
| Name | `Rewrite` (IdentifierName) | `Process` becomes `Handle` |
| Arguments | `RewriteArguments` | `next` delegate between request and token |

## Decision tree

| Leaf | Condition | Proof |
| ---- | --------- | ----- |
| Fixed | `P<Cmd>` with `Cmd : IRequest<T>` or `IRequest` | `_Combined/request-processor-callers` |
| Fixed | `await p.Process(...)` pre or post | `_Combined/request-processor-callers` |
| Manual "unknown" | type argument without `IRequest`, open argument | `unknown-response` |
| Manual "not a plain await statement" | result used, not awaited, mock, method group | `result-used` |
| Manual "through the MediatR interface" | receiver is `IRequestPreProcessor<T>`/`IRequestPostProcessor<T,R>` | `through-interface` |

## See also

- [mediatr-request-processor-caller-plan.md](mediatr-request-processor-caller-plan.md)
- [mediatr-request-processor-caller-verify.md](mediatr-request-processor-caller-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/RequestProcessorCaller/`
