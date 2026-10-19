# Verification: mediatr-request-handler-delegate

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-request-handler-delegate-plan.md`](mediatr-request-handler-delegate-plan.md) |
| **Status**    | Verified (proofs and unit tests); corpus evidence on amantin not yet run |
| **Generated** | 2026-10-12 (by hand, kingmadoc missing) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/_Combined/request-handler-delegate` (`CombinedProofTests.CombinedMediatR_Before_BecomesAfter`; `after` compiles against Mediator 3.0.2 with generator) |
| REQ-2 | Met | `RequestHandlerDelegate/method-group`, `lambda-discard-parameter`, `parameter`, `invoke-call`, `callback-returns`, `moq-returns-value` |
| REQ-3 | Met | `RequestHandlerDelegate/no-handle-call` |
| REQ-5 | Met | `Proofs/MediatR/_Combined/request-handler-delegate-shared-field` (before compiles against MediatR 12.5, after against Mediator 3.0.2 with generator; replica of amantin `AuthorizationBehaviorTests`) |
| REQ-6 | Met | `Proofs/MediatR/_Combined/request-handler-delegate-lambda` (block, expression, async and `Task.Run` lambdas; replica of afripay `ValidationBehaviorTests`) |
| REQ-7 | Met | `Proofs/MediatR/_Combined/request-handler-delegate-moq` (replica of cleanmicro `ExceptionEnrichingTests`, Moq stand-in); `moq-returns-value` |
| REQ-8 | Met | `Proofs/MediatR/_Combined/request-handler-delegate-moq` (`Func<Task<string>> act = () => _sut.Handle(..).AsTask()`); corpus cleanmicro `after` 67/67 |
| REQ-4 | Met | `FixerProjectFileTests.FixAsync_CodeStillUsesPackageWithoutFinding_ReportsManualOnThatLine` and `..._ReportsNoUnmatchedUse` |

## Deviations

- MediatR 12.5's `RequestHandlerDelegate<T>` has an optional `CancellationToken`; `Invoke(token)` keeps it as the second argument.
- The safety net skips files that already have a Manual finding (otherwise noise per cascade Manual); `FixerProjectFileTests.FixAsync_CodeStillUsesPackage_KeepsItAndReportsManual` therefore filters on `project-file`.
- Lambdas written inline as `next` (`behavior.Handle(cmd, _ => ..., ct)`) are not covered: no symbol reference, so no safety-net row either.
- Step 4 of TAAK-180 (harness on amantin: 0 Manual, build, 85/85) has not been run: the corpus project is not in this environment. Replica: the `_Combined` proof.

## See also

- [mediatr-request-handler-delegate-plan.md](mediatr-request-handler-delegate-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md` — run 2026-10-12
