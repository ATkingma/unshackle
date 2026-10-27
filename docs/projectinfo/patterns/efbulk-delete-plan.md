---
kingmadoc: 1
feature: "efbulk-delete"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/EfBulk/Patterns/DeletePattern.cs
    - src/Unshackle.Core/Fixes/EfBulk/EfBulkCall.cs
    - src/Unshackle.Tests/Proofs/EfBulk/Delete/
---

# Feature: efbulk.delete: q.Delete()/DeleteFromQuery() (+Async) → q.ExecuteDelete() (+Async)

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-27 by KingmaDoc |

> Drafted from TAAK-342, scout report `docs/projectinfo/architectuur/candidate-efbulk.md` (narrow scope). Undecided choices took the conservative option; see the council questions in `claude/tasks/TAAK-342-ef-bulk.md`.

## One-sentence summary

efbulk.delete: a batch delete of Z.EntityFramework.Plus (`Delete`, `DeleteAsync`) or Z.EntityFramework.Extensions (`DeleteFromQuery`, `DeleteFromQueryAsync`) on an EF Core `IQueryable<T>` becomes EF Core 7+ `ExecuteDelete` / `ExecuteDeleteAsync`; the receiver, type arguments and a `CancellationToken` stay.

## Scope (in / out)

**In scope**: the four spellings above, with no arguments or (async) only a `CancellationToken`.

**Out of scope**: overloads with a batch builder (`Action<BatchDelete>`: Manual `BatchOptions`), BulkExtensions `BatchDelete`, `BulkDelete(list)`, EF6 / EF Core 3-6 (Manual `NoNativeExecute`).

## Requirements

- **REQ-1**: WHEN a Z batch delete with no arguments is called on an `IQueryable<T>` THE SYSTEM SHALL rename it to `ExecuteDelete` / `ExecuteDeleteAsync` (plus-sync, extensions-from-query).
- **REQ-2**: WHEN the async call passes a `CancellationToken` THE SYSTEM SHALL keep it as the argument of `ExecuteDeleteAsync` (plus-async-token).
- **REQ-3**: WHEN the call passes a batch builder THE SYSTEM SHALL report Manual `BatchOptions` (batch-builder).
- **REQ-4**: WHEN a method with the same name binds to an assembly other than the Z packages THE SYSTEM SHALL not find it (unit test `FixAsync_SameNameOutsideZ_IsNotFound`).
- **REQ-5**: WHEN the compilation declares no `ExecuteDelete` (EF Core < 7) THE SYSTEM SHALL report Manual `NoNativeExecute` (unit test `FixAsync_NoExecuteDeleteInTheProject_IsManual`).

## Assumptions

- The call is matched by the containing assembly of the bound method (`Z.EntityFramework.Plus.EFCore`, `Z.EntityFramework.Extensions.EFCore`), never by name: FreeSql, Azure Table helpers and LanguageExt use the same names.
- Like the Z batch calls, the native call bypasses the change tracker and `SaveChanges` interceptors; the fix states this once per run (`ChangedCodeInfo`).
- The Z types in the proofs are stubs with the documented signatures (the Z packages cannot enter `Directory.Packages.props`); to be checked against the nupkg before the corpus round.
- No package or project file is changed in this slice.

## Models

[efbulk-delete-modellen.md](efbulk-delete-modellen.md).

## Open questions

- See "Council questions" in `claude/tasks/TAAK-342-ef-bulk.md`.
