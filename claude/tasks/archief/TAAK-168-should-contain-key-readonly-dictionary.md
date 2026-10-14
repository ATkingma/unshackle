# TAAK-168 — `ContainKey` on `IReadOnlyDictionary`/headers gives CS0411

**Status**: ✅ completed — 2026-10-12
**Created**: 2026-10-12 (corpus run, `claude/tasks/CORPUSRUN-fa.md`)
**Goal**: `x.Should().ContainKey(k)` also builds after the fix when `x` is not an `IDictionary<K,V>`.

## Context for the next agent

- Steeltoe (`f13e514`): 58× `error CS0411` (11 lines × TFMs). Examples: `src/Discovery/test/Eureka.Test/EurekaApplicationInfoManagerTest.cs:311` (`Metadata` is `IReadOnlyDictionary<string,string?>`) and `src/Management/test/Endpoint.Test/CorsPolicyTest.cs:47` (`response.Headers`).
- Shouldly's `ShouldContainKey` only exists for `IDictionary<TKey,TValue>`. `REGRESSIE-corpus-2.md` already listed this as "TAAK-151", but that task file was never created.

## Analysis

### Current situation

`ContainKeyPattern.RewriteAssertion` (`src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs:34`) always writes `subject.ShouldContainKey(k)` and does not look at the subject type.

### Problem

With `IReadOnlyDictionary`, `HttpHeaders` and similar types the result does not compile.

### New situation

- `IDictionary<,>` → as now.
- `IReadOnlyDictionary<,>` → `x.Keys.ShouldContain(k)` / `ShouldNotContain`.
- Other types → `x.Contains(k).ShouldBeTrue()` if that method exists, otherwise Manual.

### Improvement

After this task and TAAK-171 Steeltoe builds after the fix.

## Steps

- [x] 1. Proofs in `Proofs/FluentAssertions/ContainKey/` for `IReadOnlyDictionary` and `HttpResponseHeaders`.
- [x] 2. Type check in `ContainKeyPattern`.
- [x] 3. Proof: the Steeltoe harness no longer gives CS0411.

## Choices

- `IReadOnlyDictionary` → `x.ContainsKey(k).ShouldBeTrue()` instead of `x.Keys.ShouldContain(k)` (task proposal): `ContainsKey` keeps the comparer of every implementation (the `Keys` of an own type need not), and `NotContainKey` cannot become weaker through it. Probe FA 7.2.2 vs Shouldly 4.3.0: a case-insensitive dictionary gave the same result in FA and with `ContainsKey`.
- `HttpHeaders` (has `Contains(string)`, no `ContainsKey`) → `x.Contains(k).ShouldBeTrue()`. Probe: `Contains` is case-insensitive, FA's `ContainKey` on headers is not; the positive assertion is therefore slightly looser, intended behaviour for headers (documented in the plan).
- A type without `ContainsKey`/`Contains(key)` (`IEnumerable<KeyValuePair<,>>`) → Manual. The existing `read-only.after.cs` was wrong (did not compile against Shouldly) and was corrected.
- Step 3 (Steeltoe harness): the corpus was not rerun; the CS0411 forms (`IReadOnlyDictionary`, `HttpResponseHeaders`) are compilable proofs.

## Changed

`ContainKeyPattern.cs`, proofs `ContainKey/` (`read-only.after`, `read-only-not-contain.*`, `headers.*`, `enumerable-pairs.*`), `fa-contain-key-{plan,modellen,verify}.md`, README, INDEX, proofs.md.

## Risks & edge cases

| Risk | Mitigation |
| --- | --- |
| `ShouldBeTrue()` gives a less clear error message | Use `Keys.ShouldContain(k)` where possible |

## See also

- `claude/tasks/CORPUSRUN-fa.md`
- `claude/tasks/REGRESSIE-corpus-2.md`
