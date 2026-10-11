# Verification: fa-throw-exactly

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-throw-exactly-plan.md`](fa-throw-exactly-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis d9d8f05).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowExactlyPattern.cs` (claimt de nodes), herschrijving in `ThrowPattern`, `ThrowAsyncPattern`, `NotThrowPattern` en `ThrowMessage`; registratie in `FluentAssertionsFix.cs` (`s_assertions`, dus ook schakel van `fa.and-chain`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowExactly/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `simple` |
| REQ-2 | Gehaald | `because` |
| REQ-3 | Gehaald | `message, async-message` |
| REQ-4 | Gehaald | `async` |
| REQ-5 | Gehaald | `aggregate-wait` |
| REQ-6 | Gehaald | `because-args, assertion-scope, value-used` |

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

- [fa-throw-exactly-modellen.md](fa-throw-exactly-modellen.md)
- `claude/docs/patroon-loop.md`
