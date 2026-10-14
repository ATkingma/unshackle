# Verification: fa-timespan-extensions

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-timespan-extensions-plan.md`](fa-timespan-extensions-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-13 by KingmaDoc                                                                      |

> Verified by an AI agent against uncommitted changes (basis 04f113b). `dotnet build` green, `dotnet test` 1109/1109 green.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | proof `TimeSpanExtensions/simple` (int, double, computed, negative, all six units) |
| REQ-2 | Met | proof `static-call` |
| REQ-3 | Met | proof `no-using-system`; unit test `Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan` |
| REQ-4 | Met | proof `month` |
| REQ-5 | Met | proof `month-computed` |
| REQ-6 | Met | proofs `and`, `offset`; unit test `Rewrite_DateTimeAt_Manual` |
| REQ-7 | Met | unit test `Find_OwnSecondsExtension_FindsNothing` |

## Deviations

| #   | Area     | Plan says | Code does | Impact |
| --- | -------- | --------- | --------- | ------ |
| 1   | Usings   | using removed | The Fixer converts `using FluentAssertions.Extensions;` to `using Shouldly;` (in-place swap), also without asserts; without `using System` that one comes after it, unsorted | Cosmetic |
| 2   | Corpus   | —         | See `claude/tasks/CORPUSRUN-fa.md`, round 4 → "Herhaling TAAK-224/225" | — |
