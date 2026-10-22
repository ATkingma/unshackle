---
kingmadoc: 1
feature: "epplus-styles"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/StylesPattern.cs
    - src/Unshackle.Core/Fixes/EPPlus/Patterns/StylesMapping.cs
    - src/Unshackle.Core/Fixes/EPPlus/EPPlusFix.cs
    - src/Unshackle.Tests/Proofs/EPPlus/Styles/
    - src/Unshackle.Tests/Core/EPPlus/StylesHarnessTests.cs
    - src/Unshackle.Tests/Core/EPPlus/StylesUnsupportedTests.cs
---

# Feature: epplus.styles: EPPlus style statements → ClosedXML, harness-proven shapes only

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Project**   | Unshackle                                                    |
| **Status**    | Approved                                                     |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

> Drawn up from `claude/tasks/epplus-queue.md` row 8; council round 8, general rule, E1 and E4.

## One-sentence summary

epplus.styles: a statement that sets one style property through an EPPlus `ExcelStyle` becomes the ClosedXML statement on the same
receiver, for a closed list of shapes each proven equal per cell by the harness; every other style statement is Manual.

## Scope (in / out)

**In scope (Fixed)** — `S` is any expression of type `OfficeOpenXml.Style.ExcelStyle` (usually `x.Style`), kept as it is:

| EPPlus | ClosedXML |
| --- | --- |
| `S.Font.Bold / Italic = v;` | same |
| `S.Font.Strike = v;` | `S.Font.Strikethrough = v;` |
| `S.Font.Size = v;` / `S.Font.Name = v;` | `S.Font.FontSize = v;` / `S.Font.FontName = v;` |
| `S.Font.Color.SetColor(c);` (c a `System.Drawing.Color`) | `S.Font.FontColor = XLColor.FromColor(c);` |
| `S.Fill.PatternType = ExcelFillStyle.Solid;` directly followed by `S.Fill.BackgroundColor.SetColor(c);` | first removed, second `S.Fill.BackgroundColor = XLColor.FromColor(c);` (solid implicit) |
| `S.Numberformat.Format = v;` | `S.NumberFormat.Format = v;` |
| `S.WrapText = v;` | `S.Alignment.WrapText = v;` |
| `S.HorizontalAlignment / VerticalAlignment = Excel…Alignment.X;` (literal) | `S.Alignment.Horizontal / Vertical = XLAlignment…Values.X;` |
| `S.Border.Top/Bottom/Left/Right.Style = ExcelBorderStyle.X;` (literal) | `S.Border.TopBorder/… = XLBorderStyleValues.X;` |

**Out of scope (Manual, typed reason)**: `UnmappedEPPlusStyle` for any other property, call or non-literal enum value (`UnderLine`,
`BorderAround`, a variable alignment); `FillWithoutSolidPattern` for a fill pattern or background colour without its partner statement.
The receiver in front of `.Style` (rows 3–4) is not touched.

## Design

`StylesPattern.Find` returns every expression statement whose assignment target or call starts from an `ExcelStyle`
(`StylesMapping.Split`). `Rewrite` maps the path after `S`; enum values must be a literal EPPlus enum member whose name exists in the
ClosedXML enum. Every rewritten statement carries `StylesPattern.MigratedAnnotationKind`, so `epplus.unsupported` can skip it once it
looks for the annotation (claim request: today it still names `ExcelRangeBase.Style` because the receiver is EPPlus).

**Behaviour proof (E1, E14).** Each Fixed shape has a full proof in `Proofs/EPPlus/Styles/_Combined/`: the real `EPPlusFix` output
(`StylesCombinedProofTests`), which the harness compares with its before (`EPPlusHarnessProofTests`). `StylesHarnessTests` changes each
after's style (value, colour, enum member or border side) and requires the harness to report a difference.

## Requirements

| REQ | Requirement |
| --- | ----------- |
| REQ-1 | Every Fixed shape in the table writes the same workbook (15 harness cases). |
| REQ-2 | Every harness case fails when its style is wrong. |
| REQ-3 | Unmapped properties, calls and non-literal values are Manual `UnmappedEPPlusStyle` (proofs `underline`, `border-around`, `alignment-variable`). |
| REQ-4 | A fill pattern or colour without its partner is Manual `FillWithoutSolidPattern` (proof `fill-without-color`, `FillPatternWithoutColor_IsManual`). |
| REQ-5 | Rewritten statements carry the migrated annotation; with the whole fix and an EPPlus receiver the solution stays as it is (all or nothing). |
| REQ-6 | The existing `Unsupported` proofs still pass. |

## See also

- [verify](epplus-styles-verify.md), [modellen](epplus-styles-modellen.md)
- `claude/tasks/epplus-queue.md` row 8, `claude/tasks/TAAK-309-epplus-to-closedxml.md`
