# Models: fa.contain

x.Should().Contain(item) to x.ShouldContain(item). Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs`.

**Figure 1.** Classes around `ContainPattern`: what it implements, uses and creates. Left out: `Fixer`, `IFix` and the usings handling (see [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-contain-modellen-1.d2 -->

![Models: fa.contain](img/fa-contain-modellen-1.png)

| Element                                        | Path                                                                   | Role                                                                                                              |
| ---------------------------------------------- | ---------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `IPattern`                                     | `src/Unshackle.Core/Abstractions/IPattern.cs`                          | Contract: `Find` and `Rewrite`                                                                                     |
| `FluentAssertionPattern`                       | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs`  | Template Method: `Rewrite` does the shared checks and calls `RewriteBeforeArguments` and `RewriteAssertion` |
| `ContainPattern`                               | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs` | This pattern, Id `fa.contain`; privé: `IsGenericEnumerable(type)`                                                  |
| `FluentAssertionsSyntax`                       | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs`  | Shared building blocks: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`                      |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/`                                           | Outcome: `Fixed` with new node(s) and namespaces, or `Manual` with a reason                                         |
| «FluentAssertions» recognised                     | -                                                                      | `Contain(T item                                                                                                   | Expression predicate                                                        | string)`; `NotContain(...)` |
| «Shouldly» created                          | -                                                                      | `ShouldContain(item                                                                                               | predicate)`; `ShouldNotContain(...)`; `ShouldContain(text, Case.Sensitive)` |
| Recognised types                               | -                                                                      | `string`; `IEnumerable<T>`; `IDictionary / IReadOnlyDictionary (Manual)`                                          |

**Figure 2.** `Fixer` calls `Find` and `Rewrite` per node; `FluentAssertionPattern.Rewrite` does the shared checks and calls `ContainPattern.RewriteBeforeArguments` (dictionary) and `ContainPattern.RewriteAssertion`, which return `Fixed` or `Manual`. Left out: replacing the node and the usings by the `Fixer`.

<!-- kingmadoc:diagram img/fa-contain-modellen-2.d2 -->

![Models: fa.contain](img/fa-contain-modellen-2.png)

| Step         | Place                                   | What happens                                                                                                                                      |
| ------------ | --------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Find         | `ContainPattern.Find`                   | `FluentAssertionsSyntax.FindInvocations` looks for calls `Contain`, `NotContain` whose symbol lies in a FluentAssertions namespace       |
| Checks       | `FluentAssertionPattern.Rewrite`        | `IsInsideAssertionScope(node, model)`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; then `TryGetArguments(assertion, model)`    |
| Dictionary   | `ContainPattern.RewriteBeforeArguments` | `GetTypeInfo(subject)`: a dictionary gives Manual before the argument split                                                                       |
| Rewrite      | `ContainPattern.RewriteAssertion`       | `GetSymbolInfo(assertion)`; `Parameters[0].Type`; `GetTypeInfo(subject)`; `NotNull(subject)`                                                      |
| Manual       | `PatternResult.Manual`                  | The first failing check gives a `ManualResult` with a reason (see figure 3)                                                                            |
| Fixed        | `PatternResult.Fixed`                   | New node; the `Fixer` handles trivia and usings                                                                                                   |

**Figure 3.** Decision tree of `FluentAssertionPattern.Rewrite` and the hooks: 8 questions in the order of the code, every leaf names the proof in `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/` that proves it (`<name>.before.cs` with `.after.cs` or `.manual.txt`).

<!-- kingmadoc:diagram img/fa-contain-modellen-3.d2 -->

![Models: fa.contain](img/fa-contain-modellen-3.png)

| Question or selector                                                  | If no: Manual reason                                                                                  | Proof                                                                   |
| --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| Outside an AssertionScope?                                            | Inside an AssertionScope (a scope that `fa.assertion-scope` does not convert)                         | -                                                                       |
| Shape x.Should().Contain(...)?                                         | Contain(...) is not in the form x.Should()...                                                         | `and-chain`                                                             |
| Result discarded?                                                 | The result of Contain(...) is used                                                                    | `chain`, `value-used`                                                   |
| Subject is not a dictionary?                                          | Dictionary: Contain(...) takes key/value pairs; use ShouldContainKey/ShouldContainKeyAndValue by hand | `dictionary`                                                            |
| No named arguments; becauseArgs only with a constant `because`?       | named arguments, or becauseArgs whose because is no constant format string                            | `named-because`, `because-variable` (`because-args` is Fixed, TAAK-097) |
| One value argument (no occurrence constraint)?                        | an occurrence constraint: no Shouldly equivalent                                                      | `occurrence`                                                            |
| Parameter is an item or predicate (not several items)?                | checks several items at once                                                                          | `items`                                                                 |
| Subject is string of IEnumerable<T>?                                  | The subject is not a string or a generic IEnumerable<T>                                               | no proof, guarded in code                                             |

| Condition                 | Fixed form                                               | Proofs                                                                                                |
| ------------------------- | -------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| string                    | `x.ShouldContain(v, Shouldly.Case.Sensitive[, because])` | `string`, `edge-interpolated`, `nullable-string` (`x.ShouldNotBeNull().ShouldContain(...)`)           |
| IEnumerable<T>, item      | `x.ShouldContain(item[, because])`                       | `list`, `enumerable`, `lambda`, `because`, `because-args` (`string.Format(...)`), `edge-generic-list` |
| IEnumerable<T>, predicaat | `x.ShouldContain(predicate)`                             | `predicate`                                                                                           |
| NotContain                | `x.ShouldNotContain(...)`                                | `not-contain`                                                                                         |

## See also

- [fa-contain-plan.md](fa-contain-plan.md) - plan
- [fa-contain-verify.md](fa-contain-verify.md) - verification
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainPattern.cs` - the pattern
- `src/Unshackle.Tests/Proofs/FluentAssertions/Contain/` - the proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generic decision of a pattern
- [../README.md](../README.md)
