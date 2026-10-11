# Verification: fa-custom-assertion

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-custom-assertion-plan.md`](fa-custom-assertion-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc                                                                |

> Verified by an AI agent against uncommitted changes (basis bcf1156).

| REQ   | Oordeel | Bewijs                                                                                |
| ----- | ------- | ------------------------------------------------------------------------------------- |
| REQ-1 | Gehaald | proof `CustomAssertion/extension`                                                     |
| REQ-2 | Gehaald | proof `CustomAssertion/subclass`                                                      |
| REQ-3 | Gehaald | `IsFluentAssertions(method)` als eerste check; alle bestaande FA-proofs blijven groen |
| REQ-4 | Gehaald | unittest `ReportWriterTests.FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount` |
| REQ-5 | Gehaald | handmatig op corpus FluentResults: 7 extensies (`BeFailure` 31x, `HaveReason` 25x, …) |

## Deviations

| #   | Area        | Plan says | Code does                                                                | Impact |
| --- | ----------- | --------- | ------------------------------------------------------------------------ | ------ |
| 1   | Registratie | —         | Eén regel in `FluentAssertionsFix.Patterns` (na `AssertionScopePattern`) | None   |
