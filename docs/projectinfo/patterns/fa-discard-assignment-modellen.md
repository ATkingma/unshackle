# Models: fa.discard-assignment

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DiscardAssignmentPattern.cs`. No rendered diagram: `d2` was not available when this was written; the tables carry the same facts.

## Figure 1: classes

| Element | Path | Role |
| ------- | ---- | ---- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` and `Rewrite` |
| `DiscardAssignmentPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/DiscardAssignmentPattern.cs` | This pattern, Id `fa.discard-assignment`; implements `IPattern` directly |
| `FluentAssertionsSyntax.IsFluentAssertions` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Checks that the called method belongs to FluentAssertions |
| `PatternResult`, `FixedResult` | `src/Unshackle.Core/Models/` | Outcome: always `Fixed`, never `Manual` |

## Figure 2: sequence

| Step | Location | What happens |
| ---- | -------- | ------------ |
| Find | `Find` | expression statements `_ = rhs;` with an `IDiscardSymbol` on the left and an FA invocation (optionally awaited) on the right |
| Rewrite | `Rewrite` | the statement keeps only the right side; the Fixer copies the statement trivia |
| Next | other `fa.*` patterns | they see `x.Should().M();` as a discarded result and rewrite it |

## Figure 3: decision tree

| Question | If no |
| -------- | ----- |
| Statement is a simple assignment? | not found |
| Left side is a discard symbol (not a local `_`)? | not found |
| Right side is an invocation or an awaited invocation of FluentAssertions? | not found |

## See also

- [fa-discard-assignment-plan.md](fa-discard-assignment-plan.md)
- [fa-discard-assignment-verify.md](fa-discard-assignment-verify.md)
- `src/Unshackle.Tests/Proofs/FluentAssertions/DiscardAssignment/`
