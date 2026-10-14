# TAAK-171 — `ContainKey(k).WhoseValue` and `ContainKeys(..).And.ContainValues(..)`

**Status**: ✅ completed — 2026-10-12 (step 3: harness not rerun, see Choices)
**Created**: 2026-10-12 (corpus run, `claude/tasks/CORPUSRUN-fa.md`)
**Goal**: the 377 Manuals around `ContainKey`/`ContainKeys`/`ContainValues` (Steeltoe 249, Polly 120) become Fixed where that is safe.

## Context for the next agent

- In Steeltoe the reason is "The result of ContainKey(...) is used (.And/.Which or a value)"; mostly it is `.ContainKey(k).WhoseValue.Should().Be(v)`.
- Polly 7.2.4, `src/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerAsyncSpecs.cs:2576`: `contextData.Should().ContainKeys("key1", "key2").And.ContainValues("value1", "value2");`
- Now `ContainKeyPattern.cs:19-31` makes all Keys/Values variants Manual right away.

## New situation

- `ContainKey(k).WhoseValue.Should().Be(v)` → `x.ShouldContainKeyAndValue(k, v)`.
- `ContainKey(k)` with another follow-up assertion → `x.ShouldContainKey(k);` followed by `x[k].<rest>`, but only if `x` is a local variable, parameter or field.
- `ContainKeys(a, b)` → one `ShouldContainKey` per key, as separate statements.
- `ContainValues(a, b)` → `x.Values.ShouldContain(a)` per value.
- The type restriction from TAAK-168 applies here too.

## Steps

- [x] 1. Proofs for the three forms (`Proofs/FluentAssertions/AndChain/`: `contain-keys*`, `contain-value*`, `not-contain-*`, `whose-value*`).
- [x] 2. Implementation in `AndChainPattern` (`ContainKeyPattern` stayed unchanged).
- [x] 3. Proof: the Polly line is literally the proof `contain-keys-values`, the Steeltoe case is `whose-value`. The corpus clones are gone; the harness was not rerun (belongs to the next corpus round).

## Choices

- The split already lives in `AndChainPattern`; the new forms are "extensions of a chain": an `entries` list with one FA statement per outcome, then the same `RewriteLink` loop. That way `ContainKeyPattern`/`ContainPattern` decide per statement (also `IReadOnlyDictionary`, `because`, nullable), without duplicating logic.
- `ContainKey(k).WhoseValue.Should().Be(v)` becomes `x.ShouldContainKey(k); x[k].ShouldBe(v);`, not `ShouldContainKeyAndValue`: that way the proven rules for the value pattern apply (type conversion, because, strings) and every follow-up works (`StartWith`, `.And`). Requirements: x has a public indexer on the key type; k is constant or safe to evaluate twice; otherwise Manual. x counts as a subject for the hoist (`subject2` because `Y.WhoseValue` counts too).
- `ContainKeys`/`ContainValues`: only the params form with separate arguments (via `IInvocationOperation`, `ArgumentKind.ParamArray`); a collection or becauseArgs is Manual. `NotContainKeys`, `NotContainValues` and the singular `ContainValue`/`NotContainValue` (those keep `because`) are included too, because they have the same outcome: `x.Values.Should().Contain(v)` for `ContainPattern`.
- Probe FA 7.2.2 vs Shouldly 4.3.0: `ContainValue` and `Values.ShouldContain` both fail on an array with equal content and on `"X"` against `"x"`; no weakening.
- Nullable dictionary with `ContainValue(s)`: `x!.Values` (a null subject fails with a NullReferenceException; the test stays red).
- `.WhoseValue` without `.Should()` after it (`var v = x.Should().ContainKey(k).WhoseValue;`) stays Manual (existing proof `ContainKey/which`).

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/` (27 new proofs)
- `docs/projectinfo/patterns/fa-and-chain-{plan,modellen,verify}.md` (REQ-10..13), `docs/projectinfo/README.md`, `docs/projectinfo/architectuur/proofs.md`, `claude/docs/INDEX.md`, `claude/tasks/patroon-queue.md`

## See also

- `claude/tasks/archief/TAAK-168-should-contain-key-readonly-dictionary.md`
- `claude/tasks/CORPUSRUN-fa.md`
