# Verification: fa-contain-key

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-key-plan.md`](fa-contain-key-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs`; registration in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/`, run by `ProofTests`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `dictionary.*.cs` |
| REQ-2 | Met | `idictionary.*.cs` (after compiles against Shouldly) |
| REQ-3 | Met | `not-contain-key.*.cs` |
| REQ-4 | Met | `because.*.cs` |
| REQ-5 | Met | `lambda.*.cs` |
| REQ-6 | Replaced | By Extension TAAK-097: `because-args` is now Fixed |
| REQ-7 | Met | `chain.*`, `which.*`, `and-chain.*` |
| REQ-8 | Met | `keys.*`, `not-contain-keys.*` |
| REQ-9 | Met | `contain-value.*`, `contain-values.*`, `not-contain-value.*` |
| REQ-10 | Met | `nullable.*` |
| REQ-11 | Met | `read-only.*`, `read-only-not-contain.*` (after compileert; `ContainsKey(..).ShouldBeTrue/False`) |
| REQ-12 | Met | `headers.*` |
| REQ-13 | Met | `enumerable-pairs.manual.txt` |
| Extension TAAK-097 | Met | `because-args.before.cs` / `because-args.after.cs` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 2 | Behaviour | Equal to FA | `HttpHeaders.Contains` is case-insensitive; FA compares exactly | Low (positive assertion slightly looser, only for headers) |
| 1 | Behaviour | Error message equal to FA | Shouldly puts "Additional Info:" before the `because`; a documented difference | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | `ProofTests` 685/685 green (TAAK-168); full run: only the known CRLF failures |
| Lint  | _none_         | _not run_: no lint command configured   |

## See also

- [fa-contain-key-modellen.md](fa-contain-key-modellen.md)
