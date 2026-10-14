# Verification: fa-custom-assertion

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-custom-assertion-plan.md`](fa-custom-assertion-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc                                                                |

> Verified by an AI agent against uncommitted changes (basis bcf1156).

| REQ   | Verdict | Evidence                                                                                |
| ----- | ------- | ------------------------------------------------------------------------------------- |
| REQ-1 | Met | proof `CustomAssertion/extension`                                                     |
| REQ-2 | Met | proof `CustomAssertion/subclass`                                                      |
| REQ-3 | Met | `IsFluentAssertions(method)` as the first check; all existing FA proofs stay green |
| REQ-4 | Met | unittest `ReportWriterTests.FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount` |
| REQ-5 | Met | by hand on corpus FluentResults: 7 extensions (`BeFailure` 31x, `HaveReason` 25x, …) |
| Extension | Met | proof `CustomAssertion/in-assertion-scope` (reason: Custom assertion `ScopedResultAssertions.BeFailure`) |

## Deviations

| #   | Area        | Plan says | Code does                                                                | Impact |
| --- | ----------- | --------- | ------------------------------------------------------------------------ | ------ |
| 1   | Registratie | —         | Eén regel in `FluentAssertionsFix.Patterns` (na `AssertionScopePattern`) | None   |
