# Verification: fa-invoking

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-invoking-plan.md`](fa-invoking-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis d9d8f05).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/InvokingPattern.cs` (claimt de nodes), herschrijving in `ThrowPattern`, `ThrowAsyncPattern`, `NotThrowPattern` en `ThrowMessage`; registratie in `FluentAssertionsFix.cs` (`s_assertions`, dus ook schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Invoking/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple, func` |
| REQ-2 | Gehaald | `because-message` |
| REQ-3 | Gehaald | `throw-exactly, awaiting-exactly` |
| REQ-4 | Gehaald | `not-throw` |
| REQ-5 | Gehaald | `awaiting` |
| REQ-6 | Gehaald | `side-effect-receiver, value-type, method-group` |
| REQ-7 | Gehaald | `because-args, assertion-scope, aggregate` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| - | - | - | - | - |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Test | `dotnet test` | passed (515 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests) |

## Zie ook

- [fa-invoking-modellen.md](fa-invoking-modellen.md)
- `claude/docs/patroon-loop.md`
