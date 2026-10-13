# Verification: fa-be-true-false

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-true-false-plan.md`](fa-be-true-false-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-10 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis b1ab588).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/`, gedraaid door `ProofTests`.

| REQ   | Oordeel                     | Bewijs |
| ----- | --------------------------- | ------ |
| REQ-1 | Gehaald                     | `be-true.before.cs` / `be-true.after.cs` |
| REQ-2 | Gehaald                     | `be-false.before.cs` / `be-false.after.cs` (`(a == b).Should().BeFalse()`) |
| REQ-3 | Gehaald                     | `because.before.cs` / `because.after.cs` |
| REQ-4 | Gehaald                     | `nullable.before.cs` / `nullable.after.cs` (`ShouldBe(true)`, `ShouldBe(false, "why")`) |
| REQ-5 | Vervangen | Door Uitbreiding TAAK-097: `because-args` is nu Fixed |
| REQ-6 | Gehaald                     | `chain.before.cs` / `chain.manual.txt` |
| REQ-7 | Gehaald                     | `value-used.before.cs` / `value-used.manual.txt` |
| REQ-8 | Gehaald in code, geen proof | Type-check (`bool`/`bool?`) in `RewriteAssertion`; een onbekend type laat `before` niet compileren |
| Uitbreiding TAAK-097 | Gehaald | `because-args.before.cs` / `because-args.after.cs` (`string.Format(...)`) |
| Uitbreiding TAAK-100 | Gehaald | Gedeelde check in `FluentAssertionPattern.Rewrite`; proofs in `AssertionScope/` |
| Uitbreiding TAAK-198 | Gehaald | `own-shouldbe-in-scope.before.cs` / `.after.cs`: eigen `ShouldBe<T>` in scope, `BeTrue` → `ShouldBeTrue` met `using static Shouldly.ShouldBeBooleanExtensions;`, bestaande eigen `ShouldBe`-call blijft ongedubbelzinnig; unittests `AmbiguousAddedNamespaceTests` (gemengd bestand, namespace-variant, terugval bij botsende alias). |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Requirements | REQ-8 is gedekt | Alleen via code bewaakt, geen proof (before compileert niet) | Low |
| 2 | Architecture | TAAK-003 keuze 8: aparte Manual voor `GetSubject` null | `chain.before.cs` gebruikt `.BeTrue().And.NotBe(false)`: `ProofTests` eist dat alle findings de reden bevatten, dus een tweede FA-patroon in de keten (`Be`) zou een andere reden geven | Low |
| 3 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |

## Validation results

| Check | Command        | Result                                                |
| ----- | -------------- | ----------------------------------------------------- |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                          |
| Tests | `dotnet test`  | passed: 36 of 36 (hele solution, inclusief alle ProofTests) |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld               |
