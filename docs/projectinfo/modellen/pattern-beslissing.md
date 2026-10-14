# Decision: Fixed or Manual

**Figure 1.** The shared steps every FluentAssertions assertion pattern goes through: `FluentAssertionPattern.Rewrite` (Template Method) performs them with the help of `FluentAssertionsSyntax` and then calls the pattern's `RewriteAssertion`. The exception patterns (`Throw`, `ThrowAsync`, `NotThrow`, ...) implement `IPattern` themselves but perform the same steps. The pattern-specific checks are in [patronen.md](patronen.md).

<!-- kingmadoc:diagram img/pattern-beslissing-1.d2 -->
![Decision: Fixed or Manual](img/pattern-beslissing-1.png)

| Step | Helper in `FluentAssertionsSyntax` | Outcome on failure |
| ---- | ---------------------------------- | ------------------ |
| Scope | `IsInsideAssertionScope` | Manual: inside an `AssertionScope` that `fa.assertion-scope` did not convert |
| Form | `GetSubject` | Manual: not `x.Should().Methode(...)` |
| Result | `IsResultDiscarded` | Manual: Shouldly returns nothing (or the subject) |
| Arguments | `TryGetArguments` | Manual for named arguments or `becauseArgs` with a non-constant/invalid format string; otherwise it becomes `string.Format(CultureInfo.InvariantCulture, because, args)` (TAAK-097) |
| Own checks | `RewriteAssertion` per pattern | Manual with a pattern-specific reason |

## See also

- [patronen.md](patronen.md) — the pattern-specific checks
- [fix-flow.md](fix-flow.md) — what the Fixer does with the result
- [../README.md](../README.md)
