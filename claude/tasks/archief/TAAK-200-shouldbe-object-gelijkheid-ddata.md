# TAAK-200 — `actual.Should().Be(expected)` on `object` → `ShouldBe` gives a different outcome (Akka DData)

**Status**: ✅ completed (2026-10-13) — reopened and finished: Fixed with a warning (user decision)
**Created**: 2026-10-13 (TAAK-174 step 3, Akka repeat)
**Goal**: find the cause of the deterministic test regression in `ReplicatorMessageSerializerSpec` and adjust `fa.should-be` or make it Manual if needed.

## Context for the next agent

- Akka.NET (`23a8566`), `src/contrib/cluster/Akka.DistributedData.Tests/Serialization/ReplicatorMessageSerializerSpec.cs`: `ReplicatorMessageSerializer_should_serialize_Replicator_message` is green in the baseline and fails after the fix, also when run on its own (2×).
- Rewritten in `CheckSerialization<T>(T expected)`: `object actual = _serializer.FromBinary(…)`; `actual.Should().Be(expected)` → `actual.ShouldBe(expected)`. Message: `actual should be Akka.DistributedData.Internal.DeltaPropagation (37787978) but was Akka.DistributedData.Internal.DeltaPropagation (29656116)`.
- `DeltaPropagation` has `IEquatable<DeltaPropagation>` + `Equals(object)`, compares `Deltas` (`ImmutableDictionary<string, Delta>`) per key; `Delta : IEquatable<Delta>`.
- FA 7 `ObjectAssertions.Be(object)` uses `Subject.IsSameOrEqualTo(expected)` (`actual.Equals(expected)`). Shouldly 4.3 `ShouldBe<T>` goes via `Is.Equal` with its own `EqualityComparer<T>` (T = `object`); that possibly takes another route (argument order, `IEquatable`/`IComparable` detection, or something in `Delta`/`DataEnvelope` that is seen as a collection).

## Steps

- [x] 1. Reproduce in a unit test/scratch: Shouldly `ShouldBe` vs `object.Equals` on two equal `DeltaPropagation`s; establish which path in Shouldly deviates.
- [x] 2. Decide: adjust the rewrite (e.g. `actual.Equals(expected).ShouldBeTrue()` is unreadable — rather Manual for an `object` subject with a type that only overrides `Equals`?) and consult the user.
- [x] 3. (dropped: no behaviour change; risk documented in `fa-should-be-plan.md`/`-verify.md`) Unit test + proof.
- [x] 4. (the test stays red; cause in the target code, see Choices) Proof: Akka repeat, test green.

## Finding (probe in the Akka DData test, 2026-10-13)

Temporarily in `CheckSerialization` for the `DeltaPropagation` case:

```
a.Equals(e)=True  e.Equals(a)=False  object.Equals(a,e)=True  EqualityComparer<object>.Default=True
ShouldBe → faalt
Delta per sleutel: A, B, C symmetrisch; "DC" (ORMultiValueDictionaryDelta): a.Equals(e)=False, e.Equals(a)=True
```

(Probe output as recorded: "faalt" = fails, "per sleutel … symmetrisch" = per key … symmetric.)

- Real cause: **the order of `Equals`**. FA 7 `ObjectAssertions.Be` does `actual.Equals(expected)`; Shouldly 4.3 `ShouldBe` does `expected.Equals(actual)`. No collection or `IEquatable` path.
- The asymmetry is in Akka: `ORMultiValueDictionaryDelta.Equals(IDeltaOperation)` compares a `DeltaGroup` with one operation differently than the other way round; after deserialisation the structure differs (group vs a single operation). `DeltaPropagation.Equals` calls `other.Delta.Equals(this.Delta)` per key, which reverses the direction once more.

## Choices

- **No rewrite change, no Manual.** Whether `Equals` is symmetric cannot be seen statically; Manual on every `Be` with a type with its own `Equals` (or an `object` subject) would throw away very many correct rewrites. Swapping the arguments (`expected.ShouldBe(actual)`) makes the error message wrong. The asymmetric `Equals` is a bug in the target code that FA happened not to hit.
- Recorded as a risk in `docs/projectinfo/patterns/fa-should-be-plan.md` (Risks) and `fa-should-be-verify.md` (row TAAK-200). The user can reconsider this; no consultation possible from this agent.

## User decision (2026-10-13): warning in the report

- `ShouldBePattern.EqualsOrderWarning` (also applies to `NotBePattern`): Fixed with `Warning` "Shouldly compares expected.Equals(actual), FluentAssertions actual.Equals(expected); check symmetry of X.Equals" when the subject or expected type (or a base type) declares its own `Equals(x)` outside the BCL; with two unknown runtime types (`object`, type parameter, interface) "the runtime types are unknown here …". Akka's `object actual` vs `T expected` falls under the second rule.
- No warning: BCL types (assembly `System*`/`Microsoft*`/`mscorlib`/`netstandard`), records without their own `Equals` (generated, symmetric), one side a concrete BCL type against `object`.
- Unit tests `src/Unshackle.Tests/Core/ShouldBeEqualsOrderTests.cs`; proof `Proofs/FluentAssertions/ShouldBe/own-equals.*`.
- The Choices above ("no Manual") stay; the test in Akka stays red, but the report now points at it.

## See also

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/CORPUSRUN-fa.md`
