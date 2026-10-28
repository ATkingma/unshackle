---
kingmadoc: 1
feature: "xunit3-extensibility"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/ExtensibilityPattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/Extensibility/
---

# Feature: xunit3.extensibility: Manual inventory of xUnit extensibility code

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-341 step 5 and the scout report `docs/projectinfo/architectuur/candidate-testfw.md`.

## One-sentence summary

A class that builds on the v2 extensibility API (an `Xunit.Sdk` base type, interface or discoverer attribute, `ITestCollectionOrderer`, `MemberDataAttributeBase`) is reported Manual `ExtensibilityType`, naming the v2 types.

## Scope (in / out)

**In scope**: class declarations deriving from or implementing a type of `xunit.core`/`xunit.execution.*` in namespace `Xunit.Sdk` (custom `DataAttribute`, `BeforeAfterTestAttribute`, `ITestCaseOrderer`, discoverers, `XunitTestFramework`, runners), or `Xunit.ITestCollectionOrderer`/`Xunit.MemberDataAttributeBase`, or carrying an `Xunit.Sdk` attribute (`XunitTestCaseDiscoverer`, `TraitDiscoverer`, ...).

**Out of scope**: subclasses of `FactAttribute`/`TheoryAttribute` without a discoverer, `TheoryData<T>` subclasses (its hidden `Xunit.Sdk.ITheoryData` is not counted), `Xunit.Abstractions` types (`xunit3.abstractions-namespace`), uses of `Xunit.Sdk` types outside class base lists and attributes (Info rows of `xunit3.sdk-use`, council 22 X12). Never Fixed.

**`[PropertyData]` is out of scope (council 22 X13)**: it is `[Obsolete(error: true)]` in xUnit 2.9.3, so a v2 project that builds cannot use it; there is nothing left to migrate.

## Requirements

- **REQ-1**: WHEN a class builds on a v2 `Xunit.Sdk` type, `ITestCollectionOrderer` or `MemberDataAttributeBase` THE SYSTEM SHALL report Manual `ExtensibilityType` naming those types (proof `custom-types`).
- **REQ-2**: WHEN a class carries an `Xunit.Sdk` discoverer attribute THE SYSTEM SHALL report it Manual too (unit test `Extensibility_Find_FactSubclassNotFoundDiscovererAttributeFound`).
- **REQ-3**: WHEN a class only derives from `FactAttribute` or `TheoryData<T>` THE SYSTEM SHALL not report it (unit test `Extensibility_Find_FactSubclassNotFoundDiscovererAttributeFound`).

## Assumptions

- v3 redesigned this API (Xunit.v3 namespace, ValueTask, other signatures, `DisposalTracker`), so there is no mechanical rewrite; the row is the inventory.
- With `AllOrNothing`, one such class keeps the whole solution on v2; the summary lists it as blocking.

## Models

[xunit3-extensibility-modellen.md](xunit3-extensibility-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.
