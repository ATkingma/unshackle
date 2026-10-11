# Verification: fa-catch-assertion-exception

|               |                                                                                                          |
| ------------- | -------------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-catch-assertion-exception-plan.md`](fa-catch-assertion-exception-plan.md) |
| **Status**    | Verified                                                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc                                                                                  |

> Verified by an AI agent against uncommitted changes (basis 3bb7dcf).

| REQ   | Oordeel | Bewijs                                                                            |
| ----- | ------- | --------------------------------------------------------------------------------- |
| REQ-1 | Gehaald | proofs `CatchAssertionException/retry-filter`, `when-filter`                      |
| REQ-2 | Gehaald | proof `CatchAssertionException/catch-clause`                                      |
| REQ-3 | Gehaald | proof `catch-uses-exception`; unittest `Rewrite_IsPatternWithDesignation_Manual`  |
| REQ-4 | Gehaald | unittest `Rewrite_FilterOnFluentAssertionsAndXunit_AddsShouldlyOnce` (Polly-vorm) |
| REQ-5 | Gehaald | unittest `Find_SiteAlreadyCatchesShouldly_FindsNothing`                           |

## Deviations

| #   | Area       | Plan says       | Code does                                                                                                                 | Impact                                      |
| --- | ---------- | --------------- | ------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| 1   | ProofTests | —               | Een `after` die een verwijderde namespace nog importeert compileert tegen oude + nieuwe bibliotheek (zoals al bij Manual) | None                                        |
| 2   | Corpus     | Harnas op Polly | Niet gedraaid: geen ongewijzigde Polly-bron in het corpus (alleen de omgezette `out/polly/work`)                          | Bewijs via unittest op exact de Polly-regel |
