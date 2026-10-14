# Models: fa.throw

act.Should().Throw<T>() to Should.Throw<T>(act). Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs`.

**Figure 1.** Classes around `ThrowPattern`: what it implements, uses and creates. Omitted: `Fixer`, `IFix` and the usings handling (see [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-throw-modellen-1.d2 -->
![Models: fa.throw](img/fa-throw-modellen-1.png)

| Element | Path | Role |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` and `Rewrite` |
| `ThrowPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs` | This pattern, Id `fa.throw` |
| `ThrowSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowSyntax.cs` | `FindThrow` (node: Throw call, `WithMessage` call or `.Which`), `IsExactly`, `IsInvoking`, `TryGetAction` |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Shared building blocks: `FindInvocations`, `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `IsFluentAssertions`, `TryGetArguments` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Outcome: `Fixed` with new node(s) and namespaces, or `Manual` with a reason |
| «FluentAssertions» recognised | - | `Throw<T>()`; `ThrowExactly<T>()` (via `ThrowExactlyPattern`); `WithMessage(pattern)`; `.Which` |
| «Shouldly» created | - | `Should.Throw<T>(act[, because])`; `.ShouldBeOfType<T>()` for ThrowExactly; `thrown.Message.ShouldContain / ShouldStartWith / ShouldEndWith / ShouldBe` |
| Recognised types | - | `Action`; `Func<T> met referentietype`; `Invoking(...)` (via `InvokingPattern`) |
| `ThrowMessage` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowMessage.cs` | Shared with `ThrowAsyncPattern`: `Check` translates the `WithMessage` wildcard to `Message.ShouldX(...)`; `MayThrowAggregate`; `OfType` |

**Figure 2.** `Fixer` calls `Find` and `Rewrite` per node; `Rewrite` performs the checks via `FluentAssertionsSyntax` and `SemanticModel` and returns `Fixed` or `Manual`. Omitted: the replacement of the node and the usings by the `Fixer`.

<!-- kingmadoc:diagram img/fa-throw-modellen-2.d2 -->
![Models: fa.throw](img/fa-throw-modellen-2.png)

| Step | Place | What happens |
| --- | --- | --- |
| Find | `ThrowPattern.Find` | `ThrowSyntax.FindThrow` looks for FA calls `Throw` (ThrowExactly and Invoking subjects belong to `ThrowExactlyPattern`/`InvokingPattern`, which call this `Rewrite`); a directly following `WithMessage` or `.Which` becomes the node |
| Checks | `ThrowPattern.Rewrite` | `IsInsideAssertionScope`; `GetSubject(throwCall)`; `IsResultDiscarded(outer, model)` (not for `.Which`); `IsInvoking`/`TryGetAction` or `IsFluentAssertions(GetSymbolInfo(subject))`; `GetTypeInfo(subject)`; `MayThrowAggregate`; `TryGetArguments(throwCall, model)`; `Arguments([action], because)`; `OfType`; `GetConstantValue(pattern)`; `Check(result, pattern, because)` |
| Manual | `PatternResult.Manual` | The first failed check gives `ManualResult` with a reason (see figure 3) |
| Fixed | `PatternResult.Fixed` | New node; the `Fixer` takes care of trivia and usings |

**Figure 3.** Decision tree of `Rewrite`: 13 questions in code order, each leaf names the proof in `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/` that proves it (`<naam>.before.cs` with `.after.cs` or `.manual.txt`).

<!-- kingmadoc:diagram img/fa-throw-modellen-3.d2 -->
![Models: fa.throw](img/fa-throw-modellen-3.png)

| Question or selector | If no: Manual reason | Proof |
| --- | --- | --- |
| Outside an AssertionScope? | Inside an AssertionScope | `ThrowExactly/assertion-scope`, `Invoking/assertion-scope` |
| Exactly one type argument? | Throw without exactly one type argument | no proof, guarded in code |
| Form x.Should().Throw<T>()? | Throw<T>(...) is not in the form x.Should().Throw<T>() | no proof, guarded in code |
| Node is `.Which`, or result discarded? | The result of Throw<T>() is used (.Which/.And/.WithInnerException/.Where or a value) | `chain`, `edge-inner-exception` |
| `Invoking`: `TryGetAction` succeeds; otherwise subject not built with FluentAssertions? | Invoking receiver not stable, value type or no lambda; The subject is built with FluentAssertions | `Invoking/side-effect-receiver`, `value-type`, `method-group` |
| Subject is an Action or a Func returning a reference type? | The subject is not an Action or a Func returning a reference type | `value-func` |
| ThrowExactly, or no AggregateException risk (`MayThrowAggregate`)? | FluentAssertions unwraps AggregateException; Shouldly does not | `aggregate-wait`, `aggregate-method-group` |
| No named args on Throw; becauseArgs only with constant `because`? | named arguments, or becauseArgs whose because is no constant format string | no proof, guarded in code |
| No named args, one value on WithMessage? | WithMessage(...) with named arguments or becauseArgs | no proof, guarded in code |
| Pattern is a constant string? | pattern is not a string constant | `message-variable` |
| Only a * at the start or end? | wildcards other than a leading or trailing * | `message-wildcard` |
| No line break next to a `*`? | pattern contains a line break | `message-contains-line-break` |

| Condition | Fixed form | Proofs |
| --- | --- | --- |
| no WithMessage | `Should.Throw<T>(act[, because])`; `becauseArgs` as `string.Format(CultureInfo.InvariantCulture, because, args)` | `simple`, `because`, `because-args`, `func`, `lambda`, `aggregate-expected`, `aggregate-get-result` |
| `.Which` | `Should.Throw<T>(act)` (returns the exception) | `value-used`, `edge-which-message` |
| ThrowExactly | `Should.Throw<T>(act).ShouldBeOfType<T>()` | `ThrowExactly/*` |
| WithMessage *lit* | `Should.Throw<T>(act).Message.ShouldContain(lit, Case.Insensitive)` | `message-contains` |
| WithMessage lit* | `...Message.ShouldStartWith(lit, Case.Insensitive)` | `message-starts` |
| WithMessage *lit | `...Message.ShouldEndWith(lit, Case.Insensitive)` | `message-ends` |
| WithMessage lit | `...Message.ShouldBe(lit[, because], StringCompareShould.IgnoreCase)`; with a line break also `IgnoreLineEndings` | `message-exact`, `message-exact-because`, `message-because`, `message-because-args`, `message-line-break`, `edge-multiline-message` |

## See also

- [fa-throw-plan.md](fa-throw-plan.md) - plan
- [fa-throw-verify.md](fa-throw-verify.md) - verification
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs` - the pattern
- `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/` - the proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generic decision of a pattern
- [../README.md](../README.md)
