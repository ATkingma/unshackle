# Verification: fa-have-length

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-have-length-plan.md`](fa-have-length-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveLengthPattern.cs`; registratie in `FluentAssertionsFix.cs` (ook als schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/HaveLength/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple` |
| REQ-2 | Gehaald | `because` |
| REQ-3 | Gehaald | `nullable` |
| REQ-4 | Gehaald | `receiver-parens` |
| REQ-5 | Gehaald | `stream`, `because-args`, `assertion-scope` |
| REQ-6 | Gehaald | `AndChain/equal-length-approximately` |

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

- [fa-have-length-modellen.md](fa-have-length-modellen.md)
- `claude/docs/patroon-loop.md`
