# TAAK-099 — `fa.match`: MatchRegex/NotMatchRegex en wildcard-Match

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (acties 5–10)

## Keuzes

Nieuw patroon `MatchPattern` (`fa.match`), zie `docs/projectinfo/patterns/fa-match-plan.md`.

## Bewijs

Zie plan (probe 22 wildcardgevallen, lege regex).
Probe-code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflectie op `Shouldly.dll`).

## Proofs

Match/* (6 Fixed, 4 Manual)

## Resultaat

`dotnet build`, `dotnet test` (621) en de SampleShop-tests (38) groen. Scan: `ANALYSE-fa-100.md` sectie "Na acties 5–10".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
