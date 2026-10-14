---
kingmadoc: 1
feature: "fa-custom-assertion"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.DataAccess/Reports/ReportRow.cs
    - src/Unshackle.Interface/Output/SpectreConsoleView.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/CustomAssertion/
---

# Feature: fa.custom-assertion: own FA assertions as Manual, grouped per extension

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-091 (ANALYSE-fa-100, category 3).

## One-sentence summary

A call of a non-FA method on an FA assertions type (`result.Should().BeFailure()`) becomes `Manual` with one reason per extension, and report and console show those calls as one line with a count.

## Scope (in / out)

**In scope**

- Recognising: a method not from a `FluentAssertions` namespace, whose receiver (extension) or declaring type (instance) is or inherits from an FA type whose name ends in `Assertions`.
- Reason: "Custom assertion `Type.Method` (file or assembly): rewrite this FluentAssertions extension to Shouldly once; its calls can stay."
- Report (csv/json/md): one row per extension (first call) with "N call(s), first here." before the reason. Console: list per extension with a count, descending.

**Out of scope**

- Rewriting the extension itself (the body uses `Execute.Assertion`), generating shims.
- Own `Should()` methods that do not return an FA type.

## Requirements

- **REQ-1**: WHEN an own extension on an FA assertions type is called THE SYSTEM SHALL give Manual with the extension name (proof `CustomAssertion/extension`).
- **REQ-2**: WHEN an instance method of a subclass of an FA assertions type is called THE SYSTEM SHALL give Manual (proof `CustomAssertion/subclass`).
- **REQ-3**: WHEN an FA method itself is called THE SYSTEM SHALL not let this pattern match (`IsFluentAssertions` on the method).
- **REQ-4**: WHEN the report is written THE SYSTEM SHALL merge the calls of one extension into one row with a count (unit test `FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount`).
- **REQ-5**: WHEN the console shows the summary THE SYSTEM SHALL show count and reason per extension, with one advice header.
- **Extension**: WHEN an own extension is inside an `AssertionScope` THE SYSTEM SHALL give the same custom reason (not "Inside an AssertionScope"); `fa.assertion-scope` does not convert such a scope, because the call is not an FA method (proof `CustomAssertion/in-assertion-scope`).

## Risks

- A JSON/CSV consumer that counts Manual rows now counts one row per extension for these extensions; the count is in the reason.

## Models

[fa-custom-assertion-modellen.md](fa-custom-assertion-modellen.md).

## Open questions

- None.
