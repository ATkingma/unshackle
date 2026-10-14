# TAAK-222 — `Throw<XunitException>()` on an own assertion helper in another file → helper migrated, expected exception no longer right

**Status**: ✅ completed (2026-10-13)
**Created**: 2026-10-13 (FA corpus run round 4)
**Goal**: no regression when a test checks that an own assertion helper fails with the FA/xUnit exception.

## Context for the next agent

- LiteDB `7b71bc4`: 26 regressions (13 tests × net8.0/net10.0) in `LiteDB.Tests/Query/Int32IdAssertions_Tests.cs`.
- The own helper `Int32IdAssertions.BeEquivalentTo(..)` in `Int32IdAssertions.cs` uses FA and is migrated to Shouldly. The tests do `Action candidate = () => Int32IdAssertions.BeEquivalentTo(..); candidate.Should().Throw<XunitException>()` and now get a `ShouldAssertException`.
- TAAK-070 only catches this when the assertion is in the lambda itself. Here it is in a method in another file.

## Steps

- [x] 1. In `fa.throw`: if `T` is an assertion exception type (`XunitException`, FA `AssertionFailedException`, NUnit `AssertionException`, MSTest `AssertFailedException`) and the delegate (also via a local) calls a method from the own solution that contains FA asserts → Manual, with the reason "the helper is migrated to Shouldly and throws ShouldAssertException".
- [x] 2. Consider the alternative: leave the helper file Manual, so both sides stay FA.
- [x] 3. Unit tests + proof; LiteDB repeat with 0 regressions.

## Result

Corpus round 5 (`CORPUSRUN-fa.md`): Akka 1 regression (only TAAK-200), Steeltoe 0, LiteDB 0. Build, `dotnet test` (1093) and sample tests (38) green.

## Choices

- Making only the test Manual does not help: the helper is then still migrated and throws `ShouldAssertException`. Converting `Throw<XunitException>` to `ShouldAssertException` is fragile (if one helper assert stays Manual, the helper throws `XunitException` again) and `WithMessage` with a `*`-middle pattern becomes Manual anyway. **Chosen: alternative 2**, helper and test both stay FA.
- `ExpectsAssertionFailure` follows, with an assertion exception as `T` (`XunitException`, `AssertionFailedException`, `AssertionException`, `AssertFailedException`, or derived), the methods with source code that the action calls (two levels): if one contains an FA assertion → the `Throw` becomes Manual.
- `IsInsideExpectedFailure` (all FA patterns) is also true inside such a helper: per syntax tree (cached in a `ConditionalWeakTable<SyntaxTree, …>`; files with one of those names in the text) the helpers called by a `Throw<assertion-exception>` action are collected. Key: type + name + number of parameters, stable across the compilations the Fixer makes per file.
- With another `T` (e.g. `InvalidOperationException`) the helper is simply migrated (unit test).
- Proofs: `Throw/assertion-helper`, `BeTrueFalse/assertion-helper`; unit tests in `FluentAssertionsSyntaxTests` with the helper in another file.

## See also

- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md`
- `claude/tasks/CORPUSRUN-fa.md` (round 4)
- `claude/tasks/patroon-queue.md`
