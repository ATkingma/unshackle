# TAAK-041 — A subject with `?.` becomes Manual "result is used"

**Status**: ✅ completed (2026-10-11)
**Created**: 2026-10-11
**Goal**: the pending proof(s) below pass and are back in `Proofs/FluentAssertions/`.

## Context for the next agent

- Found while collecting edge cases from open-source projects (`docs/projectinfo/architectuur/edge-cases.md`).
- `uri?.Host.Should().BeNull();` gives Manual "The result of BeNull() is used" while the result is not used.
- Proofs are in `src/Unshackle.Tests/Proofs/_Pending/` (do not run, see `ProofTests.PendingFolderName`). The `.after.cs` is the desired outcome:
- `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/BeNull/edge-null-conditional.before.cs` + `.after.cs`

## Analysis

### Current situation

With `a?.B.Should().BeNull()` the assertion invocation is part of the `WhenNotNull` branch of a `ConditionalAccessExpressionSyntax`; the parent is therefore not an `ExpressionStatementSyntax` and `IsResultDiscarded` returns false.

### Problem

Real test code in this form is not migrated (correctly).

### New situation

`uri?.Host.ShouldBeNull();` (the `?.` stays; same semantics: with a null receiver nothing is checked, just like in FA). Or deliberately Manual with an honest reason.

### Improvement

Fewer needless Manual findings / no broken build after the migration.

## Approach

### How

Walk up in `IsResultDiscarded` through `ConditionalAccessExpressionSyntax` (as long as the invocation is in `WhenNotNull`) before the parent is tested. HaveCount already has its own `?.` Manual (HaveCountPattern.cs:32); leave it.

## Steps

- [x] 1. Move the proof(s) from `_Pending/` back to `Proofs/FluentAssertions/<Pattern>/`, see them fail.
- [x] 2. Fix in the code mentioned; all proofs (`--filter-class "*ProofTests"`) and `dotnet test` green.
- [x] 3. Update the verify doc of the affected pattern; archive the task.

## Risks & edge cases

| Risk | Mitigation |
| ---- | ---------- |
| Doubt: FA and Shouldly both skip the check with `?.`; that is already user-code behaviour, no new weakness. | Run all proofs + CombinedProofTests |

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` (`IsResultDiscarded`: `?.` chain and void expression body)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs` (ConfigureAwait)
- proofs moved out of `_Pending/`; verify docs fa-should-be, fa-be-null, fa-not-throw, fa-throw-async

## See also

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/edge-cases.md`
