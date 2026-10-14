# TAAK-014 — Pattern fa.all-satisfy

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `x.Should().AllSatisfy(a => ...)` becomes `x.ShouldNotBeEmpty(); foreach (...)`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 13 in `claude/tasks/patroon-queue.md`. Sample: `OrderHandlerTests.cs` (`orders.Should().AllSatisfy(o => o.Total.Should().Be(20m))`).

## Analysis

### Current situation

FA 7.2.2 `AllSatisfy(Action<T> expected, because, becauseArgs)`: runs an inspector with assertions for each element and collects all failures per index.

### Problem

Shouldly's `ShouldAllBe` takes an `Expression<Func<T,bool>>`: a lambda with assertions (void) does not fit. Measured FA behaviour: fails on an empty collection and on `null`; `ShouldAllBe` passes on empty.

### New situation

`AllSatisfyPattern` (ID `fa.all-satisfy`): the statement `x.Should().AllSatisfy(lambda)` becomes `x.ShouldNotBeEmpty();` + `foreach (var a in x) { body }`.

### Improvement

The sample is migrated automatically; the body assertions are rewritten by the other patterns.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | `foreach` instead of `ShouldAllBe` or `ShouldSatisfyAllConditions` | The body contains assertions (void), not a bool expression; converting each assertion to a bool is unreliable (`Be` is Equals, `BeEquivalentTo`, ...) |
| 2 | `x.ShouldNotBeEmpty();` before it | FA fails on empty/null (tested), foreach passes; this keeps the semantics the same |
| 3 | Subject must be a name or member access (otherwise Manual "evaluate it twice") | The subject appears twice |
| 4 | Pattern comes **first** in `FluentAssertionsFix` | The `using Shouldly` is only added after all patterns; if this pattern came last, the semantic model of the already rewritten body could not be bound and `Find` would find nothing. Placed first, the body is still FA and ShouldBe and others pick it up as standalone statements |
| 5 | `because` and `becauseArgs` Manual | The message cannot go into the loop; only the check would get it |
| 6 | Manual for method group, async lambda, `return` in body, parameter with modifier, not in a block, result used, `.And` | `return` would exit the method; the rest cannot be written as two statements |
| 7 | A typed parameter keeps its type, otherwise `var` | Same types as the lambda |
| 8 | Block body is re-indented via text (`Reindent`) and `ParseStatement` | No Formatter in Core; proofs compare exact text |
| 9 | Own `IsSimple` (copy from `BeGreaterLessPattern`) | Do not interfere with other people's files/helper API |
| 10 | The failure message differs (first failure instead of all per index) | Documented in the plan |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research (FA behaviour on empty/null/multiple failures measured)
- [x] 3. KingmaDoc plan (12 REQ)
- [x] 4. `AllSatisfyPattern` + registration
- [x] 5. Proofs (`AllSatisfy/`, 13)
- [x] 6. Models, verify, archive, row `klaar`

## See also

- `claude/tasks/patroon-queue.md`
- `docs/projectinfo/patterns/fa-all-satisfy-plan.md`
