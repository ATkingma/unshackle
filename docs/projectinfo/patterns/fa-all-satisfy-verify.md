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
| REQ-12 | Deels   | Handmatig: `unshackle fix` op een kopie van `samples/` gaf `ShouldNotBeEmpty(); foreach (var o in orders) { o.Total.ShouldBe(20m); }`. Geen geautomatiseerde test (proofs draaien één pattern en de after moet tegen Shouldly compileren) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Loop stopt bij het eerste mislukte element; FA toont alle indexen | Low |
| 2 | Verification | REQ-12 als test | Alleen handmatig bewezen | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | AllSatisfy-proofs (13) groen; totaal 236/237, de enige fout is een proof van een ander patroon (`BeEquivalentToObject/null-expectation`, niet van mij) |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |

## Zie ook

- [fa-all-satisfy-modellen.md](fa-all-satisfy-modellen.md)
