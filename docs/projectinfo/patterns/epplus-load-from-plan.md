---
kingmadoc: 1
feature: "epplus-load-from"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/LoadFromPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/LoadFromMapping.cs
    - src/Unshackle.Tests/Core/EPPlus/LoadFromHarnessTests.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Tests/Proofs/EPPlus/LoadFrom/
---

# Feature: epplus.load-from: LoadFromCollection without headers → InsertData (covered property types only)

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from `claude/tasks/epplus-queue.md` row 10; council round 8, general rule, E1 and E4.

## One-sentence summary

epplus.load-from: `range.LoadFromCollection(items);` or `(items, false)` as a statement becomes `range.InsertData(items);` (plain cells, no header row) when every public member of the element type is a property of a harness-covered type; everything else is Manual.

## Scope (in / out)

**In scope (Fixed)**: `R.LoadFromCollection(items)` / `R.LoadFromCollection(items, false)` as an expression statement, element type a class or struct with only its own public readable properties of type string, int, long, double, bool, decimal or an enum (or their nullable forms) → `R.InsertData(items)`. The range `R` is left to row 4.

**Out of scope (Manual)**: `LoadFromHeadersNotMapped` (headers or a table style: ClosedXML `InsertTable` adds a table, `InsertData` writes no header); `LoadFromTypeDiffers` (a DateTime property: the harness shows another value or number format, council round 12 E15); `UncoveredRuntimeType` (a property of another uncovered type, a public field, a base class; and `LoadFromDataTable`, whose column types are only known at run time, E4); `UnmappedLoadFrom` (the result is used, `LoadFromArrays`, `LoadFromText`, `LoadFromDataReader`).

## Design

`LoadFromMapping.Method` binds the call to an EPPlus range method; `UncoveredMember` walks the element type's public members. **Behaviour proof (E1/E4)**: `LoadFromHarnessTests` runs one harness case per covered type (with nulls and empty strings) plus a mixed class; the scaffold (rows 2–5) is translated by hand. Each case also fails when the data starts one row lower.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | Each covered type (string incl. null/empty, int, long, double, bool, decimal, int?, enum, enum?) and a mixed class write the same cells. |
| REQ-2 | Every case fails when the data is written elsewhere. |
| REQ-3 | Headers or a table are Manual `LoadFromHeadersNotMapped`. |
| REQ-4 | An uncovered member type, a field, or LoadFromDataTable is Manual `UncoveredRuntimeType`. |
| REQ-5 | A used result is Manual `UnmappedLoadFrom`. |
| REQ-6 | DateTime properties are Manual `LoadFromTypeDiffers`; the harness difference is pinned (E15). |

## See also

- [verify](epplus-load-from-verify.md), [modellen](epplus-load-from-modellen.md)
- `claude/tasks/epplus-queue.md` row 10, `claude/tasks/TAAK-309-epplus-to-closedxml.md`
