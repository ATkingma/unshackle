---
kingmadoc: 1
feature: "efbulk-update"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
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

**In scope**: an inline lambda creating exactly the entity type, no constructor arguments, simple assignments to scalar properties; values that are literals, `default`, captured locals/parameters/fields, instance properties of captured values, enum members and constants, the entity's own scalar properties (`x.P`), and operators, casts and parentheses over those. The value lambda always uses the long form `x => value` (EF7-10 compatible).

**Out of scope**: method calls (`Math.Max`), conditionals, static properties such as `DateTime.Now` (EF may evaluate them on the server), navigations and owned types, factories held in a variable, builder overloads. All Manual.

## Requirements

- **REQ-1**: WHEN the factory is an inline initializer with plain values THE SYSTEM SHALL emit one `SetProperty` per assignment, in source order (plus-initializer, extensions-async).
- **REQ-2**: WHEN a value is a method call, conditional or static property THE SYSTEM SHALL report Manual `UpdateValueNotProvable` (method-call; unit tests `FixAsync_StaticServerMember_IsManual`, `FixAsync_ConditionalValue_IsManual`).
- **REQ-3**: WHEN an assignment targets a non-scalar property (navigation, owned type) THE SYSTEM SHALL report Manual `UpdateValueNotProvable` (navigation).
- **REQ-4**: WHEN the factory is not an inline lambda initializer THE SYSTEM SHALL report Manual `UpdateFactoryShape` (factory-variable).
- **REQ-5**: WHEN the name `setters` is taken at the call THE SYSTEM SHALL pick `setters2`, `setters3`, ... (unit test `FixAsync_LocalNamedSetters_PicksAFreeName`).

## Assumptions

- The call is matched by the containing assembly of the bound method (`Z.EntityFramework.Plus.EFCore`, `Z.EntityFramework.Extensions.EFCore`), never by name: FreeSql, Azure Table helpers and LanguageExt use the same names.
- Like the Z batch calls, the native call bypasses the change tracker and `SaveChanges` interceptors; the fix states this once per run (`ChangedCodeInfo`).
- The Z types in the proofs are stubs with the documented signatures (the Z packages cannot enter `Directory.Packages.props`); to be checked against the nupkg before the corpus round.
- No package or project file is changed in this slice.

## Models

[efbulk-update-modellen.md](efbulk-update-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-342-ef-bulk.md`.
