# Verification: fa-be-null

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-null-plan.md`](fa-be-null-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-10 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 549bc56).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeNullPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeNull/`, gedraaid door `ProofTests`.

| REQ   | Oordeel | Bewijs |
| ----- | ------- | ------ |
| REQ-1 | Gehaald | `be-null.before.cs` / `be-null.after.cs` |
| REQ-2 | Gehaald | `not-be-null.before.cs` / `not-be-null.after.cs` |
| REQ-3 | Gehaald | `because.before.cs` / `because.after.cs` |
| REQ-4 | Gehaald | `nullable-value.before.cs` / `nullable-value.after.cs` (`int?`, `bool?`; after compileert tegen Shouldly) |
| REQ-5 | Gehaald | `collection.*.cs` (List, array), `lambda.*.cs` |
| REQ-6 | Vervangen | Door Uitbreiding TAAK-097: `because-args` is nu Fixed |
| REQ-7 | Gehaald | `chain.*` (`.And`), `value-used.*` (variabele) |
| REQ-8 | Gehaald | `and-chain.before.cs` / `and-chain.manual.txt` |
| Uitbreiding TAAK-097 | Gehaald | `because-args.before.cs` / `because-args.after.cs` (`string.Format(...)`) |
| Uitbreiding TAAK-100 | Gehaald | Gedeelde check in `FluentAssertionPattern.Rewrite`; proofs in `AssertionScope/` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Architecture | `.Which` als aparte Manual-proof | `.Which` bestaat niet op `NotBeNull` van `StringAssertions`/`ObjectAssertions` (FA 7.2.2): de `before` compileerde niet; `chain` (`.And`) en `value-used` dekken dezelfde Manual-tak | Low |
| 2 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |
| 3 | Scope | _(edge case)_ | `a?.B.Should().BeNull()` → `a?.B.ShouldBeNull()` (TAAK-041, `edge-null-conditional.*`). Semantiek gelijk: bij `a == null` slaan FA en Shouldly de check allebei over; ook voor `NotBeNull`. Expression-bodied void-leden worden herschreven (TAAK-040, `edge-expression-bodied.*`) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed: 46 of 46 (inclusief alle ProofTests); SampleShop-solution 15 van 15 |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |
