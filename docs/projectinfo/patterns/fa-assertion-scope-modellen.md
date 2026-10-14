# Models: fa.assertion-scope

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs` and `FluentAssertionsSyntax.IsInsideAssertionScope`.

## Figure 1: classes

**Figure 1.** `AssertionScopePattern`, its contract and the shared check that all FA patterns use. Since TAAK-100 the pattern runs first and converts a plain scope to `ShouldSatisfyAllConditions`.

<!-- kingmadoc:diagram img/fa-assertion-scope-modellen-1.d2 -->
![Figure 1: classes](img/fa-assertion-scope-modellen-1.png)

| Element | Role |
| ------- | ---- |
| `AssertionScopePattern` | Finds every `new AssertionScope(...)` (`Find`: the `using` statement, the block of a `using var`, otherwise the creation); `Rewrite` gives `Fixed` via `IsPlain` + `TryConvert`, otherwise `Manual` |
| `FluentAssertionsSyntax` | `IsAssertionScope(type)`, `IsInsideAssertionScope(node, model)`, `InsideAssertionScopeReason`; `IsFluentAssertions` and `IsSafeToEvaluateTwice` for `TryConvert` |
| Other FA patterns | Call `IsInsideAssertionScope` as the first step (`FluentAssertionPattern.Rewrite`, `AndChainPattern.Rewrite`, ...); afterwards rewrite the lambdas of a converted scope |
| `PatternResult` | `Fixed(x.ShouldSatisfyAllConditions(...))` or `Manual(reason)` |
| `AssertionScope` | `FluentAssertions.Execution.AssertionScope` (sealed) |

## Figure 2: sequence

**Figure 2.** A converted scope (`Fixed`), and a scope that stays with an assertion inside it (both `Manual`).

<!-- kingmadoc:diagram img/fa-assertion-scope-modellen-2.d2 -->
![Figure 2: sequence](img/fa-assertion-scope-modellen-2.png)

| Step | Method in the code | What happens |
| ---- | ------------------ | ------------ |
| Find | `AssertionScopePattern.Find` | All `BaseObjectCreationExpressionSyntax` with type `AssertionScope` |
| Scope | `AssertionScopePattern.Rewrite` → `IsPlain`, `TryConvert` | Without arguments and only FA assertion statements (no `await`, ref locals, scope variable): `x.ShouldSatisfyAllConditions(() => { a; }, ...)` with the first safe subject as `x`; otherwise `Manual` with advice |
| Assertion | `FluentAssertionPattern.Rewrite` (or `AndChainPattern.Rewrite`) → `IsInsideAssertionScope` | Walk the ancestors: `using` body or block with an earlier `using var` |
| Type | `SemanticModel.GetTypeInfo` | Declaration or expression type must be `AssertionScope` |

## Figure 3: decision tree

**Figure 3.** Questions of `AssertionScopePattern.Rewrite`, and then of `IsInsideAssertionScope` for assertions in a scope that stays.

<!-- kingmadoc:diagram img/fa-assertion-scope-modellen-3.d2 -->
![Figure 3: decision tree](img/fa-assertion-scope-modellen-3.png)

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Manual "Inside an AssertionScope" | body of an unconverted `using (new AssertionScope())`, also nested (proofs run only the ShouldBe pattern) | `ShouldBe/assertion-scope-using-block`, `-nested` |
| Manual "Inside an AssertionScope" | earlier `using var` scope in an enclosing block | `ShouldBe/assertion-scope-using-var` |
| Normal pattern | no scope ancestor | unit test `false` cases |
| Fixed `ShouldSatisfyAllConditions` | plain scope with only FA assertion statements | `_Combined/scope-using-block`, `_Combined/scope-using-var` |
| Manual "Shouldly has no scope" | context string, `Discard()`/scope variable, other statement, `await` | `AssertionScope/context`, `discard`, `other-statement`, `await` |

## See also

- [fa-assertion-scope-plan.md](fa-assertion-scope-plan.md)
- [fa-assertion-scope-verify.md](fa-assertion-scope-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AssertionScope/`
