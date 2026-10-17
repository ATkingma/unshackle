# Verification: fa-custom-assertion

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-custom-assertion-plan.md`](fa-custom-assertion-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc                                                                |

> Verified by an AI agent against uncommitted changes (basis bcf1156); REQ-6..10 against branch worktree-agent-a8433c56bf106c4f6 (TAAK-275).

| REQ   | Verdict | Evidence                                                                                |
| ----- | ------- | ------------------------------------------------------------------------------------- |
| REQ-1 | Met | proof `CustomAssertion/extension`                                                     |
| REQ-2 | Met | proof `CustomAssertion/subclass`                                                      |
| REQ-3 | Met | `IsFluentAssertions(method)` as the first check; all existing FA proofs stay green |
| REQ-4 | Met | unittest `ReportWriterTests.FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount` |
| REQ-5 | Met | by hand on corpus FluentResults: 7 extensions (`BeFailure` 31x, `HaveReason` 25x, …) |
| Extension | Met | proof `CustomAssertion/in-assertion-scope` (reason: Custom assertion `ScopedResultAssertions.BeFailure`) |
| REQ-6 | Met | proof `CustomAssertion/reference-type` (after compiles against Shouldly); SampleShop `OrderAssertions` converted, sample tests 38/38 (TAAK-275) |
| REQ-7 | Met | proof `CustomAssertion/reference-type`, `_Combined/custom-assertion-class` (holder class removed) |
| REQ-8 | Met | proof `_Combined/custom-assertion-class` (`BeCancelled(..).And.NotBeNull()` → `ShouldBeCancelled(..); ShouldNotBeNull()`) and `reference-type` (two own links) |
| REQ-9 | Met | proofs `CustomAssertion/given-then`, `CustomAssertion/class-in-assertion-scope`; corpus: PowerApps output identical to main, evolutionary ch. 4 ErrorOr class stays Manual (no call in the solution) |
| REQ-10 | Met | `CombinedProofTests.AllSatisfyPattern_IsFirstAfterAssertionScope` |

## Deviations

| #   | Area        | Plan says | Code does                                                                | Impact |
| --- | ----------- | --------- | ------------------------------------------------------------------------ | ------ |
| 1   | Registration | —         | Since TAAK-275 first in `FluentAssertionsFix.Patterns` (REQ-10)          | None   |
| 2   | Scope | REQ-6 lists the accepted shapes | The analyzer also accepts a primary constructor that only passes the subject, and hoists FailWith arguments into locals before the throw | None: same evaluation order |
