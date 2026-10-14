# TAAK-010 — Pattern fa.be-greater-less

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `BeGreaterThan`/`BeLessThan`/`...OrEqualTo`/`BeInRange`/`BePositive`/`BeNegative` (+ `.And` chains) becomes `ShouldBe...`, chain split, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 10 in `claude/tasks/patroon-queue.md`. Examples: TAAK-001 (splitting a statement), TAAK-009. Sample: `OrderTests.cs:84`.

## Analysis

FA 7.2.2 has these methods on `NumericAssertions<T>`, `ComparableTypeAssertions<T>` and `TimeSpanAssertions`; all `(expected, because, becauseArgs)` with an `AndConstraint`. Shouldly 4.3.0: `ShouldBeGreaterThan<T>(T, string? customMessage) where T : IComparable<T>` (likewise LessThan, OrEqualTo, `ShouldBeInRange`, `ShouldNotBeInRange`) and `ShouldBePositive`/`ShouldBeNegative` only for int, long, short, decimal, double, float. `DateTime` has `BeAfter`/`BeBefore` in FA: out of scope.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Scope: 4 comparisons + aliases `BeGreaterOrEqualTo`/`BeLessOrEqualTo`, `BeInRange`, `NotBeInRange`, `BePositive`, `BeNegative`; translation table `s_methods` | Same shape; Shouldly has all of them |
| 2 | Nullable subject (`int?`) is Manual | `T` becomes `int?`, which is not `IComparable<int?>`: does not compile |
| 3 | Subject must be `IComparable<T>` of itself | Shouldly constraint; covers TimeSpan, Version, numeric |
| 4 | `BePositive`/`BeNegative` only for the six Shouldly types, otherwise Manual | Other types (byte) have no overload |
| 5 | `Find` climbs along `.And` to the topmost call and, for a chain, returns the whole `ExpressionStatement` | Splitting replaces one statement with several (`Fixed(IEnumerable)`) |
| 6 | Only split a chain if all links are comparisons, the statement is directly in a block/switch section and the subject is a name or member access on names | The subject is evaluated n times; getters are assumed to be pure (shortcut) |
| 7 | Each link keeps its own `because`; `becauseArgs` is Manual | Shouldly cannot format |
| 8 | Manual reasons: "is not in the form", "not a comparison", "is used", "not directly in a block", "evaluate it twice", "nullable or has an unknown type", "becauseArgs", "only exists for" | `ProofTests` requires one reason per proof |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan (14 REQ) and models document (3 figures)
- [x] 4. `BeGreaterLessPattern` + registration
- [x] 5. Proofs (32 files in `BeGreaterLess/`)
- [x] 6. Build/test green (180 + 15 sample), verify, archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeGreaterLess/`
- `docs/projectinfo/patterns/fa-be-greater-less-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (row + Models column)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-009-fa-be-equivalent-to-string.md`
