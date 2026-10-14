# TAAK-099 — `fa.match`: MatchRegex/NotMatchRegex and wildcard Match

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (actions 5–10)

## Choices

New pattern `MatchPattern` (`fa.match`), see `docs/projectinfo/patterns/fa-match-plan.md`.

## Evidence

See plan (probe of 22 wildcard cases, empty regex).
Probe code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflection on `Shouldly.dll`).

## Proofs

Match/* (6 Fixed, 4 Manual)

## Result

`dotnet build`, `dotnet test` (621) and the SampleShop tests (38) green. Scan: `ANALYSE-fa-100.md` section "Na acties 5–10".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
