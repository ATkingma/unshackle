# Models: fa.be-equivalent-to-object

obj.Should().BeEquivalentTo(other) to obj.ShouldBeEquivalentTo(other), only for the same plain type with simple members; a collection of leaves becomes `ShouldBe(other, ignoreOrder: true)` (TAAK-092); everything else is Manual. Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`.

**Figure 1.** Classes around `BeEquivalentToObjectPattern`: what it implements, uses and creates. Left out: `Fixer`, `IFix` and the usings handling (see [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-be-equivalent-to-object-modellen-1.d2 -->
![Models: fa.be-equivalent-to-object](img/fa-be-equivalent-to-object-modellen-1.png)

| Element | Path | Role |
| --- | --- | --- |
| `IPattern` | `src/Unshackle.Core/Abstractions/IPattern.cs` | Contract: `Find` and `Rewrite` |
| `BeEquivalentToObjectPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` | This pattern, Id `fa.be-equivalent-to-object`; `Unsupported` walks over the type and its public members |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Shared building blocks: `FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`, `Invoke` |
| `FluentAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionPattern.cs` | Template Method: `Rewrite` does the shared checks, calls `RewriteBeforeArguments` (NotBeEquivalentTo, params) and then `RewriteAssertion` |
| `PatternResult`, `FixedResult`, `ManualResult` | `src/Unshackle.Core/Models/` | Outcome: `Fixed` with a new node (with a `Warning` for a non-sealed class), or `Manual` with a reason |
| «FluentAssertions» recognised | - | `BeEquivalentTo(expected[, because])`, with `options =>`, and `NotBeEquivalentTo` |
| «Shouldly» created | - | `ShouldBeEquivalentTo(expected[, because])`; for a collection of leaves `ShouldBe(expected, ignoreOrder: true[, because])` |
| Excluded types | - | `StringAssertions` (handled by `fa.be-equivalent-to-string`) |

**Figure 2.** `Fixer` calls `Find` and `Rewrite` per node; `FluentAssertionPattern.Rewrite` → `RewriteAssertion` does the checks via `FluentAssertionsSyntax` and `SemanticModel` and returns `Fixed` or `Manual`. Left out: replacing the node and the usings by the `Fixer`.

<!-- kingmadoc:diagram img/fa-be-equivalent-to-object-modellen-2.d2 -->
![Models: fa.be-equivalent-to-object](img/fa-be-equivalent-to-object-modellen-2.png)

| Step | Place | What happens |
| --- | --- | --- |
| Find | `BeEquivalentToObjectPattern.Find` | `FindInvocations` looks for `BeEquivalentTo`, `NotBeEquivalentTo` in FluentAssertions; `StringAssertions` drops out |
| Shape and arguments | `FluentAssertionPattern.Rewrite` | `IsInsideAssertionScope`, `GetSubject`, `IsResultDiscarded`, `RewriteBeforeArguments`, `TryGetArguments` (becauseArgs → `string.Format`) |
| Types | `RewriteAssertion` | `GetTypeInfo` of subject and expectation; `ElementType` + `IsLeaf` for a collection; `SymbolEqualityComparer` |
| Type examination | `Unsupported` | Collection, plain type, `Equals` override, cycle, public members, members recursively |
| Manual | `PatternResult.Manual` | The first failing check gives a `ManualResult` with a reason (see figure 3) |
| Fixed | `PatternResult.Fixed` / `FixedResult` with `Warning` | New node; the `Fixer` handles trivia and usings |

**Figure 3.** Decision tree of `FluentAssertionPattern.Rewrite` → `RewriteAssertion`: the questions in the order of the code, every leaf names the proof in `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/` that proves it.

<!-- kingmadoc:diagram img/fa-be-equivalent-to-object-modellen-3.d2 -->
![Models: fa.be-equivalent-to-object](img/fa-be-equivalent-to-object-modellen-3.png)

| Question or selector | If no: Manual reason | Proof |
| --- | --- | --- |
| Shape x.Should().BeEquivalentTo(...)? | is not in the form | `and-chain` |
| Result discarded? | The result of ... is used | `value-used` |
| Not NotBeEquivalentTo, no `params` overload? (`RewriteBeforeArguments`) | no Shouldly equivalent / (params items) is a collection assertion | `not-equivalent`, `edge-collection-params` |
| Arguments splittable (becauseArgs only with a constant because)? | named arguments, or becauseArgs whose because is no constant format string | no proof (`because-args` is Fixed since TAAK-097) |
| Exactly one value? | options => ... configuration | `options` |
| Type known? | The type of the subject or the expectation is unknown | no proof (`BeEquivalentTo(null)` is ambiguous) |
| Collection of leaves with the same element type? | yes: Fixed `ShouldBe(.., ignoreOrder: true)`; no: continue | see below |
| Same compile-time type? | Different compile-time types | `anonymous`, `different-types`, `base-vs-derived`, `collection-element-types` |
| Plain type, without collection, Equals override, cycle? | is a collection / not a plain class / overrides Equals / cyclic / no public members | `collection`, `collection-member`, `object-subject`, `object-member`, `equals-override`, `equals-member`, `cyclic`, `no-members` |

| Condition | Fixed form | Proofs |
| --- | --- | --- |
| collection of leaves (TAAK-092) | `a.ShouldBe(b, ignoreOrder: true[, because])` | `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `collection-because-args`, `edge-array-literal` |
| no because | `a.ShouldBeEquivalentTo(b)` | `simple`, `new-expectation`, `record`, `nested`, `lambda` |
| because / becauseArgs | `a.ShouldBeEquivalentTo(b, because)`; becauseArgs → `string.Format(CultureInfo.InvariantCulture, because, args)` | `because`, `because-args` |
| sealed type or both `new` | no `Warning`; otherwise a `Warning` about derived runtime types | `sealed`, `both-new` |

## See also

- [fa-be-equivalent-to-object-plan.md](fa-be-equivalent-to-object-plan.md) - plan
- [fa-be-equivalent-to-object-verify.md](fa-be-equivalent-to-object-verify.md) - verification
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` - the pattern
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/` - the proofs
- [../modellen/pattern-beslissing.md](../modellen/pattern-beslissing.md) - generic decision of a pattern
- [../README.md](../README.md)
