# TAAK-225 — `Equal(..)` on a dictionary → `ShouldBe(.., ignoreOrder: true)`

**Status**: ✅ completed
**Created**: 2026-10-13 (FA corpus run round 4)
**Goal**: convert the largest Manual group that is not yet justified (apart from the Akka conflicts).

## Context for the next agent

- Round 4: 344× `fa.equal-sequence` "Equal(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand" (Akka 337, LiteDB 5, NUKE 1, Polly 1).
- FA `GenericDictionaryAssertions.Equal` requires the same keys and an equal value per key (`Equals`); the order does not count. Shouldly `dict.ShouldBe(expected, ignoreOrder: true)` compares the `KeyValuePair`s with `Equals` and is therefore equivalent for value items.
- Only convert if the key and value types have a reliable `Equals` (leaf/record/struct, the same rule as TAAK-172); otherwise leave Manual. Mind the type of `expected` (dictionary or list of pairs).

## Steps

- [x] 1. Find out how Shouldly `ShouldBe(IEnumerable<KVP>, ignoreOrder)` behaves with duplicate keys and `null` values, compared with FA.
- [x] 2. Rewrite in `fa.equal-sequence` + unit tests + proofs; update plan/verify.
- [x] 3. Akka repeat: build green, no extra regressions.

## Choices

- Probed (FA 7.2.2 vs Shouldly 4.3.0, console probe): order, missing/extra key, another value, `null` values, empty, immutable/read-only subject, `double`, list values: the same outcome. The only difference: subject and expectation both `null` (FA throws, Shouldly passes). Custom key comparer (OrdinalIgnoreCase): both fail.
- Conversion in `EqualSequencePattern` only if: the method is on `GenericDictionaryAssertions`, the expectation is itself a dictionary (`IDictionary<,>`/`IReadOnlyDictionary<,>`) with the same `KeyValuePair<K,V>`, and K and V are `IsValueLike` (TAAK-172 rule; made `protected internal` for this). Otherwise Manual with a specific reason.
- `because` → third argument (`ShouldBe(y, ignoreOrder: true, because)`).
- Note: the probe shows that FA also simply uses `Equals` here for class values; widening to all non-object/interface types is possible but deliberately not done (assignment: value items only).

## See also

- `claude/tasks/archief/TAAK-172-be-equivalent-to-collectie-ignore-order.md`
- `claude/tasks/CORPUSRUN-fa.md` (round 4)
- `claude/tasks/patroon-queue.md`
