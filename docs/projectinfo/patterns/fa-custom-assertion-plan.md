---
kingmadoc: 1
feature: "fa-custom-assertion"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.DataAccess/Reports/ReportRow.cs
    - src/Unshackle.Interface/Output/SpectreConsoleView.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/CustomAssertion/
    - src/Unshackle.Core/Fixes/FluentAssertions/CustomAssertionAnalyzer.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/CustomAssertionPlans.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/CustomAssertionChain.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/custom-assertion-class.before.cs
---

# Feature: fa.custom-assertion: own FA assertions as Manual, grouped per extension

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Drawn up from TAAK-091 (ANALYSE-fa-100, category 3). Extended by TAAK-275 (after TAAK-271): plain `ReferenceTypeAssertions` classes convert.

## One-sentence summary

A call of a non-FA method on an FA assertions type (`result.Should().BeFailure()`) becomes `Manual` with one reason per extension, and report and console show those calls as one line with a count. Since TAAK-275 a plain `class XAssertions : ReferenceTypeAssertions<X, XAssertions>` whose methods only do `Execute.Assertion[.BecauseOf].ForCondition(bool).FailWith(constant, args)` is rewritten instead: the class becomes static `ShouldFoo(this X subject, ..., string? customMessage = null)` extensions that throw `ShouldAssertException`, its `Should()` extension is removed and every `x.Should().Foo(...)` becomes `x.ShouldFoo(...)`.

## Scope (in / out)

**In scope**

- Recognising: a method not from a `FluentAssertions` namespace, whose receiver (extension) or declaring type (instance) is or inherits from an FA type whose name ends in `Assertions`.
- Reason: "Custom assertion `Type.Method` (file or assembly): rewrite this FluentAssertions extension to Shouldly once; its calls can stay."
- Report (csv/json/md): one row per extension (first call) with "N call(s), first here." before the reason. Console: list per extension with a count, descending.

- (TAAK-275) Converting a `ReferenceTypeAssertions<T, itself>` subclass with only a pass-through constructor, an optional constant `Identifier`, and public methods returning `void`, `AndConstraint<itself>` or `AndConstraint<T>` made of `Execute.Assertion` statements. The decision is taken once per class for the whole solution (`CustomAssertionPlans`): class, `Should()` extension and every call convert, or nothing does.
- (TAAK-275) `.And` chains on such a class are split into statements; FluentAssertions links stay `x.Should().M()` for the later patterns.

**Out of scope**

- Extensions on FA types (`this StringAssertions a`), classes on other bases (`ObjectAssertions`, generic, partial), `Given`/`Then`, `AndWhichConstraint`, state, helpers, lazy messages, calls inside an `AssertionScope` or expected-failure block: Manual with the precise reason.
- Own `Should()` methods that do not return an FA type.

## Requirements

- **REQ-1**: WHEN an own extension on an FA assertions type is called THE SYSTEM SHALL give Manual with the extension name (proof `CustomAssertion/extension`).
- **REQ-2**: WHEN an instance method of a subclass of an FA assertions type is called THE SYSTEM SHALL give Manual (proof `CustomAssertion/subclass`).
- **REQ-3**: WHEN an FA method itself is called THE SYSTEM SHALL not let this pattern match (`IsFluentAssertions` on the method).
- **REQ-4**: WHEN the report is written THE SYSTEM SHALL merge the calls of one extension into one row with a count (unit test `FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount`).
- **REQ-5**: WHEN the console shows the summary THE SYSTEM SHALL show count and reason per extension, with one advice header.
- **Extension**: WHEN an own extension is inside an `AssertionScope` THE SYSTEM SHALL give the same custom reason (not "Inside an AssertionScope"); `fa.assertion-scope` does not convert such a scope, because the call is not an FA method (proof `CustomAssertion/in-assertion-scope`).

- **REQ-6**: WHEN a plain `ReferenceTypeAssertions<T, itself>` class only uses `Execute.Assertion[.BecauseOf].ForCondition(bool).FailWith(constant, args)` and all its uses convert THE SYSTEM SHALL rewrite it to Shouldly extension methods with the same pass/fail behaviour (probed against FA 7.2.2: the condition first, then every FailWith argument, failure throws outside a scope) (proof `CustomAssertion/reference-type`).
- **REQ-7**: WHEN such a class converts THE SYSTEM SHALL remove its `Should()` extension (its holder class when that is its only member) and rewrite every call `x.Should().Foo(args, because)` to `x.ShouldFoo(args, because)` (proof `CustomAssertion/reference-type`).
- **REQ-8**: WHEN a call is an `.And` chain on the class THE SYSTEM SHALL split it into statements, evaluating x once, and leave FluentAssertions links to the other patterns (proof `_Combined/custom-assertion-class`).
- **REQ-9**: WHEN the class or any use has no provably equal form (Given/Then, inside an AssertionScope, used by name, inactive `#if` code, no call in the solution) THE SYSTEM SHALL leave all of it alone and give each call Manual with "no automatic Shouldly form because <reason>" (proofs `CustomAssertion/given-then`, `CustomAssertion/class-in-assertion-scope`).
- **REQ-10**: WHEN the FluentAssertions fix runs THE SYSTEM SHALL run this pattern first, on the unchanged baseline, so every file sees the same decision (unit test `CombinedProofTests.AllSatisfyPattern_IsFirstAfterAssertionScope`).

## Risks

- (TAAK-275) The message text of the generated `ShouldAssertException` differs from FluentAssertions' (as for every Shouldly rewrite); only pass/fail is the same.
- (TAAK-275) A public assertions class in a library may be used by other solutions; a class without a call in its own solution is therefore not converted.
- (TAAK-275) `FluentAssertions.Primitives` is now a removed namespace: a file or global using that keeps it gets a `usings` row like the other FA namespaces.
- A JSON/CSV consumer that counts Manual rows now counts one row per extension for these extensions; the count is in the reason.

## Models

[fa-custom-assertion-modellen.md](fa-custom-assertion-modellen.md).

## Open questions

- None.
