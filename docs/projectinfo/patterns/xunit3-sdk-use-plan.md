---
kingmadoc: 1
feature: "xunit3-sdk-use"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/SdkUsePattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3SdkTypes.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/SdkUse/
---

# Feature: xunit3.sdk-use: Info row per file for uses of v2 Xunit.Sdk types

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-28 by KingmaDoc |

> Drafted from TAAK-341 step 5i (council 22 X12).

## One-sentence summary

A file that uses v2 `Xunit.Sdk` types (e.g. `new ExceptionAggregator()`) gets one Info row naming every type with every line, and saying when xunit.v3 3.2.2 has no type with the same full name.

## Scope (in / out)

**In scope**: names, generic names and constructor calls that bind to a public type in namespace `Xunit.Sdk` of a v2 assembly (`xunit.core`, `xunit.execution.*`, `xunit.assert`).

**Out of scope**: usings, and base lists and attributes of type declarations (Manual rows of `xunit3.extensibility`). Never Fixed and never Manual: Info does not block the AllOrNothing fix; the corpus (step 7) decides whether a use becomes Manual.

## Requirements

- **REQ-1**: WHEN a file uses v2 `Xunit.Sdk` types THE SYSTEM SHALL report one Info row per file naming each type with its lines (proof `changed-members`).
- **REQ-2**: WHEN a used type has no same-name type in xunit.v3 3.2.2 THE SYSTEM SHALL say so in the row (proof `changed-members`: `RunSummary`).
- **REQ-3**: WHEN a same-name v3 type exists but its members changed THE SYSTEM SHALL still report it; the proof's before does not build on v3 (unit test `SdkUse_ChangedMembersProof_DoesNotBuildOnV3`).
- **REQ-4**: The missing-in-v3 list SHALL be built from the real 2.9.3 and 3.2.2 assemblies, not by hand (unit test `MissingInV3_EqualsTheRealAssemblies`).

## Assumptions

- A usage alone gives no mechanical rewrite: v3 moved or redesigned much of `Xunit.Sdk`, so the row is an inventory.

## Models

[xunit3-sdk-use-modellen.md](xunit3-sdk-use-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.

## See also

- [xunit3-sdk-use-verify.md](xunit3-sdk-use-verify.md)
- [xunit3-extensibility-plan.md](xunit3-extensibility-plan.md)
