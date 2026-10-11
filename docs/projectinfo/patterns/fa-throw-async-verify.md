# Verification: fa-throw-async

|               |                                                                  |
| ------------- | ---------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-throw-async-plan.md`](fa-throw-async-plan.md) |
| **Status**    | Verified                                                         |
| **Generated** | 2026-10-11 by KingmaDoc skill 1.1.0                              |

> Verified by an AI agent against the code at uncommitted changes (basis 41ff4a3).

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs` (+ gedeeld `ThrowMessage.cs`); registratie in `FluentAssertionsFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/`, gedraaid door `ProofTests`.

| REQ    | Oordeel | Bewijs |
| ------ | ------- | ------ |
| REQ-1  | Gehaald | `simple.*.cs` |
| REQ-2  | Gehaald | `func-task-reference.*.cs` |
| REQ-3  | Gehaald | `because.*.cs` |
| REQ-4  | Gehaald | `message-contains.*.cs` |
| REQ-5  | Gehaald | `message-starts.*.cs`, `message-ends.*.cs` |
| REQ-6  | Gehaald | `message-exact.*.cs` |
| REQ-7  | Gehaald | `message-because.*.cs`, `message-exact-because.*.cs` |
| REQ-8  | Vervangen | door Uitbreiding ThrowExactly (`fa.throw-exactly`) |
| REQ-9  | Vervangen | door Uitbreiding TAAK-097 |
| REQ-10 | Gehaald | `message-wildcard.*`, `message-variable.*` |
| REQ-11 | Gehaald | `no-await.*`, `where.*` |
| REQ-12 | Gehaald | `value-used.*` |
| REQ-13 | Gehaald (Awaiting-deel vervangen) | `value-func.*` |
| ThrowExactly | Gehaald | `ThrowExactly/async.*`, `ThrowExactly/async-message.*` |
| TAAK-097 | Gehaald | `because-args.after.cs`, `message-because-args.after.cs` |
| Awaiting | Gehaald | `Invoking/awaiting.*`, `Invoking/awaiting-exactly.*` |
| ConfigureAwait | Gehaald | `edge-configure-await.*` |
| AggregateException | Gehaald | `aggregate-result` (Manual), `aggregate-awaited` (Fixed) |
| Regeleinden | Gehaald | `message-line-break` (Fixed), `message-contains-line-break` (Manual) |
| TAAK-100 | Gehaald | `IsInsideAssertionScope` als eerste controle; proof `ThrowAsync/assertion-scope` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| - | ---- | --------- | --------- | ------ |
| 1 | Shared code | Eigen patroonbestand | Wildcard-vertaling verhuisd naar `ThrowMessage` (gedeeld met `ThrowPattern`, gedrag ongewijzigd; Throw-proofs groen); `FluentAssertionsSyntax.TryGetArguments` slaat de receiver van extensiemethoden over | Low |
| 2 | Behaviour | Foutmelding gelijk aan FA | Shouldly zet "Additional Info:" voor de `because` | Low |
| 3 | Scope | `.Where`/`.WithInnerException` | Manual met reden "not directly awaited" | Low |
| 4 | Scope | _(edge case)_ | `await ...ThrowAsync<T>()[.WithMessage(p)].ConfigureAwait(x)`: de ConfigureAwait blijft staan achter `Should.ThrowAsync<T>(act)` (TAAK-042, `edge-configure-await.*`) | Low |
| 5 | Behaviour | `WithMessage` gelijk aan FA | FA negeert `\r\n` vs `\n`. Exacte melding met regeleinde → `ShouldBe(.., IgnoreCase \| IgnoreLineEndings)`; `*`-vorm met regeleinde → Manual (Shouldly's ShouldContain/StartWith/EndWith kennen geen regeleinde-optie). Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-047, `message-line-break.*`, `message-contains-line-break.*`) | Low |
| 6 | Behaviour | `ThrowAsync<T>` gelijk aan FA | FA pakt `AggregateException` uit, Shouldly niet. Heuristiek: blokkeert de te volgen actie (lambda, lokale delegate-variabele, method group) op `Task.Wait`/`WaitAll`/`.Result` of roept `Parallel.For/ForEach/Invoke` aan, en accepteert `T` geen `AggregateException` → Manual. Niet te volgen acties (parameter, veld) en indirecte aanroepen worden gewoon omgezet. `GetAwaiter().GetResult()` en `WaitAny` wikkelen niet in en blijven Fixed. Probe FA 7.2.2/Shouldly 4.3.0 (TAAK-048, `aggregate-*.*`) | Medium |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed |
| Test  | `dotnet test`  | passed (180 tests, incl. 18 ThrowAsync-proofs) |

## Zie ook

- `claude/docs/patroon-loop.md`
- [fa-throw-async-modellen.md](fa-throw-async-modellen.md)
