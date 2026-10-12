# Verification: mediatr-request-handler-delegate

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-request-handler-delegate-plan.md`](mediatr-request-handler-delegate-plan.md) |
| **Status**    | Verified (proofs en unittests); corpusbewijs op amantin nog niet gedraaid |
| **Generated** | 2026-10-12 (met de hand, kingmadoc ontbreekt) |

## Requirements

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `Proofs/MediatR/_Combined/request-handler-delegate` (`CombinedProofTests.CombinedMediatR_Before_BecomesAfter`; `after` compileert tegen Mediator 3.0.2 mét generator) |
| REQ-2 | Gehaald | `RequestHandlerDelegate/lambda`, `parameter`, `invoke-call`, `callback-returns` |
| REQ-3 | Gehaald | `RequestHandlerDelegate/no-handle-call` |
| REQ-4 | Gehaald | `FixerProjectFileTests.FixAsync_CodeStillUsesPackageWithoutFinding_ReportsManualOnThatLine` en `..._ReportsNoUnmatchedUse` |

## Deviations

- MediatR 12.5's `RequestHandlerDelegate<T>` heeft een optionele `CancellationToken`; `Invoke(token)` houdt die als tweede argument.
- Het vangnet slaat bestanden over waar al een Manual-finding staat (anders ruis per cascade-Manual); `FixerProjectFileTests.FixAsync_CodeStillUsesPackage_KeepsItAndReportsManual` filtert daarom op `project-file`.
- Lambda's als `next` (`behavior.Handle(cmd, () => ..., ct)`) zijn niet gedekt: geen symboolverwijzing, dus ook geen vangnetrij.
- Stap 4 van TAAK-180 (harnas op amantin: 0 Manual, build, 85/85) is niet gedraaid: het corpusproject staat niet in deze omgeving. Nabootsing: de `_Combined`-proof.

## Zie ook

- [mediatr-request-handler-delegate-plan.md](mediatr-request-handler-delegate-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md` — run 2026-10-12
