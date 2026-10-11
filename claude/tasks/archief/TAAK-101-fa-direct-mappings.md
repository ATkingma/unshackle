# TAAK-101 — Kleine mappings

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (acties 5–10)

## Keuzes

Nieuw patroon `DirectMappingPattern` (`fa.direct-mapping`), zie plan. `BePositive`/`BeNegative` bestonden al (`fa.be-greater-less`); nieuw: `BeNegative` op double/float → Manual (FA faalt op NaN, Shouldly niet).

## Bewijs

Zie plan; probe NaN: `double.NaN.Should().BeNegative()` faalt, `ShouldBeNegative()` slaagt.
Probe-code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflectie op `Shouldly.dll`).

## Proofs

DirectMapping/* (6 Fixed, 3 Manual), BeGreaterLess/negative-double (Manual)

## Resultaat

`dotnet build`, `dotnet test` (621) en de SampleShop-tests (38) groen. Scan: `ANALYSE-fa-100.md` sectie "Na acties 5–10".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
