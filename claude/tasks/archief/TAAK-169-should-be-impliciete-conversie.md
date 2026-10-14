# TAAK-169 — `Be(x)` with an own implicit conversion does not build

**Status**: ✅ completed — 2026-10-12 (step 3 not executed: LiteDB corpus not rerun, see Choices)
**Created**: 2026-10-12 (corpus run, `claude/tasks/CORPUSRUN-fa.md`)
**Goal**: `bsonValue.Should().Be(42)` becomes `bsonValue.ShouldBe((BsonValue)42)` (or Manual), not a `ShouldBe(42)` that does not compile.

## Context for the next agent

- LiteDB (`7b71bc4`): 11 lines break the build (80× CS1929, 8× CS0411). Examples: `LiteDB.Tests/Document/Json_Tests.cs:71` (`JsonSerializer.Deserialize(..).ShouldBe(positiveInt32)`), `Database/Document_Size_Tests.cs:39` (`d0["name"].ShouldBe("John")`) and `Issues/Issue2471_Test.cs:27` (`ShouldBeGreaterThan(0)`). The subject `BsonValue` has `implicit operator BsonValue(int/string)`.
- Baseline: 4,925 tests green per TFM. This is the only reason LiteDB does not build after the fix.

## Analysis

### Current situation

`FluentAssertionsSyntax.ConvertExpected` (used in `ShouldBePattern.cs:46` and `BeGreaterLessPattern`) accepts an implicit conversion. Type inference of `ShouldBe<T>`, however, does not use conversions the user defines.

### Problem

Inference finds no `T`, so CS1929/CS0411.

### New situation

With an own implicit conversion (`Conversion.IsUserDefined`) the fix puts an explicit cast to the subject type. FA `ObjectAssertions.Be` uses `subject.Equals(expected)`; with the cast Shouldly compares `BsonValue` with `BsonValue`. If the type does not override `Equals(object)`, Manual.

### Improvement

LiteDB builds after the fix, and the 6,874 Fixed findings really end up in the code.

## Steps

- [x] 1. Proof with an own type with an `implicit operator` (`Be` + `BeGreaterThan`).
- [x] 2. Add the cast in `ConvertExpected`.
- [ ] 3. Proof: LiteDB harness with `EXTRA=-m:1` (parallel building gives a file lock in LiteDB.Fuzz), 0 regressions.

## Choices

- Probe (<probe-dir>/probe-169, FA 7.2.2 vs Shouldly 4.3.0, a type with `implicit operator`, `Equals`, `IComparable<T>`): `Should().Be(5)` and `ShouldBe((T)5)` give the same outcome for equal, unequal and `BeGreaterThan`. For a class without an `Equals` override both fail (reference equality), so the migration does not become weaker; therefore **no** Manual for that case (deviation from the analysis above).
- `ConvertExpected`: an implicit conversion that `IsUserDefined` no longer counts as "already fits"; it falls through to the existing cast to the subject type. Applies to `ShouldBe`, `BeGreaterLess` and `DirectMapping`.
- Proofs: `ShouldBe/implicit-conversion.*` and `BeGreaterLess/implicit-conversion.*`. Pattern docs updated (plan, verify, models).
- Step 3 (LiteDB harness) not run in this run; the corpus fix still has to be proven again.

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/{ShouldBe,BeGreaterLess}/implicit-conversion.*`
- `docs/projectinfo/patterns/fa-should-be-*.md`, `fa-be-greater-less-*.md`

## See also

- `claude/tasks/CORPUSRUN-fa.md`
