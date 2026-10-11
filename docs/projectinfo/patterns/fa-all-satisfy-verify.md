# Verification: fa-all-satisfy

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-all-satisfy-plan.md`](fa-all-satisfy-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AllSatisfyPattern.cs`; registratie (eerste in de lijst) in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/AllSatisfy/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `simple.*.cs` |
| REQ-2  | Gehaald | `block.*.cs` |
| REQ-3  | Gehaald | `typed-parameter.*.cs` |
| REQ-4  | Gehaald | `member-subject.*.cs` |
| REQ-5  | Gehaald | `because.before.cs` / `.manual.txt` |
| REQ-6  | Gehaald | `because-args.*` |
| REQ-7  | Gehaald | `method-group.*` |
| REQ-8  | Gehaald | `return.*`, `async.*` |
| REQ-9  | Gehaald | `subject-call.*` |
| REQ-10 | Gehaald | `used.*`, `and-chain.*` |
| REQ-11 | Gehaald | `not-in-block.*` |
| REQ-12 | Gehaald | `CombinedProofTests` met `_Combined/all-satisfy-be.*` (het sample-geval: `ShouldNotBeEmpty(); foreach (var o in orders) { o.Total.ShouldBe(20m); }`) en `all-satisfy-other.*`; de volgorde (AllSatisfy eerst) wordt vastgelegd door `CombinedProofTests.AllSatisfyPattern_IsFirstInFix`. Eerder alleen handmatig bewezen |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Loop stopt bij het eerste mislukte element; FA toont alle indexen | Low |

## Validation results

REQ-12 is sinds TAAK-018 geautomatiseerd; afwijking 2 (alleen handmatig bewezen) is daarmee vervallen.

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | AllSatisfy-proofs (13) groen; totaal 236/237, de enige fout is een proof van een ander patroon (`BeEquivalentToObject/null-expectation`, niet van mij) |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |

## Zie ook

- [fa-all-satisfy-modellen.md](fa-all-satisfy-modellen.md)
