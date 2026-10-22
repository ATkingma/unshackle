# Verification: moq-setup-sequence

|               |                                                                                      |
| ------------- | ------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/moq-setup-sequence-plan.md`](moq-setup-sequence-plan.md) |
| **Status**    | Verified                                                                             |
| **Generated** | 2026-10-22 by KingmaDoc (agent L2)                                                   |

> Verified by an AI agent. Review every deviation; the agent reports, it does not decide.

## Requirements

Proofs: `src/Unshackle.Tests/Proofs/Moq/SetupSequence/` (`ProofTests`) and `_Runtime/sequence-pass.cs` (`MoqRuntimeProofTests`).

| REQ   | Verdict | Evidence                                                                                                            |
| ----- | ------- | ------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met     | `returns.*`, `single.*`, `property.*`                                                                               |
| REQ-2 | Met     | `returns-async.*`                                                                                                   |
| REQ-3 | Met     | `SetupSequenceSyntax.Warning` on every Fixed result (`SetupSequencePatternTests`) |
| REQ-4 | Met     | `throws-step`, `void-member`, `function-step` `.manual.txt`                                                         |
| REQ-5 | Met     | `overlap.manual.txt`, `_Runtime/sequence-pass`                                                                      |

## Deviations

| #   | Area       | Plan says        | Code does    | Impact                                                                 |
| --- | ---------- | ---------------- | ------------ | ---------------------------------------------------------------------- |
| 1   | Exhaustion | M2: warning only | warning only | behaviour after the last value differs, not run-time provable as equal |

## Validation results

| Check | Command        | Result                       |
| ----- | -------------- | ---------------------------- |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed                       |
