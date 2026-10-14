# TAAK-151 — `ContainKey` to `ShouldContainKey` only where Shouldly has it

**Status**: ✅ completed (2026-10-14) — already solved on `main`, the old fix is not ported
**Created**: 2026-10-11 (corpus regression 2)
**Goal**: no compile error after rewriting `Should().ContainKey(k)` on a type without Shouldly's `ShouldContainKey`.

## Context for the next agent

- Commit `c8a562a` (branch `worktree-agent-a99fdb954988ae2dc`, 2026-10-11) rewrote `IReadOnlyDictionary` and dictionaries below C# 13 to `ContainsKey(k).ShouldBeTrue()` and string-keyed pairs (such as `HttpResponseHeaders`) to `ShouldContain(kv => kv.Key == k)`. It never reached `main`.
- `main` solved the same cases independently in `ContainKeyPattern`.

## Result

- Reproduced on `main` (`756e2ec`) with the proofs in `Proofs/FluentAssertions/ContainKey/`; they pass in `ProofTests`:
  - `read-only.*`, `read-only-not-contain.*`: `IReadOnlyDictionary` becomes `x.ContainsKey(k).ShouldBeTrue()` / `ShouldBeFalse(..)`.
  - `headers.*`: `HttpResponseHeaders` becomes `Headers.Contains(k).ShouldBeTrue()`. This is better than `c8a562a`'s `ShouldContain(kv => kv.Key == k)`: the type's own `Contains` is case-insensitive like HTTP header names.
  - `enumerable-pairs.*`: a type without `ContainsKey`/`Contains(key)` becomes a Manual.
  - `dictionary.*`: `Dictionary<,>` keeps `ShouldContainKey`.
- Where: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs`; the C# 13 case is `IsAmbiguousBeforeCSharp13` (below C# 13 a type with both `IDictionary<,>` and `IReadOnlyDictionary<,>` falls back to `ContainsKey(k).ShouldBeTrue()`).

## See also

- `docs/projectinfo/patterns/fa-contain-key-plan.md`
- `docs/projectinfo/patterns/fa-contain-key-verify.md`
- `claude/tasks/archief/TAAK-150-own-shouldbe-extension-ambiguous.md`
