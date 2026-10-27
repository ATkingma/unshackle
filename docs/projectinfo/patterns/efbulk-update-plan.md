---
kingmadoc: 1
feature: "efbulk-update"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/EfBulk/Patterns/UpdatePattern.cs
    - src/Unshackle.Core/Fixes/EfBulk/EfBulkCall.cs
    - src/Unshackle.Tests/Proofs/EfBulk/Update/
---

# Feature: efbulk.update: q.Update(x => new T { A = e }) → q.ExecuteUpdate(s => s.SetProperty(x => x.A, x => e))

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-342, scout report `docs/projectinfo/architectuur/candidate-efbulk.md` (narrow scope). Undecided choices took the conservative option; see the council questions in `claude/tasks/TAAK-342-ef-bulk.md`.

## One-sentence summary

efbulk.update: a batch update of Z.EntityFramework.Plus (`Update`, `UpdateAsync`) or Z.EntityFramework.Extensions (`UpdateFromQuery`, `UpdateFromQueryAsync`) whose factory is an inline `x => new T { P = value, ... }` becomes `ExecuteUpdate(setters => setters.SetProperty(x => x.P, x => value)...)`, only when every value is plain.

## Scope (in / out)

**In scope**: an inline lambda creating exactly the entity type, no constructor arguments, simple assignments to scalar properties; values that are literals, `default`, captured locals/parameters/fields, instance properties of captured values, enum members and constants, the entity's own scalar properties (`x.P`), and numeric operators, comparisons, casts and parentheses over those (council round 17, B4 allow-list), plus string `==`/`!=` against a non-null literal or `const` (round 19, B10). The value lambda always uses the long form `x => value` (EF7-10 compatible).

**Out of scope**: string concatenation, string comparison against anything but a non-null literal/`const`, any other binary operator on `string` or `decimal` operands (B4: provider-specific null handling, rounding and overflow), method calls (`Math.Max`), conditionals, static properties such as `DateTime.Now` (EF may evaluate them on the server), navigations and owned types, factories held in a variable, builder overloads. All Manual.

## Requirements

- **REQ-1**: WHEN the factory is an inline initializer with plain values THE SYSTEM SHALL emit one `SetProperty` per assignment, in source order (plus-initializer, extensions-async).
- **REQ-2**: WHEN a value is a method call, conditional or static property THE SYSTEM SHALL report Manual `UpdateValueNotProvable` (method-call; unit tests `FixAsync_StaticServerMember_IsManual`, `FixAsync_ConditionalValue_IsManual`).
- **REQ-3**: WHEN an assignment targets a non-scalar property (navigation, owned type) THE SYSTEM SHALL report Manual `UpdateValueNotProvable` (navigation).
- **REQ-4**: WHEN the factory is not an inline lambda initializer THE SYSTEM SHALL report Manual `UpdateFactoryShape` (factory-variable).
- **REQ-5**: WHEN the name `setters` is taken at the call THE SYSTEM SHALL pick `setters2`, `setters3`, ... (unit test `FixAsync_LocalNamedSetters_PicksAFreeName`).
- **REQ-6**: WHEN a value is a captured local, the entity's own member or numeric arithmetic of those THE SYSTEM SHALL rewrite it (captured-local, entity-member, numeric-arithmetic); WHEN it concatenates strings, does decimal arithmetic or reads `DateTime.Now` THE SYSTEM SHALL report Manual `UpdateValueNotProvable` (string-concatenation, decimal-arithmetic, static-now).
- **REQ-7**: WHEN a call is Manual THE SYSTEM SHALL show the exact expression in the reason, and for a batch builder name the options it sets (B4, B7; batch-builder proof, unit test `FixAsync_BatchBuilder_ReasonNamesTheOptionsAndTheCall`).
- **REQ-8**: WHEN a project has calls blocked by `NoNativeExecute` THE SYSTEM SHALL add one project-level Manual row `EfCoreBelow7` "EF Core < 7: upgrade first, N batch call(s) blocked" (B3; unit tests `FixAsync_NoExecuteDeleteInTheProject_AddsOneProjectRow`, `FixAsync_ExecuteDeleteAvailable_AddsNoProjectRow`). The report has no separate Warning kind for project rows, so `EfCoreBelow7` (a non-blocking project-level Manual row) **is** the B3 Warning row (council round 19, B12).
- **REQ-9**: WHEN a value is a captured field, an instance property of a captured value, an enum member or a constant THE SYSTEM SHALL rewrite it (captured-field, captured-property, enum-member, constant; council round 19, B11); static properties stay Manual (static-now).
- **REQ-10**: WHEN a value compares strings with `==`/`!=` and one side is a non-null string literal or `const` THE SYSTEM SHALL rewrite it (string-equality, `EfBulkSqlTests.ExecuteUpdate_StringEqualityWithConstant_MatchesCSharpOnNullColumn` with a NULL column row); against a captured variable it stays Manual (string-equality-captured; council round 19, B10).

## Assumptions

- The call is matched by the containing assembly of the bound method (`Z.EntityFramework.Plus.EFCore`, `Z.EntityFramework.Extensions.EFCore`), never by name: FreeSql, Azure Table helpers and LanguageExt use the same names.
- Like the Z batch calls, the native call bypasses the change tracker and `SaveChanges` interceptors; the fix states this once per run (`ChangedCodeInfo`).
- The Z types in the proofs are stubs (the Z packages cannot enter `Directory.Packages.props`).
- Signatures verified once (TAAK-342 step 4, council round 17 B6) against Z.EntityFramework.Plus.EFCore and Z.EntityFramework.Extensions.EFCore **10.105.9** (lib/net10.0, metadata only): Plus `Z.EntityFramework.Plus.BatchDeleteExtensions`/`BatchUpdateExtensions`, Extensions in the global-namespace classes `BatchDeleteExtensions`/`BatchUpdateExtensions`, all `where T : class`, `int`/`Task<int>` returns, builder types `Z.EntityFramework.Extensions.BatchDelete`/`BatchUpdate`. The stubs in `EfBulkReferences.cs` name that version; no CI download.
- Run-time proof (step 6): `EfBulkSqlTests` runs the rewritten shapes on EF Core 10 + SQLite in memory and checks the one executed statement; Z itself cannot run (paid Extensions trial).
- Package removal (step 8, council round 17 B5): see [efbulk-unsupported-plan.md](efbulk-unsupported-plan.md).

## Models

[efbulk-update-modellen.md](efbulk-update-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-342-ef-bulk.md`.
