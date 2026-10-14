# TAAK-150 — An own `ShouldBe`/`ShouldBeTrue` extension makes Shouldly calls ambiguous (CS0121)

**Status**: ✅ completed (2026-10-14) — already solved on `main`, the old fix is not ported
**Created**: 2026-10-11 (corpus regression 2, Akka.TestKit)
**Goal**: no CS0121 after the fix when a file has its own `Should*` extensions in scope.

## Context for the next agent

- Commit `81c3dd2` (branch `worktree-agent-a4fbd048474eb1b1e`, 2026-10-11) fixed this by putting `using Shouldly;` inside the namespace whenever a call became ambiguous, so Shouldly is the closer scope and wins. It never reached `main`.
- `main` solved the same problem in TAAK-174, TAAK-198 and TAAK-199 instead.

## Result

- Reproduced on `main` (`756e2ec`) with the proofs that cover this shape; they pass in `ProofTests`:
  - `Proofs/FluentAssertions/ShouldBe/akka-own-kit-ambiguous.*`: an own kit imported at the top gives a per-call Manual "is ambiguous with … (CS0121)", no broken build.
  - `Proofs/FluentAssertions/BeTrueFalse/own-shouldbe-in-scope.*`: calls whose Shouldly name does not clash are still migrated with targeted imports (`using static`, `using Should = Shouldly.Should;`).
  - `Proofs/FluentAssertions/ShouldBe/own-shouldbe-in-namespace.*`: an own `ShouldBe` in the enclosing namespace that silently wins gets reported (misbinding).
- Where: `UsingDirectives.AmbiguityWithAddedAsync` / `ConflictsWithAddedAsync` / `AddTargetedImports` and `Fixer.FixWithTargetedImportsAsync`; tests in `src/Unshackle.Tests/Core/AmbiguousAddedNamespaceTests.cs`.
- Why `81c3dd2` is not ported: with `using Shouldly;` inside the namespace, the existing calls to the project's own `ShouldBe` (Akka.TestKit's `AkkaSpecExtensions`) silently bind to Shouldly. That is the misbinding that TAAK-199 reports, just in the other direction, so the behaviour of user code would change without a sign. TAAK-174 also chose a Manual over a different import scheme on purpose. The cost is known: after TAAK-198 Akka still has about 2,943 per-call conflict Manuals.

## See also

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/archief/TAAK-198-shouldly-conflict-manual-verkleinen.md`
- `claude/tasks/archief/TAAK-199-eigen-shouldbe-in-omsluitende-namespace.md`
- `claude/tasks/archief/TAAK-151-fa-contain-key-without-idictionary.md`
