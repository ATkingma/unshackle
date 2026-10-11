# Verification: fa-contain-single-which

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-single-which-plan.md`](fa-contain-single-which-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis beaa9c6).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs`; registratie in `FluentAssertionsFix.cs` (ook als schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ContainSingle/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple` |
| REQ-2 | Gehaald | `predicate`, `enumerable` |
| REQ-3 | Gehaald | `because` |
| REQ-4 | Gehaald | `which` |
| REQ-5 | Gehaald | `nullable` |
| REQ-6 | Gehaald | `which-chain`, `expression-variable`, `because-args`, `assertion-scope` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (432 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-contain-single-which-modellen.md](fa-contain-single-which-modellen.md)
- `claude/docs/patroon-loop.md`
