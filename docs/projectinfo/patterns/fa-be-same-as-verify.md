# Verification: fa-be-same-as

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-same-as-plan.md`](fa-be-same-as-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis bcf1156).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeSameAsPattern.cs`; registratie in `FluentAssertionsFix.cs` (`s_assertions`, ook als schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeSameAs/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple, nullable` |
| REQ-2 | Gehaald | `not` |
| REQ-3 | Gehaald | `because` |
| REQ-4 | Deels vervangen | `result-used, assertion-scope` (Manual); `because-args` is nu Fixed (TAAK-097) |
| REQ-5 | Gehaald | `AndChain/be-same-as` |
| Uitbreiding TAAK-097 | Gehaald | `because-args.after.cs` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (579 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-be-same-as-plan.md](fa-be-same-as-plan.md)
- [fa-be-same-as-modellen.md](fa-be-same-as-modellen.md)
- `claude/docs/patroon-loop.md`
