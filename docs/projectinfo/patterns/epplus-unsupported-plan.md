---
kingmadoc: 1
feature: "epplus-unsupported"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/EPPlusApi.cs
    - src/Unshackle.Tests/Proofs/EPPlus/Unsupported/
    - src/Unshackle.Tests/Core/EPPlus/EPPlusFixTests.cs
---

# Feature: epplus.unsupported: every EPPlus use left is Manual, with its queue row

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from TAAK-309 (queue row 11, `claude/tasks/epplus-queue.md`); council round 8, general rule and E6.

## One-sentence summary

epplus.unsupported runs last. It reports every member that still names EPPlus as Manual with a text-prefixed reason code
(`UnmigratedEPPlusApi`, listing each API and the queue row that will migrate it). A public or protected signature in a packable project
that exposes an EPPlus type is Manual by design (`PublicApiExposesOldLibrary`, E6).

## Scope (in / out)

**In scope**

- Every simple name that binds to a type or member in `OfficeOpenXml` (or a sub-namespace), or that is an unresolved member on an EPPlus-typed
  receiver (EPPlus 5+ member against 4.5.3.3, E2), outside `using` directives.
- One finding per member: method, constructor, property, field, event, top-level statement; a name in a type's base list or attribute
  anchors to the type.

**Out of scope**

- Code another EPPlus pattern already reports (spans of `LicenseContextPattern.Find` and `PackagePattern.Find`): not reported twice.
- Rewriting anything: this pattern only reports.

## Design

`EPPlusApi.Name` turns a name into `Type` or `Type.Member`; `EPPlusApi.Row` maps the member (else the type) name to its queue row, or
"no row planned, by hand". E6 checks the anchor's declared symbol: public/protected/protected internal up the containing-type chain, a signature
type (return, parameters, field/property/event type, base type, interfaces; array elements and generic arguments too) that names EPPlus, and a
packable project. Packable uses the same rule as MediatR's `AssemblyResolver`: a library that references no test framework. The reason codes
are text prefixes until TAAK-318 brings typed codes (council round 10, C1). Because `EPPlusFix.AllOrNothing` is true, any such row keeps its
whole project on EPPlus (per-project plan, council round 13 E17, TAAK-326).

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | A member using unmigrated EPPlus APIs is Manual, listing each API with its row (proof `Unsupported/cells`). |
| REQ-2 | An API with no planned row says "no row planned, by hand" (proof `Unsupported/chart`). |
| REQ-3 | A public signature in a packable project exposing EPPlus is `PublicApiExposesOldLibrary` (proof `Unsupported/public-api`). |
| REQ-4 | The same signature that is internal, or in a test project, is `UnmigratedEPPlusApi` (proof `Unsupported/internal-api`, `UnsupportedPattern_PublicApiInTestProject_NotE6`). |
| REQ-5 | An unresolved EPPlus 5+ member is named with its receiver (`UnsupportedPattern_UnresolvedEPPlus5Member_NamedWithItsReceiver`). |
| REQ-6 | Code the licence or package pattern reports is not reported again (`Fix_SaveWithoutFile_ManualAndSolutionUntouched`, `Fix_TwoPackagesInOneBlock_Manual`). |
| REQ-7 | R3 (council round 19): an `epplus.address` read left Manual in a member this pattern reports is folded into this pattern's one row for that member (`Fix_AddressReadInMemberReportedByUnsupported_OneRowWithAddressReason`, proof `Unsupported/address-folded`). |

## See also

- [verify](epplus-unsupported-verify.md), [modellen](epplus-unsupported-modellen.md)
- `claude/tasks/epplus-queue.md`
