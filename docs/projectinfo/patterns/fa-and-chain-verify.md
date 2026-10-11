# Verification: fa-and-chain

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-and-chain-plan.md`](fa-and-chain-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis b53e3cc).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple`, `three-links`, `be-of-type`, `edge-multiline` |
| REQ-2 | Gehaald | `because` |
| REQ-3 | Gehaald | `member-subject`, `nullable` |
| REQ-4 | Gehaald | `subject-call`, `subject-indexer` |
| REQ-5 | Gehaald | `unsupported-link`, `link-manual` |
| REQ-6 | Gehaald | `used`, `which`, `no-block` |
| REQ-7 | Gehaald | `assertion-scope` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| 1 | Code | Alleen eigen bestanden | `FluentAssertionsFix`: assertion-patronen in `s_assertions`, gedeeld met `AndChainPattern`; volgorde ongewijzigd | Low |
| 2 | Proof | - | `.And`-typecheck zonder proof: geen compileerbaar FA 7.2.2-voorbeeld gevonden | Low |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (412 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-and-chain-modellen.md](fa-and-chain-modellen.md)
- `claude/docs/patroon-loop.md`
