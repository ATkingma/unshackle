---
kingmadoc: 1
feature: "moq-verify-all"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
  - src/Unshackle.Core/Fixes/Moq/Patterns/VerifyAllPattern.cs
  - src/Unshackle.Core/Fixes/Moq/VerifyAllSyntax.cs
  - src/Unshackle.Core/Fixes/Moq/VerifyAllPlan.cs
  - src/Unshackle.Core/Fixes/Moq/SetupReturnsSyntax.cs
  - src/Unshackle.Tests/Proofs/Moq/VerifyAll/
---
# Feature: moq.verify-all: `m.Verify()` / `m.VerifyAll()` become one `Received()` per covered setup

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton) |

> Drawn up from `claude/tasks/moq-queue.md` row 10 and council round 8, M4 (run-time proofs: round 9, Q2).

## One-sentence summary

`m.Verify()` (the `.Verifiable()` setups) and `m.VerifyAll()` (every setup) expand to one `m.Received().M(..)` per covered setup, with a
warning naming the setup lines, only when the setups are provably local; `VerifyNoOtherCalls` is Manual.

## Scope (in / out)

**In scope**

- `m.Verify();`, `m.VerifyAll();` on a local mock; setups `m.Setup(..).Returns(v)[.Verifiable()];` and `m.Setup(x => x.Void(..))[.Verifiable()];`
  (the void setup statement goes: a loose mock already does nothing; `moq.setup-returns` drops `.Verifiable()`).

**Out of scope**

- `VerifyNoOtherCalls` (Manual `VerifyNoOtherCalls`), static `Mock.Verify(m1, m2)`.

## Requirements

- **REQ-1**: WHEN `VerifyAll()` THE SYSTEM SHALL write one `m.Object.Received().M(args)` per setup, with the setup's argument specs; a property becomes `_ = m.Object.Received().P;` (proof `verify-all`).
- **REQ-2**: WHEN `Verify()` THE SYSTEM SHALL cover only the `.Verifiable()` setups (proof `verify-verifiable`).
- **REQ-3**: Each expansion SHALL carry a warning with the covered setups' lines (council M4).
- **REQ-4**: IF the mock is not a local, or a setup is not a plain statement before the Verify in the same method-body block (constructor, helper, field, condition, loop) THEN Manual `VerifyScopeNotLocal` (proofs `conditional-setup`, `field-mock`).
- **REQ-5**: IF two covered setups are on the same member THEN Manual `SetupsOverlap`: Moq credits a call only to the latest matching setup, NSubstitute's `Received` sees every matching call (proof `overlap`).
- **REQ-6**: IF `VerifyNoOtherCalls()` THEN Manual `VerifyNoOtherCalls` (proof `no-other-calls`).
- **REQ-7**: The whole fix SHALL pass and fail where Moq does (run-time `verify-all-pass`, `verify-verifiable-pass`; `verify-all-must-fail`, `verify-verifiable-must-fail` fail under both).

## Assumptions

- Moq's `Verify()`/`VerifyAll()` check that each covered setup matched at least one call; with no overlapping setups that equals `Received()` with the same argument specs.
- `moq.verify-all` runs first in `MoqFix.Patterns`, before `moq.setup-returns` drops the `.Verifiable()` marks.

## Risks

- A Verify between setups (setups after it) is Manual (`VerifyScopeNotLocal`), even though Moq would ignore the later setups.

## Models

[moq-verify-all-modellen.md](moq-verify-all-modellen.md).

## Open questions

- None.
