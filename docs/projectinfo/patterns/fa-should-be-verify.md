# Verification: fa-should-be

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-should-be-plan.md`](fa-should-be-plan.md)     |
| **Status**    | Matches plan                                                        |
| **Generated** | 2026-10-10T23:35+02:00 by KingmaDoc skill 1.1.0                             |

> Verified by an AI agent against the code at uncommitted changes (basis b1ab588).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs`; registratie in `FluentAssertionsFix.cs` (`Patterns`-lijst). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `simple.before.cs` / `simple.after.cs` (int, string) |
| REQ-2  | Gehaald | `because.before.cs` / `because.after.cs`; `because` gaat mee in de originele argumentlijst |
| REQ-3  | Gehaald | `lambda.before.cs` / `lambda.after.cs` (`IsResultDiscarded` accepteert void-lambda) |
| REQ-4  | Gehaald | `constant-cast.before.cs` / `constant-cast.after.cs`: `(byte)1`, `(Status)0`, `long` zonder cast (`ConvertExpected`) |
| REQ-5  | Gehaald | `chain.before.cs` / `chain.manual.txt` (reden "is used") |
| REQ-6  | Gehaald | `because-args.before.cs` / `because-args.manual.txt` (`TryGetArguments` geeft false) |
| REQ-7  | Gehaald | `comparer.before.cs` / `comparer.manual.txt` (`values.Count != 1`) |
| REQ-8  | Gehaald | `nullable-expected.before.cs` / `nullable-expected.manual.txt` (`ConvertExpected` geeft null) |
| REQ-9  | Gehaald | `xml.before.cs` / `xml.manual.txt` (namespace `FluentAssertions.Xml`/`.Types`) |
| REQ-10 | Gehaald in code, geen proof | `subject is null` en `IErrorTypeSymbol` geven Manual in `Rewrite`; geen proof-bestand |

## Deviations

| #   | Area         | Plan says                                              | Code does                                                                                       | Impact |
| --- | ------------ | ------------------------------------------------------ | ----------------------------------------------------------------------------------------------- | ------ |
| 1   | Requirements | REQ-10: onbekend type en afwijkende vorm worden Manual | Code doet dit (`ShouldBePattern.cs`, begin van `Rewrite`), maar geen proof dekt het        | Low    |
| 2   | Risk         | TAAK-001 noemt `Guid` tegenover `string` als Manual  | Valt onder dezelfde tak als `nullable-expected` (`ConvertExpected` null); alleen `int?` bewezen | Low    |
| 3 | Scope | _(edge case)_ | Expression-bodied void-methode/local function/accessor (`void M() => x.Should().Be(1);`) telt als discarded resultaat (TAAK-040, `edge-expression-bodied.*`); `a?.B.Should()` idem (TAAK-041) | Low |

Verder geen afwijkingen: alle `files_expected` bestaan en zijn aangeraakt, geen nieuwe containers of externe systemen, scope niet overschreden (geen `NotBe`, geen ketensplitsing).

## Validation results

| Check | Command        | Result                                                             |
| ----- | -------------- | ------------------------------------------------------------------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                                       |
| Tests | `dotnet test`  | passed: 29 of 29 (hele solution, inclusief alle ProofTests)        |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld in `.featuredoc.yml`       |

