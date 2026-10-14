# Models: mediatr.request-handler-delegate

Facts from `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs`, `src/Unshackle.Core/Fixer.cs` and `src/Unshackle.Core/ProjectPackages.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `RequestHandlerDelegatePattern` | Finds the type that names `RequestHandlerDelegate<T>` outside a behavior `Handle` and rewrites the type as a whole |
| `MediatRSyntax` | `IsMediatR`, `IsTaskMember`, `OnValueTask`, `CommaList` |
| `ProjectPackages.RemainingUsesAsync` | Safety net: remaining MediatR symbols per project |
| `Fixer.UnmatchedPatternId` | Pattern id `unmatched-use` of the safety-net rows |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `DelegateReferences`, `InnermostType` | Every `RequestHandlerDelegate<T>` name (not the parameter of a behavior `Handle`); the containing type is the finding |
| Holder | `Holder` | Field or local variable: declared type, or assigned or initialized with `Substitute.For<...>()` |
| Usage | `Rewrite` (switch) | Per usage: assignment of `Substitute.For`, `next` argument of a behavior `Handle` (yields TReq), or `Invoke()` configuration |
| Rewrite | `ReplaceNodes` | Type, `Invoke` arguments, `Task.FromResult` in `Returns` |
| Safety net | `Fixer.MigrateProjectFilesAsync` | Remaining symbols in files without Manual: Manual `unmatched-use` on that line |

## Decision tree

| Leaf | Condition | Proof |
| ---- | --------- | ----- |
| Fixed | mock + `Handle` argument + `Invoke().Returns(x)` / `Received().Invoke()` | `_Combined/request-handler-delegate` |
| Manual "not a field or local" | parameter of a helper method, property, public field | `parameter` |
| Manual "initialized with a real delegate" | `RequestHandlerDelegate<int> next = () => ...` | `lambda` |
| Manual "used other than as a mock configuration" | `await next()` or `next.Invoke()` outside `Returns` | `invoke-call` |
| Manual "is a Task or a callback" | `Returns(_ => ...)` or another Task | `callback-returns` |
| Manual "not handed to a behavior's Handle" | no `Handle` call to which the delegate is passed as `next` | `no-handle-call` |
| Fixed (shared field) | same field passed to `Handle` of multiple request types: type `MessageHandlerDelegate<IMessage, TRes>`, `Arg.Any<IMessage>()`, adapter lambda per `next` argument | `_Combined/request-handler-delegate-shared-field` |

## See also

- [mediatr-request-handler-delegate-plan.md](mediatr-request-handler-delegate-plan.md)
- [mediatr-request-handler-delegate-verify.md](mediatr-request-handler-delegate-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/`
