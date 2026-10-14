# TAAK-114 — `NotBeEmpty()` on a `Guid` → `ShouldNotBeEmpty()` does not exist

**Status**: done (2026-10-11)
**Created**: 2026-10-11 (corpus run Polly, `claude/tasks/CORPUSRUN-fa.md`)

## Context

Polly `src/Polly.Specs/ContextSpecs.cs:43`: `context.CorrelationId.Should().NotBeEmpty()` (CorrelationId is `Guid`) was turned by
`fa.be-empty` into `context.CorrelationId.ShouldNotBeEmpty()` → CS1061: Shouldly only has `ShouldBeEmpty` for string and IEnumerable.

## Steps

- [x] Proofs `BeEmpty/guid.*` (Guid and `Guid?` BeEmpty) and `BeEmpty/guid-nullable-not-empty.*` (Manual).
- [x] `BeEmptyPattern`: subject `Guid`/`Guid?` → `ShouldBe(Guid.Empty)` / `ShouldNotBe(Guid.Empty)` (with because).
  `NotBeEmpty()` on `Guid?` → Manual: FA also fails on null, `ShouldNotBe(Guid.Empty)` does not.
- [x] Corpus: fresh copy of Polly (`scratchpad/v110/work/polly`): line 43 becomes `ShouldNotBe(Guid.Empty)`, 4446 Fixed / 966 Manual,
  **build green**.

## See also

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs`
- `docs/projectinfo/patterns/fa-be-empty-verify.md`
