# TAAK-269 — `NotBeNull()` on an unconstrained type parameter breaks the build (CS0452)

**Status**: done (fixed in `204fcd4`, `object?` in `d1f33bf`; ogooreck green in blind round 2)
**Created**: 2026-10-15 (blind round, `claude/tasks/CORPUSRUN-blind.md`, ogooreck)

## Context

Ogooreck (`src/Ogooreck/API/ApiSpecificationExtensions.cs:315`, `HttpResponseMessageExtensions.cs:92`): `result.Should().NotBeNull()` where `result` is of an unconstrained type parameter (`TBody`, `T`). `fa.be-null` rewrites it to `result.ShouldNotBeNull()`, but Shouldly's `ShouldNotBeNull<T>(T?)` needs `T : class` (or a `Nullable<T>`): 12× CS0452 over the target frameworks, the build fails after the fix.

## Steps

1. [x] Proof: `NotBeNull()` / `BeNull()` on an unconstrained `T`; `T : class` keeps the plain form (`T : struct` is covered by the existing struct path).
2. [x] `BeNullPattern`: when the subject's type is a type parameter without a `class` constraint, rewrite to `((object?)x).ShouldNotBeNull()` (boxing keeps the null check semantics of FA) or Manual; prefer the cast if the proof compiles and behaves the same.
3. [x] Proof: harness `after` on ogooreck (`claude/tools/corpus-blind-list.txt`) builds, 0 regressions. Round 2 of TAAK-268.

## Report

Steps 1-2 done. `BeNullPattern` now also boxes (`((object)x).ShouldNotBeNull()`) when the subject type is a type parameter that is not reference-type constrained. Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/`: `generic-not-be-null.*`, `generic-be-null.*`, `generic-class-constraint.*` (plain form). `dotnet build` clean, `dotnet test` 1498/1498. Step 3 (ogooreck harness run) not done.

## See also

- `claude/tasks/CORPUSRUN-blind.md`
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs`
