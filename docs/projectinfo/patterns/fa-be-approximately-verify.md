# Verification: fa-be-approximately

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-approximately-plan.md`](fa-be-approximately-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeApproximatelyPattern.cs`; registratie in `FluentAssertionsFix.cs` (ook als schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeApproximately/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple` |
| REQ-2 | Gehaald | `because`, `close-to` |
| REQ-3 | Gehaald | `not` |
| REQ-4 | Gehaald | `close-to` |
| REQ-5 | Gehaald | `nullable` |
| REQ-6 | Gehaald | `int-close-to`, `nullable-not`, `because-args`, `assertion-scope` |
| REQ-7 | Gehaald | `AndChain/equal-length-approximately` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (474 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-be-approximately-modellen.md](fa-be-approximately-modellen.md)
- `claude/docs/patroon-loop.md`
