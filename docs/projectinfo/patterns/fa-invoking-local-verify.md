# Verification: fa-invoking-local

|               |                                                                                      |
| ------------- | ------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-invoking-local-plan.md`](fa-invoking-local-plan.md) |
| **Status**    | Verified                                                                             |
| **Generated** | 2026-10-18 by KingmaDoc skill                                                        |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/FluentAssertions/InvokingLocal/simple.*`, unit test `Rewrite_VarLocal_WritesTheReturnedDelegateType` |
| REQ-2 | Met | `InvokingLocal/side-effect-receiver.*`, unit test `Rewrite_ReceiverWithSideEffects_IsManual` |
| REQ-3 | Met | `_Combined/invoking-local.*`; corpus numbers in `claude/tasks/archief/TAAK-287-fa-residual-round-2.md` |

## Deviations

None.

## Validation results

See the task file (`dotnet build`, `dotnet test`, sample tests via `claude/tools/verify-head.sh`).

## See also

- [fa-invoking-local-modellen.md](fa-invoking-local-modellen.md)
