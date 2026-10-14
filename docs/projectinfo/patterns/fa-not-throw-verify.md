# Verification: fa-not-throw

|               |                                                                          |
| ------------- | ------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-throw-plan.md`](fa-not-throw-plan.md) |
| **Status**    | Verified                                                                 |
| **Generated** | 2026-10-11 by KingmaDoc skill                                            |

> Verified by an AI agent against the code at uncommitted changes (basis 41ff4a3).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/`, run by `ProofTests`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `simple.*.cs` |
| REQ-2 | Met | `func.*.cs` |
| REQ-3 | Met | `value-func.*.cs` |
| REQ-4 | Met | `because.*.cs` |
| REQ-5 | Replaced | by Extension TAAK-097: `because-args` is nu Fixed |
| REQ-6 | Met | `generic.*`, `after.*` |
| REQ-7 | Met | `value-used.*`, `result-used.*` |
| REQ-8 | Replaced | by Extension Invoking; `NotThrow/invoking.*` does not exist |
| TAAK-097 | Met | `because-args.after.cs` |
| Invoking | Met | `Invoking/not-throw.*` (Fixed); `Invoking/side-effect-receiver`, `value-type`, `method-group` (Manual) |
| TAAK-100 | Met | `IsInsideAssertionScope` as the first check in the code (proof `NotThrow/assertion-scope`) |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; a documented difference | Low |
| 2 | Scope | _(edge case)_ | Expression-bodied void members are rewritten (TAAK-040, `edge-expression-bodied.*`) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Test  | `dotnet test`  | passed (108 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (15 tests) |

## See also

- `claude/docs/patroon-loop.md`
