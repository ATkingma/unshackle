# Models: fa.only-contain

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs`.

## Figure 1: classes

**Figure 1.** `OnlyContainPattern`, its contract and the helpers; on the right the FluentAssertions method it recognises and the two Shouldly methods it creates.

<!-- kingmadoc:diagram img/fa-only-contain-modellen-1.d2 -->
![Figure 1: classes](img/fa-only-contain-modellen-1.png)

| Element | Role |
| ------- | --- |
| `FluentAssertionPattern` | Base class (Template Method): `Rewrite` does the scope, subject, result and argument checks and then calls `RewriteAssertion` |
| `OnlyContainPattern` | Finds `OnlyContain` (`Find`, the whole statement if it is in a block) and builds two statements (`RewriteAssertion`) |
| `FluentAssertionsSyntax` | Shared steps: subject, discarded result, `because` arguments, splittable statement, `IsSafeToEvaluateTwice`/`Hoist` |
| `PatternResult` | `Fixed` with two statements, or `Manual(reason)` |
| FA and Shouldly methods | `OnlyContain` fails on an empty collection, `ShouldAllBe` does not: `ShouldNotBeEmpty` makes up for that |

## Figure 2: sequence

**Figure 2.** One document: `Find` collects statements or calls, `Rewrite` decides per node between `Fixed` (two statements) and `Manual`.

<!-- kingmadoc:diagram img/fa-only-contain-modellen-2.d2 -->
![Figure 2: sequence](img/fa-only-contain-modellen-2.png)

| Step | Method in the code | What happens |
| ---- | ------------------ | -------------- |
| Find | `Find` | `FindInvocations` with `OnlyContain`; if the call is a statement in a block, the statement is the node |
| Scope and subject | `FluentAssertionPattern.Rewrite`: `IsInsideAssertionScope`, `GetSubject` | Not in a (non-converted) `AssertionScope`; must be `x.Should()`, otherwise Manual |
| Result | `IsResultDiscarded` | A used result gives Manual |
| Arguments | `TryGetArguments` | Predicate and `because`; `becauseArgs` with a constant `because` become `string.Format(CultureInfo.InvariantCulture, because, args)`; named args or a non-constant `because` give Manual |
| Type | `OnlyContainPattern.RewriteAssertion`: `GetTypeInfo` | Must be a generic `IEnumerable<T>` |
| Split | `TryGetSplittableStatement` | Statement directly in a block, otherwise Manual |
| Hoisting | `IsSafeToEvaluateTwice`, `Hoist` | A subject with possible side effects first becomes `var subject = x;` |

## Figure 3: decision tree

**Figure 3.** Every question in `Rewrite`; the leaves are the Shouldly form or the Manual reason.

<!-- kingmadoc:diagram img/fa-only-contain-modellen-3.d2 -->
![Figure 3: decision tree](img/fa-only-contain-modellen-3.png)

| Leaf | Condition | Proof (`Proofs/FluentAssertions/OnlyContain/`) |
| ---- | ---------- | ---------------------------------------------- |
| Fixed | statement in blok, eenvoudig subject | `simple`, `enumerable`, `array`, `dictionary`, `member-subject` |
| Fixed | with `because` (on both statements) | `because` |
| Fixed | `becauseArgs` with a constant `because` (`string.Format`) | `because-args` |
| Fixed with hoisting | the subject is a call: `var subject = x;` before it | `subject-call` |
| Manual "is not in the form" | na `.And` | `and-chain` |
| Manual "is used" | `.And`, variabele | `chain`, `used` |
| Manual "cannot hold two statements" | lambda body, `if` without braces | `lambda`, `no-block` |

## See also

- [fa-only-contain-plan.md](fa-only-contain-plan.md)
- [fa-only-contain-verify.md](fa-only-contain-verify.md)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/OnlyContainPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/OnlyContain/`
