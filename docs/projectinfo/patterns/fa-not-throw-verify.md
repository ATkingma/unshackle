# Verification: fa-not-throw

|               |                                                                          |
| ------------- | ------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-throw-plan.md`](fa-not-throw-plan.md) |
| **Status**    | Verified                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc skill                                            |

> Verified by an AI agent against the code at uncommitted changes (basis 41ff4a3).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `simple.*.cs` |
| REQ-2 | Gehaald | `func.*.cs` |
| REQ-3 | Gehaald | `value-func.*.cs` |
| REQ-4 | Gehaald | `because.*.cs` |
| REQ-5 | Vervangen | door Uitbreiding TAAK-097: `because-args` is nu Fixed |
| REQ-6 | Gehaald | `generic.*`, `after.*` |
| REQ-7 | Gehaald | `value-used.*`, `result-used.*` |
| REQ-8 | Vervangen | door Uitbreiding Invoking; `NotThrow/invoking.*` bestaat niet |
| TAAK-097 | Gehaald | `because-args.after.cs` |
| Invoking | Gehaald | `Invoking/not-throw.*` (Fixed); `Invoking/side-effect-receiver`, `value-type`, `method-group` (Manual) |
| TAAK-100 | Gehaald | `IsInsideAssertionScope` als eerste controle in de code (proof alleen voor Throw: `Invoking/assertion-scope`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |
| 2 | Scope | _(edge case)_ | Expression-bodied void-leden worden herschreven (TAAK-040, `edge-expression-bodied.*`) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Test  | `dotnet test`  | passed (108 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (15 tests) |

## Zie ook

- `claude/docs/patroon-loop.md`
