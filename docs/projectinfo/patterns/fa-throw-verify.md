# Verification: fa-throw

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-throw-plan.md`](fa-throw-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 135f8e5).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `simple.*.cs` |
| REQ-2  | Gehaald | `func.*.cs` |
| REQ-3  | Gehaald | `because.*.cs` |
| REQ-4  | Gehaald | `lambda.*.cs` |
| REQ-5  | Gehaald | `message-contains.*.cs` |
| REQ-6  | Gehaald | `message-starts.*.cs`, `message-ends.*.cs` |
| REQ-7  | Gehaald | `message-exact.*.cs` |
| REQ-8  | Gehaald | `message-because.*.cs`, `message-exact-because.*.cs` |
| REQ-9  | Gehaald | `throw-exactly.*` |
| REQ-10 | Gehaald | `because-args.*`, `message-because-args.*` |
| REQ-11 | Gehaald | `message-wildcard.*`, `message-variable.*` |
| REQ-12 | Deels   | `chain.*`, `value-used.*`; de `.And.Throw`-vorm heeft geen proof (compileert niet met FA 7.2.2), de code-tak (`GetSubject` is null) wordt wel doorlopen via de gedeelde helper |
| REQ-13 | Gehaald | `invoking.*`, `value-func.*` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |
| 2 | Behaviour | `WithMessage` gelijk aan FA | Het patroon wordt als string-literal opnieuw opgebouwd (verbatim/raw-opmaak gaat verloren) | Low |
| 3 | Behaviour | `WithMessage` gelijk aan FA | FA negeert `\r\n` vs `\n`. Exacte melding met regeleinde → `ShouldBe(.., IgnoreCase \| IgnoreLineEndings)`; `*`-vorm met regeleinde → Manual (Shouldly's ShouldContain/StartWith/EndWith kennen geen regeleinde-optie). Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-047, `message-line-break.*`, `message-contains-line-break.*`) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Test  | `dotnet test`  | passed (98 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (15 tests) |

## Zie ook

- `claude/docs/patroon-loop.md`
