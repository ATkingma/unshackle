# Verification: fa-be-equivalent-to-object

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-equivalent-to-object-plan.md`](fa-be-equivalent-to-object-plan.md) |
| **Status**    | Verified                                                  |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                       |

> Verified by an AI agent against the code at uncommitted changes (basis 31c568e).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`; registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `simple.*.cs`, `new-expectation.*.cs`, `record.*.cs`, `nested.*.cs` (after compileert tegen Shouldly) |
| REQ-2  | Gehaald | `because.*.cs` |
| REQ-3  | Gehaald | `lambda.*.cs` |
| REQ-4  | Gehaald | `not-equivalent.*` |
| REQ-5  | Gehaald | `and-chain.*`, `value-used.*` |
| REQ-6  | Vervangen | door REQ-14 (TAAK-097): `because-args` is nu Fixed |
| REQ-7  | Gehaald | `options.*` |
| REQ-8  | Gehaald | `anonymous.*`, `different-types.*`, `base-vs-derived.*` |
| REQ-9  | Gehaald | `collection.*`, `collection-member.*` |
| REQ-10 | Gehaald | `object-subject.*`, `object-member.*` |
| REQ-11 | Gehaald | `equals-override.*`, `equals-member.*` |
| REQ-12 | Gehaald | `cyclic.*` |
| REQ-13 | Gehaald | `no-members.*` |
| TAAK-092 | Gehaald | `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal`, `AndChain/equivalent-collection`; Manual: `collection-element-types`, `collection` |
| REQ-14 | Gehaald | `because-args.*.cs`, `collection-because-args.*.cs` |
| REQ-15 | Gehaald | `edge-collection-params.*` (Manual "is a collection") |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because`; andere meldingstekst | Low |
| 2 | Behaviour | Zelfde beslissing | Shouldly vergelijkt runtime-types (ook genest), FA alleen de gedeclareerde members (probe: afgeleid subject, afgeleide geneste member en beide-afgeleid met verschillend extra member: FA PASS, Shouldly FAIL). Besluit gebruiker (TAAK-049): blijft Fixed, met waarschuwing `FixedResult.Warning` in rapport (kolom Reason) en console-telling, tenzij type en geneste class-members `sealed`/struct zijn of beide kanten een `new`-expressie zijn. Unittest `BeEquivalentToObjectPatternTests`, proofs `sealed`, `both-new` | Medium |
| 3 | Coverage | Onbekend type geeft Manual | Tak "type onbekend" (null-literal) heeft geen proof: `BeEquivalentTo(null)` compileert niet (ambigue overload) | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed (0 warnings) |
| Tests | `dotnet test`  | passed: 236 of 236; sample-tests 15 van 15 |
| Lint  | _geen_         | _not run_: geen lint-commando ingesteld |
