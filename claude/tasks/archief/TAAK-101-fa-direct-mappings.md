# TAAK-101 — Small mappings

**Status**: ✅ Done
**Created**: 2026-10-11
**Source**: `claude/tasks/ANALYSE-fa-100.md` (actions 5–10)

## Choices

New pattern `DirectMappingPattern` (`fa.direct-mapping`), see plan. `BePositive`/`BeNegative` already existed (`fa.be-greater-less`); new: `BeNegative` on double/float → Manual (FA fails on NaN, Shouldly does not).

## Evidence

See plan; NaN probe: `double.NaN.Should().BeNegative()` fails, `ShouldBeNegative()` passes.
Probe code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflection on `Shouldly.dll`).

## Proofs

DirectMapping/* (6 Fixed, 3 Manual), BeGreaterLess/negative-double (Manual)

## Result

`dotnet build`, `dotnet test` (621) and the SampleShop tests (38) green. Scan: `ANALYSE-fa-100.md` section "Na acties 5–10".

## See also

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
