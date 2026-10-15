# Verification: mediatr-request-handler

|               |                                                                                                |
| ------------- | ---------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/mediatr-request-handler-plan.md`](mediatr-request-handler-plan.md) |
| **Status**    | Verified                                                                                       |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                                                            |

> Verified by an AI agent against the code at uncommitted changes (basis ed2e155).

## Requirements

Code: `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerPattern.cs`, registration in `MediatRFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/`.

| REQ   | Verdict | Evidence                                              |
| ----- | ------- | ----------------------------------------------------- |
| REQ-1 | Met     | `async`                                               |
| REQ-2 | Met     | `from-result-expression`                              |
| REQ-3 | Met     | `from-result-block` (also `Task.FromResult<int>(...)`) |
| REQ-4 | Met     | `generic`                                             |
| REQ-5 | Met     | `token-name`                                          |
| REQ-6 | Met     | `other-return`                                        |
| REQ-7 | Met     | `explicit`, `explicit-async`                          |
| REQ-7 | Met     | `decorator-inner-handle` (TAAK-212)                   |
| Extension TAAK-080 | Met | `edge-marker-interface` |

Regression edges: `edge-nested-generic` (nested generic response for REQ-1), `edge-primary-ctor` (primary constructor for REQ-2).

Scan on SampleShop (`scan samples/SampleShop/SampleShop.slnx --fix mediatr`, after `dotnet restore`): `mediatr.request-handler` 5 Fixed, 0 Manual (`CountQuery`, `RegisterCustomerCommand`, `GetOrderHandler`, `ListOrdersHandler`, `PlaceOrderHandler`); `project-file` Manual for `SampleShop.csproj` because MediatR is still used there (notification handler, `AddMediatR`: later patterns).

## Deviations

| #   | Area  | Plan says | Code does                                                                                                                                                                         | Impact                                    |
| --- | ----- | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------- |
| 1   | Tests | —         | `ProofTests` now also gives the fix substitute `SwappedTypes` (needed for `using MediatR` → `using Mediator`); empty for `*.manual.txt` proofs, so those only test the pattern | None for FA proofs (`SwappedTypes` empty) |

## Validation results

| Check   | Command                                                     | Result                                  |
| ------- | ----------------------------------------------------------- | --------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings)                     |
| Tests   | `dotnet test`                                               | 318/318 green                           |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 green                             |
| Lint    | _none_                                                      | _not run_: no lint command configured   |

## Follow-up (2026-10-11)

TAAK-080: handlers via an own marker interface (`IAppQueryHandler<TQ, TR> : IRequestHandler<TQ, TR>`) are found: `MediatRSyntax.FindHandleMethods` looks at `AllInterfaces`. Proof `edge-marker-interface`. `mediatr.unsupported` skips interface declarations (a generic marker interface is not a generic handler).

End-to-end (`fix --fix mediatr` on a copy of SampleShop, after restore): 20 Fixed, 8 Manual; build without CS0436 (only MSG0003/MSG0005 from the generic `CountQuery`). Manual: pre/post-processor, exception handler, stream behavior (TAAK-061/062), generic `CountQuery`, and as a result `AddMediatR` (`AddOpenStreamBehavior`). Because the registration stays MediatR, 28/38 sample tests fail at startup (`LoggingBehavior`2 must implement MediatR.IPipelineBehavior`2`); this resolves with TAAK-061/062.

## Follow-up TAAK-212 (2026-10-13)

REQ-7: `return _decorated.Handle(...)` stays instead of `new ValueTask<T>(...)` (CS1503 in kgrzybek). Build `dotnet build` ok; `dotnet test` 1062/1062 green. Corpus kgrzybek: see `claude/tasks/CORPUSRUN-mediatr.md` "Herhaling ronde 5".

## See also

- [mediatr-request-handler-plan.md](mediatr-request-handler-plan.md)
- [mediatr-request-handler-modellen.md](mediatr-request-handler-modellen.md)
