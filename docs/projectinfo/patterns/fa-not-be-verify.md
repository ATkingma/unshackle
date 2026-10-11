# Verification: fa-not-be

|               |                                                                    |
| ------------- | ------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-not-be-plan.md`](fa-not-be-plan.md) |
| **Status**    | Verified                                                           |
| **Generated** | 2026-10-11 by KingmaDoc skill                                      |

> Verified by an AI agent against uncommitted changes (basis ed2e155).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotBePattern.cs`; registratie in `FluentAssertionsFix.cs`; `ConvertExpected` (nu publiek in `FluentAssertionsSyntax`) gedeeld. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/NotBe/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs                         |
| ----- | ------- | ------------------------------ |
| REQ-1 | Gehaald | `simple.*.cs`                  |
| REQ-2 | Gehaald | `because.*.cs`                 |
| REQ-3 | Gehaald | `nullable.*.cs`                |
| REQ-4 | Gehaald | `record.*.cs`                  |
| REQ-5 | Gehaald | `constant-cast.*.cs`           |
| REQ-6 | Gehaald | `assertion-scope.*`            |
| REQ-7 | Gehaald (becauseArgs vervangen) | `comparer.*` |
| REQ-8 | Gehaald | `chain.*`                      |
| Uitbreiding TAAK-097 | Gehaald | `because-args.after.cs` (`string.Format(CultureInfo.InvariantCulture, ...)`) |

## Deviations

| #   | Area | Plan says              | Code does                                                                  | Impact |
| --- | ---- | ---------------------- | -------------------------------------------------------------------------- | ------ |
| 1   | Code | Alleen eigen bestanden | `FluentAssertionsSyntax.ConvertExpected` werd `internal` om de conversie te delen | Low    |

## Validation results

| Check  | Command                                                     | Result              |
| ------ | ----------------------------------------------------------- | ------------------- |
| Build  | `dotnet build`                                              | passed (0 warnings) |
| Test   | `dotnet test`                                               | passed (320 tests)  |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (38 tests)   |

## Zie ook

- [fa-not-be-modellen.md](fa-not-be-modellen.md)
- `claude/docs/patroon-loop.md`
