# TAAK-113 — `fa.equal-sequence` wraps a collection expression in an array

**Status**: done (2026-10-11)
**Created**: 2026-10-11 (corpus run, `claude/tasks/CORPUSRUN-fa.md` follows)

## Context

PowerApps-Tooling (42× CS0029, 12× CS1729, 12× CS1061 after fix): `x.Should().Equal([ "a", "b" ])` (collection expression as the only argument) becomes `x.ShouldBe(new string[] { [ "a", "b" ] })` — the pattern treats the collection expression as a params element.

## Steps

1. Proof `EqualSequence/collection-expression` (before compiles against FA 7.2.2).
2. One argument that is itself a collection (collection expression, array, IEnumerable) → pass it through; only wrap real separate params elements in `new T[] { … }`. A collection expression needs a target type: `ShouldBe((string[])[...])` or `new string[] { "a", "b" }` — choose what compiles and prove it.
3. Evidence: PowerApps copy `fix` again + build green.

## Result

- `EqualSequencePattern`: a collection expression as the only argument supplies the elements: `Equal(["a", "b"])` →
  `ShouldBe(new string[] { "a", "b" })`. With a spread (`[.. t, "b"]`) → Manual.
- Proofs: `EqualSequence/collection-expression.*`, `EqualSequence/collection-expression-spread.*`.
- Corpus: **not re-proven**. There is no clean PowerApps copy (`run-fa/src` and `casus/` have already been fixed; reverting requires
  version control, which is refused outside the worktree in this session). Rerun on a fresh clone.

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
