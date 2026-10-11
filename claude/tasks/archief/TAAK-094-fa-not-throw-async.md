# TAAK-094 — `await act.Should().NotThrowAsync()` → `await Should.NotThrowAsync(act)`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (top-10 acties 1–4)

## Keuzes

Nieuw patroon `NotThrowAsyncPattern` (IPattern, zoals ThrowAsync: node = de await, `ConfigureAwait` blijft; `ThrowSyntax.Follow` publiek gemaakt). Geen aggregate-heuristiek: NotThrow faalt op elke exceptie, ook AggregateException (geprobed). Generiek, `NotThrowAfterAsync`, niet-awaited/gebruikt resultaat, `Func<Task<valuetype>>`, `FluentActions.Awaiting` en becauseArgs → Manual.

## Bewijs

Probe: ok, synchrone throw, async throw, AggregateException, `Func<Task<int>>`: gelijk oordeel (bij synchrone throw gooit Shouldly de originele exceptie; beide rood). Overloads: `Should.NotThrowAsync(Func<Task>, string)` en `(Task, string)`.

Probe-code: scratchpad `p092/Program.cs` (FA 7.2.2 + Shouldly 4.3.0). Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/` (zie `docs/projectinfo/architectuur/proofs.md`).

## Resultaat

`dotnet build`, `dotnet test` (579) en de SampleShop-tests (38) groen. Scan: zie `ANALYSE-fa-100.md` sectie "Na acties 1–4".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/README.md` (patroontabel)
