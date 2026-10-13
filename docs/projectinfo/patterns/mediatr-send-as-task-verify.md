# Verification: mediatr-send-as-task

|               |                                                                                          |
| ------------- | ---------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-send-as-task-plan.md`](mediatr-send-as-task-plan.md) |
| **Status**    | Verified                                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                      |

> Verified by an AI agent against the code at uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs`, registratie in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/SendAsTask/`.

| REQ   | Oordeel | Bewijs                                          |
| ----- | ------- | ----------------------------------------------- |
| REQ-1 | Gehaald | `task-uses`                                     |
| REQ-2 | Gehaald | `task-uses` (await en `.ConfigureAwait(false)`) |
| REQ-3 | Gehaald | `send-object`                                   |
| REQ-4 | Gehaald | `Rewrite` geeft `Fixed`, behalve bij `?.` (uitbreiding) en onbekende NSubstitute-setupwaarden (REQ-5) |
| REQ-5 | Gehaald | `nsubstitute-returns` (after compileert tegen Mediator + NSubstitute), `nsubstitute-returns-lambda` (Manual), `SendAsTaskPatternTests`; corpus dotnet/eShop ronde 4 |
| REQ-6 | Gehaald | `nsubstitute-returns` (`Throws`, `Received().Send`), `SendAsTaskPatternTests.Find_NSubstituteThrowsSetupAndReceivedCheck_FindsNothing` |
| Uitbreiding review B L4 | Gehaald | `null-conditional` |

Scan op SampleShop: 5 Fixed (`OrderFacade` 2×, `Func<Task> act = () => Sender.Send(...)` 3× in de testprojecten).

## Deviations

Geen.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 458/458 groen                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 groen                             |
| Lint    | _geen_                                                      | _not run_: geen lint-commando ingesteld |

## Bijgewerkt (review B L4, 2026-10-11)

- `_sender?.Send(x)` (MemberBinding) → Manual (proof `null-conditional`).

## Bijgewerkt (TAAK-211, 2026-10-13)

- NSubstitute-setups: `Returns`-waarden naar `ValueTask`, `Throws*`/`Received()` ongemoeid, rest Manual.

## Zie ook

- [mediatr-send-as-task-plan.md](mediatr-send-as-task-plan.md)
- [mediatr-send-as-task-modellen.md](mediatr-send-as-task-modellen.md)
