# Models: fa.be-greater-less

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`.

## Figure 1: classes

**Figure 1.** `BeGreaterLessPattern`, its contract and the helpers; on the right the FluentAssertions methods it recognises and the Shouldly methods it creates.

<!-- kingmadoc:diagram img/fa-be-greater-less-modellen-1.d2 -->
![Figure 1: classes](img/fa-be-greater-less-modellen-1.png)

| Element | Role |
| ------- | ---- |
| `BeGreaterLessPattern` | Finds the comparisons (`Find`) and builds `Should...` calls (`RewriteAssertion`, after the shared checks of `FluentAssertionPattern`); `.And` chains are split by `AndChainPattern` |
| `FluentAssertionsSyntax` | Shared steps: subject (`GetSubject`), discarded result, `because` arguments (`TryGetArguments`), `ConvertExpected` (cast to the subject type) |
| `FluentAssertionPattern` | Template Method: `Rewrite` does the scope, shape, result and argument checks and calls `RewriteAssertion` |
| `PatternResult` | `Fixed` with one node, or `Manual(reason)` |
| FA and Shouldly methods | Fixed translation table `s_methods`; the aliases `BeGreaterOrEqualTo` and `BeLessOrEqualTo` coincide with their long name; `BeAfter`/`BeOnOrAfter`/`BeBefore`/`BeOnOrBefore` (TAAK-095) become `ShouldBeGreaterThan`/`…OrEqualTo`/`ShouldBeLessThan`/`…OrEqualTo` |

## Figure 2: sequence

**Figure 2.** `Find` returns every comparison; `FluentAssertionPattern.Rewrite` does the shared checks and calls `RewriteAssertion`. `.And` chains are split by `AndChainPattern`.

<!-- kingmadoc:diagram img/fa-be-greater-less-modellen-2.d2 -->
![Figure 2: sequence](img/fa-be-greater-less-modellen-2.png)

| Step | Method in the code | What happens |
| ---- | ------------------ | ------------ |
| Find | `Find` | Every comparison (`FindInvocations`) |
| Shared | `FluentAssertionPattern.Rewrite` | AssertionScope, `GetSubject` (`x.Should()`), result discarded |
| Type | `RewriteAssertion` → `SemanticModel.GetTypeInfo` | Non-nullable and `IComparable<T>` of itself |
| Arguments | `TryGetArguments` | Values and `because`; `becauseArgs` with a constant `because` → `string.Format(CultureInfo.InvariantCulture, ...)`, otherwise Manual |
| Values | `ConvertExpected` | Cast to the subject type (`(byte)1`); if that fails: Manual |

## Figure 3: decision tree

**Figure 3.** Every question in `FluentAssertionPattern.Rewrite` → `RewriteAssertion`; the leaves are the Shouldly form or the Manual reason, with the proof in the table.

<!-- kingmadoc:diagram img/fa-be-greater-less-modellen-3.d2 -->
![Figure 3: decision tree](img/fa-be-greater-less-modellen-3.png)

| Leaf | Condition | Proof (`Proofs/FluentAssertions/BeGreaterLess/`) |
| ---- | --------- | ------------------------------------------------ |
| Fixed, 1 call | standalone comparison, `because` included | `simple`, `because`, `because-args`, `in-range`, `sign`, `comparable`, `lambda`, `dates`, `edge-double`, `edge-interpolated-because` |
| Manual "is used" | result used | `value-used` |
| Fixed with cast | a value of another type than the `byte`/`short` subject | `byte-short` |
| Manual "nullable or has an unknown type" | `int?`, `DateTime?` | `nullable`, `dates-nullable` |
| Manual "named arguments, or becauseArgs" | named args or a non-constant `because` | `named-because`, `because-variable` (`because-args` is Fixed since TAAK-097) |
| Manual "only exists for" | `BePositive` on `byte` | `sign-byte` |
| Fixed `(x < 0).ShouldBeTrue()` | `BeNegative` on double/float (NaN: FA fails, `ShouldBeNegative` passes; `x < 0` is false) | `negative-double`, `negative-float` |
| Manual "Value type ... differs" | the value cannot be converted to the subject type | `value-type-differs` |

## See also

- [fa-be-greater-less-plan.md](fa-be-greater-less-plan.md)
- [fa-be-greater-less-verify.md](fa-be-greater-less-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/`
