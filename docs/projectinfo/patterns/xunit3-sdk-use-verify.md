# Verification: xunit3-sdk-use

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/xunit3-sdk-use-plan.md`](xunit3-sdk-use-plan.md) |
| **Status**    | Verified                                                          |
| **Generated** | 2026-10-28 by KingmaDoc                                           |

> Verified by an AI agent (TAAK-341 step 5i) against the code on branch `taak-341d`.

## Requirements

Code: `src/Unshackle.Core/Fixes/XunitV3/Patterns/SdkUsePattern.cs`, `XunitV3SdkTypes.cs`, registration in `XunitV3Fix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/XunitV3/SdkUse/` (`ProofTests`: before compiles against xUnit 2.9.3).
Unit tests: `src/Unshackle.Tests/Core/XunitV3/XunitV3PatternTests.cs`, `XunitV3SdkTypesTests.cs`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | proof `changed-members`; unit test `SdkUse_GenericAndQualifiedUses_OneInfoRow`, `SdkUse_Find_UsingAndDeclarationBaseOnly_NotFound` |
| REQ-2 | Met | proof `changed-members` |
| REQ-3 | Met | unit test `SdkUse_ChangedMembersProof_DoesNotBuildOnV3` |
| REQ-4 | Met | unit test `MissingInV3_EqualsTheRealAssemblies` |

## Deviations

None. No corpus run yet (TAAK-341 step 7).

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed |
| Tests | `dotnet test` | all green |
| Verify | `claude/tools/verify-head.sh` | VERIFY OK (see TAAK-341) |

## See also

- [xunit3-sdk-use-plan.md](xunit3-sdk-use-plan.md)
- [xunit3-sdk-use-modellen.md](xunit3-sdk-use-modellen.md)
