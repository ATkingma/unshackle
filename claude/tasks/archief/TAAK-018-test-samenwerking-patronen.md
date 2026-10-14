# TAAK-018 — Test patterns working together (combined proofs)

**Status**: ✅ done — 2026-10-11
**Created**: 2026-10-11
**Goal**: Automated tests that run the real `FluentAssertionsFix` (all 15 patterns, real order) through the `Fixer` and so prove that patterns work together.

## Context for the next agent

- `ProofTests` runs one pattern per proof (mocked `IFix`). Cooperation (AllSatisfy body rewritten by later patterns, `.And` splitting next to other patterns, final state of usings) was only proven manually (REQ-12 of `fa.all-satisfy`).
- In parallel another agent is working on TAAK-017 (Core contract, DataAccess, Integration tests): do not touch those files. Do not change pattern code.
- If a case finds a real bug in a pattern/Fixer: do not fix it, record it here with `[!]`.

## Analysis

### Current situation

`src/Unshackle.Tests/ProofTests.cs` discovers `Proofs/<Fix>/<Pattern>/<case>.before.cs` and runs only that one pattern. `FluentAssertionsFix.Patterns` (`src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`) deliberately has AllSatisfy first.

### Problem

- If someone moves AllSatisfy further back, the foreach body stays FluentAssertions code and no test fails.
- Interaction between patterns in one file (and the usings) is not tested.

### New situation

A second kind of proof in `Proofs/FluentAssertions/_Combined/<case>.before.cs` + `.after.cs`, run by `CombinedProofTests` with the whole fix; plus a test that pins the order.

### Improvement

Regressions in order, `.And` splitting and usings are caught automatically; REQ-12 of fa.all-satisfy is automated.

## Approach

### What / Where

- `src/Unshackle.Tests/CombinedProofTests.cs` (new): theory over `_Combined/*.before.cs` + order test.
- `src/Unshackle.Tests/ProofTests.cs`: excludes the `_Combined` folder from pattern discovery.
- Cases: `all-satisfy-be`, `all-satisfy-other`, `mixed`, `usings`, `usings-existing`, `manual-and-fixed`.
- Docs: `docs/projectinfo/patterns/fa-all-satisfy-verify.md`, `docs/projectinfo/architectuur/testen.md`.

### How

1. Before must compile against FluentAssertions. The Fixer runs with one `FluentAssertionsFix`.
2. After the fix: exact text equal to `.after.cs`; after compiles against Shouldly only. If the after still contains `using FluentAssertions;` (Manual remainder), then against Shouldly + FluentAssertions.
3. Order test: `Patterns[0].Id == "fa.all-satisfy"` with an explanation in the assert message.

### Why this approach

Only `.after.cs` (no `.manual.txt`): a combined file contains Fixed and Manual side by side, so the exact final text is the proof. A separate test class instead of extending ProofTests: ProofTests looks for one pattern per case and uses a mocked fix, which is a different contract.

## Steps

- [x] 1. Create task
- [x] 2. Write `_Combined` cases (before)
- [x] 3. `CombinedProofTests` + ProofTests exclusion
- [x] 4. Check/pin after files; note bugs
- [x] 5. Update docs (verify fa-all-satisfy, testen.md)
- [x] 6. `dotnet build` + `dotnet test` green

## Risks & edge cases

| Risk | Mitigation |
| ---- | ---------- |
| Other agent temporarily breaks the build | Wait 30 s, retry at most 5× |
| Case reveals a pattern bug | Do not fix; `[!]` + report |

## Changed files

- `src/Unshackle.Tests/CombinedProofTests.cs` (new), `src/Unshackle.Tests/ProofTests.cs` (excludes `_Combined`)
- `src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/*` (6 cases)
- `docs/projectinfo/patterns/fa-all-satisfy-verify.md`, `docs/projectinfo/architectuur/testen.md`

Finding (not a bug): `HaveCount(n).And.OnlyContain(...)` stays Manual (`.And` after HaveCount is not supported; own proofs `HaveCount/chain`, `OnlyContain/and-chain`). Only `BeGreaterThan().And.BeLessThan()` is split; that is in `mixed`.

## See also

- `claude/docs/INDEX.md`
- `docs/projectinfo/architectuur/testen.md`
- `docs/projectinfo/patterns/fa-all-satisfy-verify.md`
