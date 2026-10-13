# Verification: fa-timespan-extensions

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-timespan-extensions-plan.md`](fa-timespan-extensions-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-13 by KingmaDoc                                                                      |

> Verified by an AI agent against uncommitted changes (basis 04f113b). `dotnet build` groen, `dotnet test` 1109/1109 groen.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | proof `TimeSpanExtensions/simple` (int, double, berekend, negatief, alle zes eenheden) |
| REQ-2 | Gehaald | proof `static-call` |
| REQ-3 | Gehaald | proof `no-using-system`; unittest `Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan` |
| REQ-4 | Gehaald | proof `month` |
| REQ-5 | Gehaald | proof `month-computed` |
| REQ-6 | Gehaald | proofs `and`, `offset`; unittest `Rewrite_DateTimeAt_Manual` |
| REQ-7 | Gehaald | unittest `Find_OwnSecondsExtension_FindsNothing` |

## Deviations

| #   | Area     | Plan says | Code does | Impact |
| --- | -------- | --------- | --------- | ------ |
| 1   | Usings   | using weg | De Fixer zet `using FluentAssertions.Extensions;` om naar `using Shouldly;` (in-place swap), ook zonder asserts; zonder `using System` komt die erna, ongesorteerd | Cosmetisch |
| 2   | Corpus   | —         | Zie `claude/tasks/CORPUSRUN-fa.md`, ronde 4 → "Herhaling TAAK-224/225" | — |
