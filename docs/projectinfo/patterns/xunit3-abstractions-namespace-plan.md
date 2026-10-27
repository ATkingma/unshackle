---
kingmadoc: 1
feature: "xunit3-abstractions-namespace"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/AbstractionsNamespacePattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/AbstractionsNamespace/
---

# Feature: xunit3.abstractions-namespace: using Xunit.Abstractions → using Xunit (ITestOutputHelper)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-341 (first xunit3 slice) and the scout report `docs/projectinfo/architectuur/candidate-testfw.md`.

## One-sentence summary

`using Xunit.Abstractions;` becomes `using Xunit;` (or is dropped when the same scope already imports `Xunit`), and `Xunit.Abstractions.ITestOutputHelper` written out becomes `Xunit.ITestOutputHelper`: v3 moved `ITestOutputHelper` to `Xunit`.

## Scope (in / out)

**In scope**: plain and `global` usings of `Xunit.Abstractions` (also `global::`), qualified `Xunit.Abstractions.ITestOutputHelper` (also in an alias using).

**Out of scope**: every other `Xunit.Abstractions` type (`IMessageSink`, `ITestCase`, ...: v3 redesigned the extensibility API, Manual `ExtensibilityType`); own `ITestOutputHelper` implementations (Manual `OutputHelperImplemented`, v3 adds `Output` and `Write`).

## Requirements

- **REQ-1**: WHEN a file uses only `ITestOutputHelper` from `Xunit.Abstractions` THE SYSTEM SHALL replace the using with `using Xunit;`, or drop it when the scope already has `using Xunit;` (proof `output-helper`, `swap-only`).
- **REQ-2**: WHEN `Xunit.Abstractions.ITestOutputHelper` is written qualified THE SYSTEM SHALL write `Xunit.ITestOutputHelper`, keeping `global::` (proof `qualified`).
- **REQ-3**: WHEN the files the using serves (one file, or every file for a `global using`) use another `Xunit.Abstractions` type THE SYSTEM SHALL report Manual `ExtensibilityType` (proof `message-sink`; unit test `AbstractionsNamespace_GlobalUsingWithSinkInAnotherFile_Manual`).
- **REQ-4**: WHEN a type in those files implements `ITestOutputHelper` THE SYSTEM SHALL report Manual `OutputHelperImplemented` (proof `own-helper`).

## Assumptions

- v3 has `ITestOutputHelper` in namespace `Xunit` with `WriteLine(string)` and `WriteLine(string, params object[])` (checked: every Fixed proof compiles against xunit.v3 4.0.2).
- The fix is `AllOrNothing`: one Manual row anywhere leaves the whole solution on v2 (half a migration does not build).

## Models

[xunit3-abstractions-namespace-modellen.md](xunit3-abstractions-namespace-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.
