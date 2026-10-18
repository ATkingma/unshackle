# Verification: mediatr-send-as-task

|               |                                                                                          |
| ------------- | ---------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-send-as-task-plan.md`](mediatr-send-as-task-plan.md) |
| **Status**    | Verified                                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                      |

> Verified by an AI agent against the code at uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/SendAsTaskPattern.cs`, registration in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/SendAsTask/`.

| REQ   | Verdict | Evidence                                        |
| ----- | ------- | ----------------------------------------------- |
| REQ-1 | Met     | `task-uses`                                     |
| REQ-2 | Met     | `task-uses` (await and `.ConfigureAwait(false)`) |
| REQ-3 | Met     | `send-object`                                   |
| REQ-4 | Met     | `Rewrite` returns `Fixed`, except for `?.` (extension) and unknown NSubstitute setup values (REQ-5) |
| REQ-5 | Met     | `nsubstitute-returns` (after compiles against Mediator + NSubstitute), `nsubstitute-returns-lambda` (Manual), `SendAsTaskPatternTests`; corpus dotnet/eShop round 4 |
| REQ-6 | Met     | `nsubstitute-returns` (`Throws`, `Received().Send`), `SendAsTaskPatternTests.Find_NSubstituteThrowsSetupAndReceivedCheck_FindsNothing` |
| REQ-7 | Met     | `expression-tree`, `Find_SendInsideExpressionTree_FindsNothing` |
| Extension review B L4 | Met | `null-conditional` |

Scan on SampleShop: 5 Fixed (`OrderFacade` 2×, `Func<Task> act = () => Sender.Send(...)` 3× in the test projects).

## Deviations

None.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 458/458 green                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green                             |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## Updated (review B L4, 2026-10-11)

- `_sender?.Send(x)` (MemberBinding) → Manual (proof `null-conditional`).

## Updated (TAAK-211, 2026-10-13)

- NSubstitute setups: `Returns` values to `ValueTask`, `Throws*`/`Received()` untouched, the rest Manual.

## See also

- [mediatr-send-as-task-plan.md](mediatr-send-as-task-plan.md)
- [mediatr-send-as-task-modellen.md](mediatr-send-as-task-modellen.md)
