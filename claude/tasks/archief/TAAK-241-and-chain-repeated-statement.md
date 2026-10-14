# TAAK-241 — FA: `.And` chain writes `ShouldNotBeEmpty()` twice for `NotBeEmpty().And.OnlyContain(p)`

**Status**: ✅ completed
**Finished**: 2026-10-14
**Created**: 2026-10-14 (found by the corpus regression proofs of 2026-10-13, LiteDB shape)
**Goal**: a chain whose links rewrite to the same statement one after the other writes that statement once.

## Context for the next agent

- `OnlyContainPattern` becomes `x.ShouldNotBeEmpty(); x.ShouldAllBe(p);` (FluentAssertions fails on an empty collection).
  After `NotBeEmpty().And`, `AndChainPattern` wrote `x.ShouldNotBeEmpty();` twice: correct, but noise in the migrated code.
- `SyntaxNode.IsEquivalentTo` does not see the two statements as equal (nodes from different link patterns differ in token
  details), so the comparison uses the normalized text.

## Steps

- [x] 1. `AndChainPattern.Rewrite`: skip a statement that equals the previous one (`IsSameStatement`, normalized text).
- [x] 2. Proof `AndChain/not-empty-only-contain`; `_Combined/litedb-bson-assertions` expects one `ShouldNotBeEmpty()`.
- [x] 3. KingmaDoc plan/verify `fa-and-chain` REQ-14.

## Result

- `*Proof*` 835/835 green.
