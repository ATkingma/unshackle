# Verification: fa-catch-assertion-exception

|               |                                                                                                          |
| ------------- | -------------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-catch-assertion-exception-plan.md`](fa-catch-assertion-exception-plan.md) |
| **Status**    | Verified                                                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc                                                                                  |

> Verified by an AI agent against uncommitted changes (basis 3bb7dcf).

| REQ   | Verdict | Evidence                                                                          |
| ----- | ------- | --------------------------------------------------------------------------------- |
| REQ-1 | Met     | proofs `CatchAssertionException/retry-filter`, `when-filter`                      |
| REQ-2 | Met     | proof `CatchAssertionException/catch-clause`                                      |
| REQ-3 | Met     | proof `catch-uses-exception`; unit test `Rewrite_IsPatternWithDesignation_Manual` |
| REQ-4 | Met     | unit test `Rewrite_FilterOnFluentAssertionsAndXunit_AddsShouldlyOnce` (Polly form) |
| REQ-5 | Met     | unit test `Find_SiteAlreadyCatchesShouldly_FindsNothing`                          |

## Deviations

| #   | Area       | Plan says       | Code does                                                                                                                 | Impact                                      |
| --- | ---------- | --------------- | ------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| 1   | ProofTests | —               | An `after` that still imports a removed namespace compiles against old + new library (as already with Manual)            | None                                        |
| 2   | Corpus     | Harness on Polly | Not run: no unchanged Polly source in the corpus (only the converted `out/polly/work`)                                   | Evidence via a unit test on exactly the Polly line |
