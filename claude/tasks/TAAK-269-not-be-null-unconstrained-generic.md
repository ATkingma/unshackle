# TAAK-269 — `NotBeNull()` on an unconstrained type parameter breaks the build (CS0452)

**Status**: open
**Created**: 2026-10-15 (blind round, `claude/tasks/CORPUSRUN-blind.md`, ogooreck)

## Context

Ogooreck (`src/Ogooreck/API/ApiSpecificationExtensions.cs:315`, `HttpResponseMessageExtensions.cs:92`): `result.Should().NotBeNull()` where `result` is of an unconstrained type parameter (`TBody`, `T`). `fa.be-null` rewrites it to `result.ShouldNotBeNull()`, but Shouldly's `ShouldNotBeNull<T>(T?)` needs `T : class` (or a `Nullable<T>`): 12× CS0452 over the target frameworks, the build fails after the fix.

## Steps

1. [ ] Proof: `NotBeNull()` / `BeNull()` on an unconstrained `T`, on `T : struct`, on `T : notnull`.
2. [ ] `BeNullPattern`: when the subject's type is a type parameter without a `class` constraint, rewrite to `((object?)x).ShouldNotBeNull()` (boxing keeps the null check semantics of FA) or Manual; prefer the cast if the proof compiles and behaves the same.
3. [ ] Proof: harness `after` on ogooreck (`claude/tools/corpus-blind-list.txt`) builds, 0 regressions. Round 2 of TAAK-268.

## See also

- `claude/tasks/CORPUSRUN-blind.md`
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs`
