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
| REQ-4 | Vervangen | door REQ-8 (TAAK-098): `subject-call`, `subject-indexer` zijn nu Fixed |
| REQ-5 | Gehaald | `unsupported-link`; Manual-schakel zonder eigen proof (`link-manual` bestaat niet meer, `link-because-args` is sinds TAAK-097 Fixed) |
| REQ-6 | Gehaald | `used`, `which`, `no-block` |
| REQ-7 | Gehaald | `assertion-scope` |
| REQ-8 | Gehaald | `subject-call`, `subject-indexer`, `subject-call-twice` (`subject`, `subject2`) |
| REQ-9 | Gehaald | `be-same-as`, `dates`, `equal-length-approximately`, `equivalent-collection`, `greater-less`, `link-because-args` |
| REQ-10 | Gehaald | `contain-keys`, `contain-keys-and`, `not-contain-keys`, `contain-keys-read-only`, `contain-keys-subject-call` (`subject2`) |
| REQ-11 | Gehaald | `contain-value`, `contain-value-because`, `not-contain-value`, `contain-value-read-only`, `contain-values`, `not-contain-values`, `contain-keys-values` (Polly-geval) |
| REQ-12 | Gehaald | `whose-value`, `whose-value-because`, `whose-value-other`, `whose-value-and`, `whose-value-read-only`, `whose-value-key-variable`, `whose-value-subject-call` |
| REQ-13 | Gehaald | `contain-keys-collection`, `contain-values-collection`, `whose-value-no-indexer`, `whose-value-computed-key` |
| REVIEW-A | Gehaald | `greater-less`, `lambda` (verhuisd uit BeGreaterLess); `AndChainPatternTests`: de `Warning` van een schakel blijft staan (H2) en een schakel van meerdere statements (OnlyContain) wordt overgenomen (L3) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| 1 | Code | Alleen eigen bestanden | `FluentAssertionsFix`: assertion-patronen in `s_assertions`, gedeeld met `AndChainPattern`; volgorde ongewijzigd | Low |
| 2 | Proof | - | `.And`-typecheck zonder proof: geen compileerbaar FA 7.2.2-voorbeeld gevonden | Low |
| R-A | Performance | `Find` over het hele bestand per schakel | REVIEW-A M6: `Find` draait op de ouder van het schakelstatement | Low |
| 3 | Docs | Eigen patroon voor TAAK-171 | TAAK-171 breidt `fa.and-chain` uit (de splitsing is al daar); `ContainKeyPattern` is ongewijzigd | Low |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (412 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-and-chain-modellen.md](fa-and-chain-modellen.md)
- `claude/docs/patroon-loop.md`
