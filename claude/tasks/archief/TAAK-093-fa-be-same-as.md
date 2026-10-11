# TAAK-093 — `BeSameAs`/`NotBeSameAs` → `ShouldBeSameAs`/`ShouldNotBeSameAs`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (top-10 acties 1–4)

## Keuzes

Nieuw patroon `BeSameAsPattern` op `FluentAssertionPattern`; de basis doet scope, vorm, resultaat en becauseArgs.

## Bewijs

Probe: zelfde, ander, beide null, één null, geboxte ints: gelijk oordeel (ReferenceEquals). Overloads: `ShouldBeSameAs(object, object, string)`, `ShouldNotBeSameAs(object, object, string)`.

Probe-code: scratchpad `p092/Program.cs` (FA 7.2.2 + Shouldly 4.3.0). Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/` (zie `docs/projectinfo/architectuur/proofs.md`).

## Resultaat

`dotnet build`, `dotnet test` (579) en de SampleShop-tests (38) groen. Scan: zie `ANALYSE-fa-100.md` sectie "Na acties 1–4".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/README.md` (patroontabel)
