# TAAK-114 — `NotBeEmpty()` op een `Guid` → `ShouldNotBeEmpty()` bestaat niet

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11 (corpus-run Polly, `claude/tasks/CORPUSRUN-fa.md`)

## Context

Polly `src/Polly.Specs/ContextSpecs.cs:43`: `context.CorrelationId.Should().NotBeEmpty()` (CorrelationId is `Guid`) werd door
`fa.be-empty` `context.CorrelationId.ShouldNotBeEmpty()` → CS1061: Shouldly heeft `ShouldBeEmpty` alleen voor string en IEnumerable.

## Stappen

- [x] Proofs `BeEmpty/guid.*` (Guid en `Guid?` BeEmpty) en `BeEmpty/guid-nullable-not-empty.*` (Manual).
- [x] `BeEmptyPattern`: subject `Guid`/`Guid?` → `ShouldBe(Guid.Empty)` / `ShouldNotBe(Guid.Empty)` (met because).
  `NotBeEmpty()` op `Guid?` → Manual: FA faalt ook op null, `ShouldNotBe(Guid.Empty)` niet.
- [x] Corpus: verse kopie Polly (`scratchpad/v110/work/polly`): r43 wordt `ShouldNotBe(Guid.Empty)`, 4446 Fixed / 966 Manual,
  **build groen**.

## Zie ook

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEmptyPattern.cs`
- `docs/projectinfo/patterns/fa-be-empty-verify.md`
