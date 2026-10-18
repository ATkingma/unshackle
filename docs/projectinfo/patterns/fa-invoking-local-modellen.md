# Models: fa.invoking-local

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingLocalPattern.cs`. No rendered diagram; the tables carry the same facts.

## Figure 1: classes

| Element | Path | Role |
| ------- | ---- | ---- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` and `Rewrite` |
| `InvokingLocalPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingLocalPattern.cs` | This pattern, Id `fa.invoking-local` |
| `ThrowSyntax.TryGetAction` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowSyntax.cs` | Builds `() => body[s := x]` and refuses unsafe receivers; shared with `fa.invoking` |

## Figure 2: sequence

| Step | Location | What happens |
| ---- | -------- | ------------ |
| Find | `Find` | local declarations with one declarator initialized by a FluentAssertions `Invoking`/`Awaiting` extension call |
| Rewrite | `Rewrite` | `TryGetAction` gives the lambda; `var` is replaced by the declared type of the local (Action, Func<R>, Func<Task>) |
| Next | `fa.throw`, `fa.not-throw`, ... | `act.Should().Throw<T>()` on the delegate local is rewritten |

## Figure 3: decision tree

| Question | If no |
| -------- | ----- |
| Declaration has one declarator with an `Invoking`/`Awaiting` initializer from FluentAssertions? | not found |
| Receiver is a stable reference (local, parameter, field, this) of a reference type? | Manual (reason of `fa.invoking`) |
| Argument is a one-parameter lambda, method group or delegate variable? | Manual |

## See also

- [fa-invoking-local-plan.md](fa-invoking-local-plan.md)
- [fa-invoking-local-verify.md](fa-invoking-local-verify.md)
- `src/Unshackle.Tests/Proofs/FluentAssertions/InvokingLocal/`
