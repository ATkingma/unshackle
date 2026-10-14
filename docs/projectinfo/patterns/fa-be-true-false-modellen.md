# Models: fa.be-true-false

x.Should().BeTrue() to x.ShouldBeTrue(). Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs`.

**Figure 1.** Classes around `BeTrueFalsePattern`: what it implements, uses and creates. Omitted: `Fixer`, `IFix` and the usings handling (see [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-true-false-modellen-1.d2 -->
![Models: fa.be-true-false](img/fa-be-true-false-modellen-1.png)

| Element | Path | Role |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` and `Rewrite` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` does the shared checks and calls `RewriteAssertion(AssertionContext)` |
| `BeTrueFalsePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs` | This pattern, Id `fa.be-true-false` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Shared building blocks: `FindInvocations`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `Invoke` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Outcome: `Fixed` with new node(s) and namespaces, or `Manual` with a reason |
| «FluentAssertions» recognized | - | `BeTrue([because])`; `BeFalse([because])` |
| «Shouldly» created | - | `ShouldBeTrue([because])`; `ShouldBeFalse([because])`; `ShouldBe(true|false[, because]) voor bool?` |
| Recognized types | - | `bool`; `bool? (Nullable<bool>)` |

**Figure 2.** `Fixer` calls `Find` and, per node, `Rewrite`; `FluentAssertionPattern.Rewrite` does the shared checks and then calls `BeTrueFalsePattern.RewriteAssertion` (type check, `Fixed` or `Manual`). Omitted: replacing the node and the usings by the `Fixer`.

<!-- kingmadoc:diagram img/fa-be-true-false-modellen-2.d2 -->
![Models: fa.be-true-false](img/fa-be-true-false-modellen-2.png)

| Step | Location | What happens |
| --- | --- | --- |
| Find | `BeTrueFalsePattern.Find` | `FluentAssertionsSyntax.FindInvocations` looks for calls to `BeTrue`, `BeFalse` whose symbol lives in a FluentAssertions namespace |
| Checks | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope(node, model)`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)` |
| Rewrite | `BeTrueFalsePattern.RewriteAssertion` | `GetTypeInfo(subject)`: `bool` or `bool?`, then `Invoke(...)` |
| Manual | `PatternResult.Manual` | The first failing check yields `ManualResult` with a reason (see figure 3) |
| Fixed | `PatternResult.Fixed` | New node; trivia and usings are handled by the `Fixer` |

**Figure 3.** Decision tree of `FluentAssertionPattern.Rewrite` and `RewriteAssertion`: 5 questions in code order, each leaf names the proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/` that proves it (`<naam>.before.cs` with `.after.cs` or `.manual.txt`).

<!-- kingmadoc:diagram img/fa-be-true-false-modellen-3.d2 -->
![Models: fa.be-true-false](img/fa-be-true-false-modellen-3.png)

| Question or selector | If no: Manual reason | Proof |
| --- | --- | --- |
| Outside an AssertionScope? | Inside an AssertionScope (a scope that `fa.assertion-scope` does not convert) | - |
| Form x.Should().BeTrue()? | BeTrue()/BeFalse() is not in the form x.Should()... | no proof, guarded in code |
| Result discarded? | The result of BeTrue()/BeFalse() is used | `chain`, `value-used` |
| No named arguments; becauseArgs only with a constant `because`? | named arguments, or becauseArgs whose because is no constant format string | - (`because-args` is Fixed, TAAK-097) |
| Subject is bool or bool?? | The type of the subject is unknown or not bool/bool? | no proof, guarded in code |

| Condition | Fixed form | Proofs |
| --- | --- | --- |
| bool | `x.ShouldBeTrue([because]) / x.ShouldBeFalse([because])` | `be-true`, `be-false`, `because`, `because-args` (`string.Format(...)`), `edge-has-value`, `edge-interpolated-because`, `edge-linq-subject`, `edge-pattern-subject` |
| bool? | `x.ShouldBe(true|false[, because])` | `nullable` |

## See also

- [fa-be-true-false-plan.md](fa-be-true-false-plan.md) - plan
- [fa-be-true-false-verify.md](fa-be-true-false-verify.md) - verification
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs` - the pattern
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/` - the proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generic decision of a pattern
- [../README.md](../README.md)
