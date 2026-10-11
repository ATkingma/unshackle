# Verification: fa-contain-key

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-contain-key-plan.md`](fa-contain-key-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainKeyPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ContainKey/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `dictionary.*.cs` |
| REQ-2 | Gehaald | `read-only.*.cs`, `idictionary.*.cs` (after compileert tegen Shouldly) |
| REQ-3 | Gehaald | `not-contain-key.*.cs` |
| REQ-4 | Gehaald | `because.*.cs` |
| REQ-5 | Gehaald | `lambda.*.cs` |
| REQ-6 | Gehaald | `because-args.before.cs` / `.manual.txt` |
| REQ-7 | Gehaald | `chain.*`, `which.*`, `and-chain.*` |
| REQ-8 | Gehaald | `keys.*`, `not-contain-keys.*` |
| REQ-9 | Gehaald | `contain-value.*`, `contain-values.*`, `not-contain-value.*` |
| REQ-10 | Gehaald | `nullable.*` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | ProofTests voor `ContainKey/` alle groen; zie verslag voor het totaal |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |

## Zie ook

- [fa-contain-key-modellen.md](fa-contain-key-modellen.md)
