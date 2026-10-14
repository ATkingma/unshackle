# Models: fa.should-be

x.Should().Be(y) to x.ShouldBe(y). Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs`.

**Figure 1.** Classes around `ShouldBePattern`: what it implements, uses and creates. Left out: `Fixer`, `IFix` and the usings handling (see [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-should-be-modellen-1.d2 -->
![Models: fa.should-be](img/fa-should-be-modellen-1.png)

| Element | Path | Role |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` and `Rewrite` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Base class (Template Method): `Rewrite` does scope, shape, result and arguments, then `RewriteAssertion` |
| `ShouldBePattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs` | This pattern, Id `fa.should-be`; also the base of `NotBePattern` (`ShouldName`); shared via `FluentAssertionsSyntax`: `ConvertExpected(expected, subjectType, model, position)` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Shared building blocks: `FindInvocations`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Outcome: `Fixed` with new node(s) and namespaces, or `Manual` with a reason |
| «FluentAssertions» recognised | - | `Be(expected[, because])`; `Be uit FluentAssertions.Xml / .Types` |
| «Shouldly» created | - | `ShouldBe(expected[, because])`; `cast naar het subjecttype` |

**Figure 2.** `Fixer` calls `Find` and `Rewrite` per node; `Rewrite` does the checks via `FluentAssertionsSyntax` and `SemanticModel` and returns `Fixed` or `Manual`. Left out: replacing the node and the usings by the `Fixer`.

<!-- kingmadoc:diagram img/fa-should-be-modellen-2.d2 -->
![Models: fa.should-be](img/fa-should-be-modellen-2.png)

| Step | Place | What happens |
| --- | --- | --- |
| Find | `ShouldBePattern.Find` | `FluentAssertionsSyntax.FindInvocations` looks for calls `Be` whose symbol lies in a FluentAssertions namespace |
| Checks | `FluentAssertionPattern.Rewrite` → `ShouldBePattern.RewriteAssertion` | `IsInsideAssertionScope(node, model)`; `GetSubject(assertion)`; `IsResultDiscarded(assertion, model)`; `TryGetArguments(assertion, model)`; then `GetSymbolInfo(assertion)`; `GetTypeInfo(subject)`; `ConvertExpected(expected, subjectType, ...)`; `ClassifyConversion(expected, subjectType)` |
| Manual | `PatternResult.Manual` | The first failing check gives a `ManualResult` with a reason (see figure 3) |
| Fixed | `PatternResult.Fixed` | New node; the `Fixer` handles trivia and usings |

**Figure 3.** Decision tree of `Rewrite`: 8 questions in the order of the code, every leaf names the proof in `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/` that proves it (`<name>.before.cs` with `.after.cs` or `.manual.txt`).

<!-- kingmadoc:diagram img/fa-should-be-modellen-3.d2 -->
![Models: fa.should-be](img/fa-should-be-modellen-3.png)

| Question or selector | If no: Manual reason | Proof |
| --- | --- | --- |
| Not inside an AssertionScope? | Inside an AssertionScope | `assertion-scope-nested`, `assertion-scope-using-block`, `assertion-scope-using-var` |
| Shape x.Should().Be(y)? | Be(...) is not in the form x.Should().Be(y) | no proof, guarded in code |
| Result discarded? | The result of Be(...) is used | `chain` |
| No named arguments; becauseArgs only with a constant, formattable `because`? | named arguments, or becauseArgs whose because is no constant format string | `edge-named-because`, `because-args-variable`, `because-args-bad-format` |
| Be not from FluentAssertions.Xml or .Types? | Be(...) from Xml/Types: different equality | `xml` |
| Exactly one value argument (no comparer)? | Be(...) with a comparer | `comparer` |
| Type of the subject known? | The type of the subject is unknown | no proof, guarded in code |
| ConvertExpected finds a safe form? | Expected type differs from subject type | `nullable-expected` |

| Condition | Fixed form | Proofs |
| --- | --- | --- |
| no | `x.ShouldBe(y[, because])`; `becauseArgs` become `string.Format(CultureInfo.InvariantCulture, because, args)` | `simple`, `because`, `lambda`, `because-args` |
| yes (constant only implicitly to the subject type) | `x.ShouldBe((byte)1)` | `constant-cast` |
| yes (only via an own implicit conversion, `Conversion.IsUserDefined`) | `x.ShouldBe((Doc)42)` | `implicit-conversion` |

## See also

- [fa-should-be-plan.md](fa-should-be-plan.md) - plan
- [fa-should-be-verify.md](fa-should-be-verify.md) - verification
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs` - the pattern
- `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/` - the proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generic decision of a pattern
- [../README.md](../README.md)
