# TAAK-172 — `BeEquivalentTo` on a collection with value-equal items → `ShouldBe(…, ignoreOrder: true)`

**Status**: ✅ completed — 2026-10-12
**Created**: 2026-10-12 (corpus run, `claude/tasks/CORPUSRUN-fa.md`)
**Goal**: 207 Manuals with the reason "FluentAssertions ignores the item order by default, Shouldly does not", spread over 13 projects, become Fixed when the items have value equality.

## Context for the next agent

- Examples: FluentResults `src/FluentResults.Test/MergeTests.cs:70` (`List<string>` vs `new[] { "A", … }`) and CSFE `ResultTests/Extensions/CombineInOrderTests.cs:80` (`IEnumerable<string>`).
- Distribution: FluentResults 53, LiteDB 50, Steeltoe 30, CSFE 28, Spec 15, EventFlow 9, PowerApps 8, the rest ≤ 4.
- The reason comes from `BeEquivalentToObjectPattern.cs:33` (params) and `:96` (collection).

## New situation

- Item type is string, primitive, enum, or a record/struct with only such members (the same leaf check as `ContainEquivalentOfPattern`) → `x.ShouldBe(expected, ignoreOrder: true)`.
- Params form → `new[] { … }`, keeping the formatting (see TAAK-153).
- In all other cases it stays Manual.

## Choices

- The collection case already existed for leaf items (TAAK-092); it was extended to `IsValueLike` (sealed record, record struct, struct with leaf members), the same check as `ContainEquivalentOfPattern`.
- Only `StringCollectionAssertions` has a `params` overload (`IEnumerable<int>.Should().BeEquivalentTo(1, 2)` does not compile): the params form is therefore strings only. It becomes `new string[] { ... }` (like `fa.equal-sequence`, not `new[]`: an explicit type also works with `null` items). Array building moved to `ParamsArraySyntax`, shared with `EqualSequencePattern`.
- An array as the argument is an ordinary expectation (normal form), not params.
- Manual reasons improved: two collections with a different element type or non-value items previously got "Different compile-time types".
- Probe (13 cases: reordering, duplicate items, another count, case, null items, empty, null subject, struct, record struct): FA and Shouldly gave the same outcome everywhere.
- Files: `BeEquivalentToObjectPattern.cs`, `ParamsArraySyntax.cs` (new), `EqualSequencePattern.cs`, proofs `BeEquivalentToObject/` (params-*, collection-record*, collection-struct, edge-collection-params now Fixed), docs `fa-be-equivalent-to-collection-*`.

## Risks & edge cases

| Risk | Mitigation |
| --- | --- |
| Strings: FA compares exactly in BeEquivalentTo | Matches `ShouldBe`; record in a proof |
| Duplicate items | Both count the number of occurrences; record in a proof |

## Steps

- [x] 1. Proofs: `List<string>`, params, `IEnumerable<int>`, record.
- [x] 2. Implementation; share the leaf check with `ContainEquivalentOfPattern`.
- [x] 3. Proof: probe FA 7.2.2 vs Shouldly 4.3.0 (the corpus clones were removed; harness not rerun).

## See also

- `claude/tasks/CORPUSRUN-fa.md`
