# TAAK-163 — Pattern fa.equivalent-options

**Status**: ✅ completed
**Created**: 2026-10-12
**Goal**: `BeEquivalentTo(y, o => o.Option())` automatic as exactly as possible; otherwise Manual with a reason per option.

## Context for the next agent

- Row 32 in `claude/tasks/patroon-queue.md`. Previously `fa.be-equivalent-to-object` gave the same Manual for every options call ("options => ...").

## Analysis

Probe (scratchpad `probe163`, FA 7.2.2 vs Shouldly 4.3.0, 45 cases): `WithStrictOrdering` on `List<T>` vs `T[]` and `ShouldBe(y)` give the same outcome for leaf items (int, string with null items, double with NaN, enum, int?, DateTime, decimal), sealed records, record structs and structs with leaf members: equal, reversed, longer/shorter, duplicates, empty, null subject/expectation/both, because. The last of `WithStrictOrdering`/`WithoutStrictOrdering` wins. `ExcludingMissingMembers`, `ThrowingOnMissingMembers` and `WithStrictOrdering` on an object behave the same as "no option" with the same compile-time type. Shouldly only has `(expected, ignoreOrder, customMessage)` for `ShouldBe` with a message.

## Choices

| #   | Choice | Reason |
| --- | ------ | ------ |
| 1 | Own class `EquivalentOptionsPattern : BeEquivalentToObjectPattern`; the base `Find` skips `BeEquivalentTo` with options | One owner per node; reuse of the object and leaf-collection logic |
| 2 | Only an inline lambda `o => o.A().B()` is read | Block, variable, method group: cannot be checked, Manual |
| 3 | `WithoutStrictOrdering`, `ExcludingMissingMembers`, `ThrowingOnMissingMembers` are dropped; `WithStrictOrdering` on a non-collection too | Tried: no effect with the same type |
| 4 | Strict on a collection with an equal item type and value items → `ShouldBe(y)` (with because: `ignoreOrder: false, because`) | Tried as equal; existing form from `fa.equal-sequence` |
| 5 | Value items: leaf, sealed record, record struct, struct with only leaf fields (public or behind a public auto-property), no own `Equals`, no computed public property | Then `Equals` equals FA's member comparison; class, non-sealed record, collection member: Manual |
| 6 | `ExcludingMissingMembers` + different types → own Manual reason | That is the usual case of that option |
| 7 | Other options: Manual, a reason per category (selection, comparison, runtime types, cycles, tracing) with the option text in it | Requested: a specific reason per option |
| 8 | `options.*` proof moved from `BeEquivalentToObject/` to `EquivalentOptions/excluding` | The pattern no longer finds that call |

## Steps

- [x] 1. Research + probe
- [x] 2. Plan
- [x] 3. `EquivalentOptionsPattern`, registration, base pattern adjusted
- [x] 4. 38 proofs, `proofs-doc.py`
- [x] 5. Build and tests green (35 known CRLF failures), models, verify, row `done`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/EquivalentOptionsPattern.cs` (new), `BeEquivalentToObjectPattern.cs`, `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/EquivalentOptions/`; `BeEquivalentToObject/options.*` removed
- `docs/projectinfo/patterns/fa-equivalent-options-{plan,modellen,verify}.md`, README row, `architectuur/proofs.md`

## See also

- `claude/tasks/archief/TAAK-016-fa-be-equivalent-to-object.md`
- `docs/projectinfo/patterns/fa-equivalent-options-plan.md`
