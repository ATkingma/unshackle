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
| REQ-9  | Vervangen | door Uitbreiding ThrowExactly (`fa.throw-exactly`) |
| REQ-10 | Vervangen | door Uitbreiding TAAK-097 |
| REQ-11 | Gehaald | `message-wildcard.*`, `message-variable.*` |
| REQ-12 | Deels   | `chain.*`, `edge-inner-exception.*` (`.Which`-deel vervangen door TAAK-096); de `.And.Throw`-vorm heeft geen proof (compileert niet met FA 7.2.2), de code-tak (`GetSubject` is null) wordt wel doorlopen via de gedeelde helper |
| REQ-13 | Gehaald (Invoking-deel vervangen) | `value-func.*` |
| ThrowExactly | Gehaald | `ThrowExactly/*.after.cs` |
| TAAK-097 | Gehaald | `because-args.after.cs`, `message-because-args.after.cs` |
| TAAK-096 | Gehaald | `value-used.after.cs`, `edge-which-message.after.cs`; Manual: `chain`, `edge-inner-exception` |
| Invoking | Gehaald | `Invoking/simple`, `func`, `throw-exactly` (Fixed); `Invoking/side-effect-receiver`, `value-type`, `method-group` (Manual) |
| AggregateException | Gehaald | `aggregate-wait`, `aggregate-method-group` (Manual); `aggregate-expected`, `aggregate-get-result` (Fixed) |
| Regeleinden | Gehaald | `message-line-break`, `edge-multiline-message` (Fixed); `message-contains-line-break` (Manual) |
| TAAK-100 | Gehaald | `ThrowExactly/assertion-scope`, `Invoking/assertion-scope` |
| TAAK-070 | Gehaald | `edge-assertion-in-throw-lambda` (`.manual.txt`): `Throw<T>`/`ThrowAsync<T>` met T ≠ `Exception`/`ShouldAssertException` op een actie met een FA-assertion → Manual (`ExpectsAssertionFailure`) |
| TAAK-220 | Gehaald | `aggregate-helper-method`, `aggregate-exception-and` (Manual), `aggregate-exception-plain` (Fixed); `ThrowMessage.MayThrowAggregate` volgt nu eigen methoden (2 niveaus) en kijkt bij `T = Exception` of de keten de exception gebruikt |
| TAAK-222 | Gehaald | `Throw/assertion-helper`, `BeTrueFalse/assertion-helper` (Manual); `FluentAssertionsSyntaxTests.IsInsideExpectedFailure_HelperInOtherFile_*`, `ExpectsAssertionFailure_FollowsHelperInOtherFile` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; gedocumenteerd verschil | Low |
| 2 | Behaviour | `WithMessage` gelijk aan FA | Het patroon wordt als string-literal opnieuw opgebouwd (verbatim/raw-opmaak gaat verloren) | Low |
| 3 | Behaviour | `WithMessage` gelijk aan FA | FA negeert `\r\n` vs `\n`. Exacte melding met regeleinde → `ShouldBe(.., IgnoreCase \| IgnoreLineEndings)`; `*`-vorm met regeleinde → Manual (Shouldly's ShouldContain/StartWith/EndWith kennen geen regeleinde-optie). Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-047, `message-line-break.*`, `message-contains-line-break.*`) | Low |
| 4 | Behaviour | `Throw<T>` gelijk aan FA | FA pakt `AggregateException` uit, Shouldly niet. Heuristiek: blokkeert de te volgen actie (lambda, lokale delegate-variabele, method group) op `Task.Wait`/`WaitAll`/`.Result` of roept `Parallel.For/ForEach/Invoke` aan, en accepteert `T` geen `AggregateException` → Manual. Niet te volgen acties (parameter, veld) en indirecte aanroepen worden gewoon omgezet. `GetAwaiter().GetResult()` en `WaitAny` wikkelen niet in en blijven Fixed. Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-048, `aggregate-*.*`) | Medium |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Test  | `dotnet test`  | passed (98 tests) |
| Sample | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | passed (15 tests) |

## Corpus (TAAK-170, TAAK-173)

- Polly 7.2.4 (`b1a5a17`, 2026-10-13, harnas zonder kloon-wijzigingen): Fixed 4449 → 5255, Manual 964 → 171, build 0 fouten, tests 5685/5685 → 5685/5685, 0 regressies.

## TAAK-220..222 (2026-10-13)

- `dotnet build` 0 warnings; `dotnet test` 1093/1093; sample 38/38.
- Corpus ronde 5: Akka 1 regressie (bekend, TAAK-200), Steeltoe 0, LiteDB 0 (was 10/18/26).

## Zie ook

- `claude/docs/patroon-loop.md`
