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
| Holder | `Holder` | Field or local variable: declared type, assigned or initialized with `Substitute.For<...>()`, or the type argument of a Moq `Mock<...>` |
| Moq usage | `MoqUse` | `.Object` as `next` argument (yields TReq), `Setup`/`Verify(x => x.Invoke())`, the chain after `Setup` |
| Real lambda | `RealLambda`, `ReturnedTask` | Parameters `(_, token)`; returned Tasks of a non-async body become ValueTasks |
| Usage | `Rewrite` (switch) | Per usage: assignment of `Substitute.For`, `next` argument of a behavior `Handle` (yields TReq), or `Invoke()` configuration |
| Rewrite | `ReplaceNodes` | Type, `Invoke` arguments, `Task.FromResult` in `Returns` |
| Safety net | `Fixer.MigrateProjectFilesAsync` | Remaining symbols in files without Manual: Manual `unmatched-use` on that line |

## Decision tree

| Leaf | Condition | Proof |
| ---- | --------- | ----- |
| Fixed | mock + `Handle` argument + `Invoke().Returns(x)` / `Received().Invoke()` | `_Combined/request-handler-delegate` |
| Manual "not a field or local" | parameter of a helper method, property, public field | `parameter` |
| Fixed (real lambda) | `RequestHandlerDelegate<int> next = _ => Task.FromResult(1)` → `(_, _) => ValueTask.FromResult(1)` | `_Combined/request-handler-delegate-lambda` |
| Fixed (Moq) | `Mock<RequestHandlerDelegate<T>>` + `Setup(x => x.Invoke()).ReturnsAsync(..)` + `.Object` as `next` | `_Combined/request-handler-delegate-moq` |
| Manual "not a lambda or a Substitute.For mock" | method group | `method-group` |
| Manual "uses its parameter _" | `_ => ... _.IsCancellationRequested ...` | `lambda-discard-parameter` |
| Manual "used other than as a mock configuration" | `await next()` or `next.Invoke()` outside `Returns` | `invoke-call` |
| Manual "is a Task or a callback" | `Returns(_ => ...)` or another Task (NSubstitute or Moq) | `callback-returns`, `moq-returns-value` |
| Manual "not handed to a behavior's Handle" | no `Handle` call to which the delegate is passed as `next` | `no-handle-call` |
| Fixed (shared field) | same field passed to `Handle` of multiple request types: type `MessageHandlerDelegate<IMessage, TRes>`, `Arg.Any<IMessage>()`, adapter lambda per `next` argument | `_Combined/request-handler-delegate-shared-field` |

## See also

- [mediatr-request-handler-delegate-plan.md](mediatr-request-handler-delegate-plan.md)
- [mediatr-request-handler-delegate-verify.md](mediatr-request-handler-delegate-verify.md)
- `src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/`
