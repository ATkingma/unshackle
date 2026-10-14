# TAAK-015 — Pattern fa.only-contain

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `x.Should().OnlyContain(pred)` becomes `x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 12 in `claude/tasks/patroon-queue.md`. Mechanism for one statement → several: `PatternResult.Fixed(IEnumerable<SyntaxNode>)`, like `BeGreaterLessPattern`. Sample: `OrderTests.cs:83`.

## Analysis

### Current situation

FA 7.2.2 `GenericCollectionAssertions<T>.OnlyContain(Expression<Func<T,bool>>, because, becauseArgs)` returns `AndConstraint`. Test program (scratchpad, FA 7.2.2 + Shouldly 4.3.0):

| Case | FA `OnlyContain` | Shouldly `ShouldAllBe` |
|---|---|---|
| empty collection | **fails** ("but the collection is empty") | **passes** |
| all items match | passes | passes |
| an item does not match | fails | fails |
| `null` | assertion failure | `ArgumentNullException` |

`ShouldNotBeEmpty()` is `void` and supports `customMessage`.

### Problem

A 1-to-1 `ShouldAllBe` lets an empty collection pass: a silent change in semantics.

### New situation

`OnlyContainPattern`: two statements `x.ShouldNotBeEmpty(); x.ShouldAllBe(pred);`; Manual where that is not possible.

### Improvement

The sample `order.Lines.Should().OnlyContain(l => l.UnitPrice > 0)` migrates automatically and keeps its behaviour.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Extra `ShouldNotBeEmpty()` statement, not Manual for everything | Preserves FA behaviour and keeps the pattern useful (sample) |
| 2 | Only when the statement is directly in a block (or switch section); otherwise Manual "cannot hold two statements" | A lambda body or an `if` without braces cannot contain two statements |
| 3 | Subject must be simple (name, `this`, member path); otherwise Manual "evaluate the subject twice" | Splitting evaluates the subject twice; same rule as `fa.be-greater-less` |
| 4 | `because` on both statements; `becauseArgs`/named args Manual | Like other patterns |
| 5 | Predicate carried over unchanged | FA and Shouldly both take `Expression<Func<T,bool>>`; a delegate predicate does not exist in FA |
| 6 | Subject must be a generic `IEnumerable<T>` (list, array, dictionary pass) | Shouldly `ShouldAllBe` is generic |
| 7 | Result used / after `.And` Manual | Shouldly returns nothing |
| 8 | Manual reasons: "is not in the form", "is used", "becauseArgs", "cannot hold two statements", "evaluate the subject twice" | `ProofTests` requires one reason per proof |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan
- [x] 4. `OnlyContainPattern` + registration
- [x] 5. Proofs (`OnlyContain/`)
- [x] 6. Build/test green, models, verify, archive, row `klaar`

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-010-fa-be-greater-less.md`
