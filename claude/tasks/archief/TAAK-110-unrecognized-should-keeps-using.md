# TAAK-110 — An unrecognized `.Should()` must not get `using FluentAssertions` removed

**Status**: done (2026-10-11)
**Created**: 2026-10-11 (corpus run, `claude/tasks/CORPUSRUN-fa.md` follows)

## Context

CSharpFunctionalExtensions (corpus run 2026-10-11): `maybe2.HasValue.Should().BeTrue()` (MaybeTests/Extensions/BindTests.cs line 54) gets **no** finding, but after `fix` only `using Xunit;` is left in the file → 42× CS1061 (`'bool'`/`'TestBase.T'` has no `Should`). 4481 Fixed / 49 Manual; build after fix fails.
Suspicion: the subject/call does not bind (type via lambda inference of `maybe2`), so `UsedRemovedNamespaces` does not see FluentAssertions as used and the using is removed.

## Steps

1. Minimal proof/unit test: `.Should()` on a member of a local whose type follows from a lambda (also: unbound `Should` call, error-type subject).
2. Root cause in `UsingDirectives`/`Fixer`: a remaining `Should()` call (syntactic, name `Should` on member access) or an unbound symbol in a file with a removed using → **keep** the using + Manual finding ("assertion not recognised; using kept").
3. Why the pattern did not recognize the line: separate finding (may be a `fa.be-true-false` gap).
4. Evidence: corpus copy of CSFE `fix` again + build green.

## Result

- Cause of line 54 (BindTests.cs): `fa.should-be` first rewrites `ctx.Should().Be(context)` in the lambda argument of `maybe.Bind(...)`.
  In the loaded model Shouldly is not yet referenced, so `ctx.ShouldBe` does not bind; as a result the overload resolution of
  `Bind` (Maybe/Task/ValueTask variants) picks nothing and `maybe2` is an error type. `fa.be-true-false` then no longer sees
  `maybe2.HasValue.Should()`. No gap in the pattern; the successive patterns on the intermediate result are the cause.
- Fix: `IFix.RemovedExtensionMethods` (FA: `Should`). `UsingDirectives.UnboundCalls` finds `x.Should()` that binds to nothing;
  then the old using stays (`UpdateAsync`), the package stays (`ProjectPackages`) and a Manual finding `usings` is added
  ("Run the fix again once the project builds"). A second run rewrites the rest.
- Proof: `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/unbound-should.*` (Maybe/Bind imitation; since TAAK-116 `lambda-overload-one-run.*`, Fixed in one run).
- Corpus: fresh copy of CSFE (`scratchpad/v110/work/csfe`): fix 4481 Fixed / 70 Manual (21 `usings`), **build green** (was 42x CS1061).

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
