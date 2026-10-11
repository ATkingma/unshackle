# TAAK-012 — Patroon fa.throw-async

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `await act.Should().ThrowAsync<T>()` (+ `.WithMessage`) wordt `await Should.ThrowAsync<T>(act)` (+ message-check), met proofs.

## Context

Rij 7. Bouwt voort op TAAK-008 (`fa.throw`). Sample: `OrderHandlerTests.cs:47`.

## Analyse

FA 7.2.2: `ThrowAsync<T>` op `AsyncFunctionAssertions` (subject `Func<Task>` / `Func<Task<T>>`; `Func<ValueTask>` heeft geen `Should()`), geeft `Task<ExceptionAssertions<T>>`; `WithMessage` is een extensiemethode op die taak. Shouldly 4.3.0: `Should.ThrowAsync<T>(Func<Task>|Task, string customMessage)`; geen `Func<Task<T>>`-overload, maar `Func<Task<ref>>` converteert via covariantie. Afgeleide exceptions worden geaccepteerd (zoals `Throw`).

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Find geeft de omringende `await` (of de aanroep zelf zonder await) | Bij een message-check moet de await tussen haakjes: `(await Should.ThrowAsync<T>(act)).Message...` |
| 2 | Alleen `await` als losse statement is Fixed; zonder await, `.Where`, `var x = await` → Manual ("not directly awaited" / "is used") | Geen eenduidige vorm |
| 3 | `ThrowExactlyAsync` → Manual | Shouldly accepteert afgeleide types |
| 4 | `Func<Task>` en `Func<Task<refType>>` Fixed; `Func<Task<valueType>>` → Manual ("reference type") | Geen implicit conversie naar `Func<Task>` |
| 5 | `Awaiting(...)`-subject → Manual ("Invoking") | Zoals bij Throw |
| 6 | Wildcard-vertaling en argumentopmaak gedeeld in `ThrowMessage` (ook door `ThrowPattern`) | Voorkomt duplicatie; Throw-proofs blijven groen |
| 7 | `TryGetArguments` slaat argumenten zonder `ArgumentSyntax` (receiver van extensiemethode) over | Anders InvalidCastException bij `WithMessage` op een taak |

## Stappen

- [x] Claimen, onderzoek, plan (13 REQ), modellen, pattern + registratie, 18 proofs, verify, build/test groen

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowAsyncPattern.cs`, `ThrowMessage.cs` (nieuw), `ThrowPattern.cs` (gebruikt helper), `FluentAssertionsSyntax.cs` (receiver overslaan), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/ThrowAsync/`
- `docs/projectinfo/patterns/fa-throw-async-{plan,modellen,verify}.md` + `img/`, `docs/projectinfo/README.md`

## Zie ook

- `claude/tasks/archief/TAAK-008-fa-throw.md`
- `claude/tasks/patroon-queue.md`
