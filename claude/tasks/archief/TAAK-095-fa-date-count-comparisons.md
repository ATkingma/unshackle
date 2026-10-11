# TAAK-095 — `BeAfter`/`BeOnOrAfter`/`BeBefore`/`BeOnOrBefore` en `HaveCountGreaterThan…`/`HaveCountLessThan…`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (top-10 acties 1–4)

## Keuzes

Mapping toegevoegd aan `BeGreaterLessPattern` (dezelfde IComparable- en nullable-controles) en `HaveCountPattern` (dezelfde count-receiver: Count/Length/Count()).

## Bewijs

Probe: DateTime gelijk/±1 tick/andere Kind, DateTimeOffset gelijk/zelfde instant andere offset/+1 tick, DateOnly; counts op n-1, n, n+1: overal gelijk oordeel.

Probe-code: scratchpad `p092/Program.cs` (FA 7.2.2 + Shouldly 4.3.0). Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/` (zie `docs/projectinfo/architectuur/proofs.md`).

## Resultaat

`dotnet build`, `dotnet test` (579) en de SampleShop-tests (38) groen. Scan: zie `ANALYSE-fa-100.md` sectie "Na acties 1–4".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/README.md` (patroontabel)
