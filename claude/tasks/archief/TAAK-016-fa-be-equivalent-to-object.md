# TAAK-016 — Pattern fa.be-equivalent-to-object

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `a.Should().BeEquivalentTo(b)` on objects becomes `a.ShouldBeEquivalentTo(b)` only where FluentAssertions and Shouldly demonstrably decide the same; the rest is Manual.

## Context for the next agent

- Row 14 in `claude/tasks/patroon-queue.md`. Strings are row 9 (TAAK-009); Find excludes `StringAssertions`. Sample: `OrderHandlerTests.cs:30` (`OrderDto`, primitives only) becomes Fixed.

## Analysis (test program FA 7.2.2 against Shouldly 4.3.0, ~70 cases)

- Shouldly is stricter: collections in order (FA is not), runtime types/members (FA uses the members of the expectation; anonymous types, base vs derived class, subset expectation fail in Shouldly), `int` vs `long`, interface types, `Equals` override where FA uses `Equals` but members differ. These are audible extra failures.
- Dangerous (FA fails, Shouldly passes): type with an `Equals` override that returns false (also as a member), cyclic graph, type without public members (FA throws `InvalidOperationException`).
- Equal: same type, primitives, string (case-sensitive, null vs empty fails), decimal, enum, DateTime/Guid, records, structs, private ignored, public fields included, null/null.
- `ShouldBeEquivalentTo(expected, customMessage)` exists; `ShouldNotBeEquivalentTo` does not.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Fixed only when both sides have the same compile-time type (ignoring nullability) | Otherwise FA only compares the members of the expectation |
| 2 | Type and reachable members: only plain class/struct/record + leaf members (primitives, string, decimal, enum, DateTime(Offset), Guid, TimeSpan, DateOnly, TimeOnly, Nullable of those) | Only those were tried and behave the same |
| 3 | Collection (subject or member) Manual | FA ignores order |
| 4 | `Equals` override, cycle, no public members Manual | Dangerous direction: Shouldly passes where FA fails |
| 5 | Non-sealed class allowed | A derived runtime type gives an audible extra failure, never a silent difference |
| 6 | `options =>`, becauseArgs, NotBeEquivalentTo, chains, used result Manual | No equivalent |

## Steps

- [x] 1. Claim, research, choices
- [x] 2. KingmaDoc plan (13 REQ, approved)
- [x] 3. `BeEquivalentToObjectPattern` + registration
- [x] 4. Proofs (6 Fixed, 14 Manual) in `BeEquivalentToObject/`
- [x] 5. Models (3 figures), verify, README row
- [x] 6. Build/test green (236 + 15 sample), archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`
- `docs/projectinfo/patterns/fa-be-equivalent-to-object-{plan,modellen,verify}.md` (+ `img/`), `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-009-fa-be-equivalent-to-string.md`
