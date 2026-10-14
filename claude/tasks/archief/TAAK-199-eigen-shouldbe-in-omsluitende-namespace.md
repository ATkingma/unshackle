# TAAK-199 — An own `ShouldBe` in the enclosing namespace silently displaces Shouldly (no CS0121)

**Status**: ✅ completed (2026-10-13)
**Created**: 2026-10-13 (TAAK-174 step 3, Akka repeat)
**Goal**: a rewritten call that does not bind to Shouldly, but to an own extension, becomes Manual instead of silently changing behaviour.

## Context for the next agent

- Akka.NET (`23a8566`), test regression after the fix: `Akka.Tests.Serialization.V2PortKit.MixedBindingPairSpec.Should_FailTheCheck_When_TheV2NodeIsStillPinnedToLegacy`.
- Cause: `src/core/Akka.Tests.Shared.Internals.Xunit3/V2Port/MixedBindingPair.cs` is in `namespace Akka.TestKit;`. `serializer.Identifier.Should().Be(writerId, "{0}: …", stage, c.Name)` became `serializer.Identifier.ShouldBe(writerId, string.Format(…))`. That call binds to `Akka.TestKit.AkkaSpecExtensions.ShouldBe<T>(T, T, string)` (extensions from the enclosing namespace win over `using` imports), not to Shouldly. Akka's version throws an xUnit `EqualException` without the given text, so the test that checks for "V2 node to legacy node" in the error message fails.
- TAAK-174 only looks for CS0121 (ambiguity). Here there is no ambiguity, so the check sees nothing.

## Analysis

After the rewrite, check semantically per rewritten invocation that the bound symbol is in the Shouldly assembly (or `Should`). If the call binds to something else: file Manual with the reason "binds to <type>.<method> instead of Shouldly". Same place as `UsingDirectives.AmbiguityWithAddedAsync` (`Fixer.cs`). Also relevant for TAAK-198 (approach 2).

## Steps

- [x] 1. Unit test: a file in a namespace with its own `ShouldBe<T>(this T, T, string)`; the FA call is now silently rewritten.
- [x] 2. Binding check after the rewrite (all rewritten Shouldly calls), Manual if the symbol does not come from Shouldly.
- [x] 3. Proof in `src/Unshackle.Tests/Proofs/FluentAssertions/…` (`*.manual.txt`).
- [x] 4. Proof: Akka repeat, `MixedBindingPairSpec` green.

## Result

- `UsingDirectives.MisboundAddedCallAsync`: looks in the updated file for calls with the name of an extension from an added namespace (`ShouldBe`, `ShouldBeTrue` …) that bind to something outside those namespaces, and compares the count per name with the original (existing own `ShouldBe` calls therefore do not count). More than in the original → the `Fixer` falls back as with CS0121 (TAAK-174): file unchanged, Fixed → Manual "Not rewritten: …", plus Manual `usings` "a rewritten ShouldBe() binds to <type>.ShouldBe instead of Shouldly …".
- Unit tests: `AmbiguousAddedNamespaceTests.FixAsync_FileInNamespaceOfOwnShouldBe_LeavesFileAndReportsManual` and `…AlreadyUsingIt_MigratesOtherCalls`.
- Proof: `Proofs/FluentAssertions/ShouldBe/own-shouldbe-in-namespace.*` (`.manual.txt`). `ProofTests` now passes Shouldly as `AddedReferences` to the FA fix (like the real fix), otherwise the model does not see Shouldly; all 773 proofs stay green.
- Akka (fresh fix on `23a8566`): 10 files (54 findings) fall back on this rule, among others `MixedBindingPair.cs`, `V2PortSpec.cs` and 7 files in `Akka.TestKit.Tests`. Akka.Tests: 1553 passed, 0 failed (incl. `MixedBindingPairSpec`); build 0 errors.

## See also

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/archief/TAAK-198-shouldly-conflict-manual-verkleinen.md`
- `claude/tasks/CORPUSRUN-fa.md`
