# TAAK-095 — `BeAfter`/`BeOnOrAfter`/`BeBefore`/`BeOnOrBefore` and `HaveCountGreaterThan…`/`HaveCountLessThan…`

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (top-10 actions 1–4)

## Choices

Mapping added to `BeGreaterLessPattern` (same IComparable and nullable checks) and `HaveCountPattern` (same count receiver: Count/Length/Count()).

## Evidence

Probe: DateTime equal/±1 tick/different Kind, DateTimeOffset equal/same instant different offset/+1 tick, DateOnly; counts at n-1, n, n+1: same verdict everywhere.

Probe code: scratchpad `p092/Program.cs` (FA 7.2.2 + Shouldly 4.3.0). Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/` (see `docs/projectinfo/architectuur/proofs.md`).

## Result

`dotnet build`, `dotnet test` (579) and the SampleShop tests (38) green. Scan: see `ANALYSE-fa-100.md` section "Na acties 1–4".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/README.md` (pattern table)
