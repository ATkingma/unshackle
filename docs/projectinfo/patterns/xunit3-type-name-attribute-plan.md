---
kingmadoc: 1
feature: "xunit3-type-name-attribute"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/XunitV3/Patterns/TypeNameAttributePattern.cs
    - src/Unshackle.Core/Fixes/XunitV3/XunitV3Fix.cs
    - src/Unshackle.Tests/Proofs/XunitV3/TypeNameAttribute/
---

# Feature: xunit3.type-name-attribute: string-based [TestCaseOrderer] and friends → typeof(..)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-341 step 5 and the scout report `docs/projectinfo/architectuur/candidate-testfw.md`.

## One-sentence summary

`[CollectionBehavior]`, `[TestCaseOrderer]`, `[TestCollectionOrderer]` and `[TestFramework]` with a type name and an assembly name become `typeof(Type)`, because v3 only has the `Type` constructor.

## Scope (in / out)

**In scope**: the (string, string) constructors of those four v2 attributes, assembly-level or on a class, naming a type declared in the project itself.

**Out of scope**: a type of another assembly or a built-in v2 type (`Xunit.Sdk.XunitTestFramework`): Manual `TypeNameArgument`, as the v3 counterpart has another name. The named class itself is inventoried by `xunit3.extensibility`.

## Requirements

- **REQ-1**: WHEN the attribute names a type of this project and this assembly THE SYSTEM SHALL replace both arguments with `typeof(Type)` (minimal name) (proof `own-orderer`).
- **REQ-2**: WHEN the type is not in the project or the assembly name differs THE SYSTEM SHALL report Manual `TypeNameArgument` (proof `other-assembly`; unit test `TypeNameAttribute_NotOwnConstantType_Manual`).
- **REQ-3**: WHEN the attribute uses another constructor (`CollectionBehavior.CollectionPerClass`) THE SYSTEM SHALL leave it alone (unit test `TypeNameAttribute_EnumConstructor_NotFound`).

## Assumptions

- Attribute arguments are always constants, so the names are read with `GetConstantValue`; a non-string constant stays Manual.
- An own orderer or factory implements the v2 interfaces, so `xunit3.extensibility` reports it Manual and `AllOrNothing` keeps the solution on v2 until it is rewritten; the `typeof` rewrite is still correct groundwork.

## Models

[xunit3-type-name-attribute-modellen.md](xunit3-type-name-attribute-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-341-xunit-v3.md`.
