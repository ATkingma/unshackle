# TAAK-164 — Pattern fa.contain-equivalent-of

**Status**: ✅ completed
**Created**: 2026-10-12
**Goal**: `x.Should().ContainEquivalentOf(item)` / `NotContainEquivalentOf` automatically to `ShouldContain`/`ShouldNotContain` as exactly as possible; otherwise Manual with a specific reason.

## Context for the next agent

- Row 33 in `claude/tasks/patroon-queue.md`. Previously this call was found by no pattern at all.
- FA 7.2.2: `ContainEquivalentOf<TExpectation>(TExpectation, because, args)` and an overload with `config`; Shouldly 4.3.0: `ShouldContain<T>(IEnumerable<T>, T, customMessage)`.

## Analysis

Probe (scratchpad `probe164`, over 40 cases): for int, string, double (also `0.1+0.2` vs `0.3`: both fail), enum, int?, DateTime, DateTimeOffset (same moment, another offset: both pass), decimal (scale), TimeSpan, Guid, NaN, records, record structs and structs FA and Shouldly give the same outcome; also for a missing item, an empty collection, duplicates, a null item and a null subject (both fail, also `NotContainEquivalentOf`).

- Strings: FA compares **case-sensitively** in equivalency (`"abc"` vs `"Abc"` fails), just like `ShouldContain` on a collection (unlike `ShouldContain` on a string, where `Case.Sensitive` is needed).
- Difference 1: a class without `Equals` (`C { X = 1 }`): FA passes (members), Shouldly fails (reference) → Manual.
- Difference 2: an expectation type other than the element type: `List<object>` with `1L` (FA passes), `List<int>` with `2L` or `2.0` (FA passes; `ShouldContain(2L)` does not compile) → Manual.
- Null subject: both fail; `NotNull(subject)` puts `ShouldNotBeNull()` before the call for a nullable subject, like `fa.contain`.

## Choices

| #   | Choice | Reason |
| --- | ------ | ------ |
| 1 | `ContainEquivalentOfPattern : BeEquivalentToObjectPattern`; `IsValueLike`/`IsLeafOrNullable` moved from `EquivalentOptionsPattern` to the base pattern (protected) | One definition of "Equals equal to member comparison" (TAAK-163) |
| 2 | Fixed only if expectation and element have the same type and the element is value-like (leaf, sealed record, record struct, struct with leaf members) | Tried as equal; otherwise `ShouldContain` does not compile or the behaviour differs |
| 3 | Outcome `x.ShouldContain(item, because)` / `ShouldNotContain`, with `ShouldNotBeNull()` for a nullable subject | Same form as `fa.contain`; no weaker test on null |
| 4 | `config` overload: Manual ("equivalency options have no Shouldly equivalent") | Options change the comparison; `ShouldContain` does not have them |
| 5 | Class, non-sealed record, record with a collection member, dictionary (`KeyValuePair`): Manual | Not proven equal (reason: "is no leaf, record or struct of leaf members") |
| 6 | Result used (`.Which`) and AssertionScope: Manual via the template | Shared rules |

## Steps

- [x] 1. Research + probe
- [x] 2. Plan
- [x] 3. `ContainEquivalentOfPattern`, registration, `IsValueLike` moved
- [x] 4. 21 proofs, `proofs-doc.py`
- [x] 5. Build and tests green (35 known CRLF failures), models, verify, row `done`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainEquivalentOfPattern.cs` (new), `BeEquivalentToObjectPattern.cs`, `EquivalentOptionsPattern.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ContainEquivalentOf/`
- `docs/projectinfo/patterns/fa-contain-equivalent-of-{plan,modellen,verify}.md`, README row, `architectuur/proofs.md`

## See also

- `claude/tasks/archief/TAAK-163-fa-equivalent-options.md`
- `docs/projectinfo/patterns/fa-contain-equivalent-of-plan.md`
