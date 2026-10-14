# Verification: mediatr-unsupported

|               |                                                                                        |
| ------------- | -------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-unsupported-plan.md`](mediatr-unsupported-plan.md) |
| **Status**    | Verified                                                                               |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                    |

> Verified by an AI agent against the code at uncommitted changes (basis c461a14).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/UnsupportedPattern.cs`, registration and `SwappedTypes` (`IStreamRequest`1`, `IStreamRequestHandler`2`) in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/Unsupported/`; stream handler: `SwappedTypesTests.FixAsync_StreamRequestHandler_CompilesAgainstMediator`.

| REQ   | Verdict | Evidence                   |
| ----- | ------- | ------------------------ |
| REQ-1 | Dropped | own pattern `mediatr.request-processor` (TAAK-061) |
| REQ-2 | Dropped | own pattern `mediatr.request-processor` (TAAK-061) |
| REQ-3 | Met (only `IRequestExceptionAction`) | `exception-action`; `IRequestExceptionHandler` → `mediatr.exception-handler` |
| REQ-4 | Met | `notification-publisher` |
| REQ-5 | Dropped | own pattern `mediatr.stream-pipeline-behavior` (TAAK-062) |
| REQ-6 | Met | `generic-handler`, `derived-generic-message` |

Scan on SampleShop: 6 Manual (pre and post processor, stream behavior, exception handler, `CountQuery<TEntity>`, `CountHandler<TEntity>`). Solved by hand with this advice on a copy, SampleShop builds and 38/38 tests pass (TAAK-037).

## Deviations

- Row 7 named `IStreamRequest` handlers and `Send(object)` as Manual; both have a twin in Mediator and are therefore not Manual.

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 458/458 green                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green                             |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## Updated (TAAK-061/062, review B M5, 2026-10-11)

- Pre/post processors, exception handlers and stream behaviors are patterns of their own (`mediatr.request-processor`, `mediatr.exception-handler`, `mediatr.stream-pipeline-behavior`); proofs `pre-processor`, `post-processor`, `stream-behavior` removed, `exception-handler` → `exception-action`.
- M5: advice only on the type that declares the interface itself; a generic message is also reported if `IRequest` comes via a base type (`AllInterfaces`, proof `derived-generic-message`).

## Updated (TAAK-132/133, corpus run, 2026-10-11)

- TAAK-132: an **abstract** generic type (`CommandBase<TResult> : IRequest<TResult>`) is not a generic message: the generator only registers concrete types (checked with Mediator 3.0.2). Proof `RequestHandler/edge-abstract-generic-base` (builds with the generator).
- TAAK-133: a type in own code whose direct base type or interface comes from **metadata** (a package, not MediatR/MediatR.Contracts itself) and implements MediatR → Manual "`<package>` builds on MediatR …". A project in the solution is a source reference and is migrated itself. No proof possible (second assembly): `UnsupportedPatternTests`. ardalis 9.1: 11 Manual, all-or-nothing leaves the solution intact.

## See also

- [mediatr-unsupported-plan.md](mediatr-unsupported-plan.md)
- [mediatr-unsupported-modellen.md](mediatr-unsupported-modellen.md)
