# TAAK-021 — Nullable subject breaks the build after fa.have-count and fa.contain (string)

**Status**: ✅ done
**Created**: 2026-10-11
**Found in**: TAAK-019 step 1, case microsoft/PowerApps-Tooling @ 630e6973

## Problem

In a project with `<Nullable>enable</Nullable>` + warnings-as-errors the fix produces code that does not build. FluentAssertions' `Should()` accepts a nullable subject; the Shouldly form does not.

1. **fa.have-count** — `x.Should().HaveCount(n)` → `x.Count.ShouldBe(n)`. If `x` is nullable (`Dictionary<..>?`), then CS8602.
   Example: `src/Persistence.Tests/PaYaml/Serialization/PaYamlSerializerTests.cs:150,156,195-197`
   (`screen.Properties.Count.ShouldBe(...)`, `componentDefinition.Children.Count.ShouldBe(...)`).
2. **fa.contain (string)** — `s.Should().Contain("x")` → `s.ShouldContain("x", Shouldly.Case.Sensitive)`. `ShouldContain(string actual, ...)` has a non-nullable `actual`; with `string?` (e.g. `Exception.StackTrace`) CS8604.
   Example: `src/Persistence.Tests/Compression/PaArchiveTests.cs:335,389,390`.

8 unique build errors in total (x3 target frameworks).

## Approach (proposal)

Use the semantic model: if the subject is nullable-annotated (`NullableAnnotation.Annotated` / flow state MaybeNull), rewrite to `x.ShouldNotBeNull().Count.ShouldBe(n)` and `x.ShouldNotBeNull().ShouldContain(...)` respectively; that also preserves FA's failure on null. Alternative: Manual with a reason. A proof per case in `src/Unshackle.Tests/Proofs/FluentAssertions/<Pattern>/` with nullable enabled.

## Steps

- [x] Proofs (before/after) with a nullable subject for both patterns
- [x] Fix in `HaveCountPattern.cs` and `ContainPattern.cs` (`src/Unshackle.Core/.../Patterns/`)
- [x] Check other patterns that pass the subject as a non-nullable argument (be-empty, all-satisfy, contain-key, only-contain)
- [ ] PowerApps-Tooling case again: Persistence.Tests builds (not done in this session: belongs to TAAK-019)

## Choices

- **ShouldNotBeNull chain, no Manual.** `FluentAssertionsSyntax.NotNull(subject, model)` puts `.ShouldNotBeNull()` before the receiver when `GetTypeInfo(subject).Nullability.FlowState == MaybeNull`. Reason: it stays automatic, the behaviour stays the same (FA also fails on null) and Shouldly's `ShouldNotBeNull` has `[NotNull]`/returns `T`. Flow state instead of annotation: after a null check or in a nullable-disabled project the flow state is not MaybeNull, so no noise.
- **Applied in** have-count, contain and contain-key. Be-empty, only-contain and all-satisfy have a nullable proof without change: Shouldly's `ShouldBeEmpty`/`ShouldNotBeEmpty` accept `IEnumerable<T>?` and mark the subject as non-null afterwards, so the subsequent `foreach`/`ShouldAllBe` does not warn either.
- **Guard**: `TestSolution.CompileErrors` now also counts CS8602/CS8604, so every after proof with `#nullable enable` fails on these warnings (unit test `FluentAssertionsSyntaxTests`).
- Not done: the have-count branch with an unsafe receiver (parentheses) gets no `ShouldNotBeNull`; such an expression is rarely MaybeNull.

## See also

- `claude/tasks/TAAK-019-lancering.md` — case results
- `docs/projectinfo/patterns/`
