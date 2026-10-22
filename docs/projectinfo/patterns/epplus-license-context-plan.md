---
kingmadoc: 1
feature: "epplus-license-context"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/LicenseContextPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/EPPlusSyntax.cs
    - src/Unshackle.Tests/Proofs/EPPlus/LicenseContext/
    - src/Unshackle.Tests/Core/EPPlus/EPPlusFixTests.cs
    - src/Unshackle.Tests/Core/EPPlus/EPPlusReferences.cs
---

# Feature: epplus.license-context: remove the EPPlus 5+ licence lines

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-309 step 3 (queue row 1, `claude/tasks/epplus-queue.md`); council round 8, E2 and the general rule.

## One-sentence summary

epplus.license-context: `ExcelPackage.LicenseContext = ...;` (EPPlus 5–7) and `ExcelPackage.License.SetXxx(...);` (EPPlus 8) are
removed when they are a statement of their own in a block, because ClosedXML has no licence switch.

## Scope (in / out)

**In scope**

- An expression statement in a block (or a top-level statement) whose expression is a simple assignment to `ExcelPackage.LicenseContext`
  or an invocation of `ExcelPackage.License.Set*(...)`, with `ExcelPackage` written plain, qualified (`OfficeOpenXml.ExcelPackage`)
  or `global::`-qualified. Any right-hand side (`NonCommercial`, `Commercial`, a variable, a config lookup).

**Out of scope**

- The same expression anywhere else: an expression-bodied member, an embedded statement (`if (x) ExcelPackage.LicenseContext = ...;`),
  a lambda body: Manual.
- Reading the licence (`if (ExcelPackage.LicenseContext == null)`): not found by this pattern (row 11 `epplus.unsupported`).
- The licence in configuration (`appsettings.json` `EPPlus:ExcelPackage:LicenseContext`, `app.config`): non-C#, a later
  `IProjectFileMigration` edit (council round 8, S3).

## Design

`EPPlusSyntax.IsPackageType` recognises `ExcelPackage` by symbol (namespace `OfficeOpenXml`), or, when the symbol does not resolve (EPPlus
5+ members against a 4.5.3.3 model, council E2), by its written name. An own type named `ExcelPackage` binds to a symbol outside
`OfficeOpenXml` and is never matched. The rewrite returns an empty `FixedResult` (the statement goes); the Fixer drops the
`using OfficeOpenXml;` once nothing binds to it.

The proofs compile against EPPlus 4.5.3.3 plus a small in-memory "EPPlus 5 surface" (`EPPlusReferences`): `LicenseContext`,
`EPPlusLicense` and C# 14 static extension members `ExcelPackage.LicenseContext` / `ExcelPackage.License`, so 5+ code compiles in a proof
while the test types stay 4.5.3.3 (E2).

No harness compare (E1): the pattern touches no value or style; removing a statement that only set a licence flag cannot change a
cell. The proof that pins behaviour is that the line is gone and the rest of the method is byte-for-byte the same.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | `ExcelPackage.LicenseContext = X;` as a statement in a block is removed (proof `statement`). |
| REQ-2 | Qualified `OfficeOpenXml.ExcelPackage.LicenseContext = ...` and the EPPlus 8 `ExcelPackage.License.SetXxx(...)` are removed (proofs `qualified`, `license-set`). |
| REQ-3 | The same expression outside a block statement is Manual (proofs `expression-bodied`, `embedded-if`). |
| REQ-4 | An own type named `ExcelPackage` / `LicenseContext` is not found (`Fix_OwnTypesNamedLikeEPPlus_NothingFound`). |
| REQ-5 | The line is found by syntax when EPPlus does not resolve at all (`LicenseContextPattern_UnresolvedExcelPackage_MatchedBySyntax`). |

## See also

- [verify](epplus-license-context-verify.md), [modellen](epplus-license-context-modellen.md)
- `claude/tasks/epplus-queue.md`, `claude/tasks/TAAK-309-epplus-to-closedxml.md`
- `docs/projectinfo/architectuur/corpus-epplus.md` — 33 hits / 15 repos
