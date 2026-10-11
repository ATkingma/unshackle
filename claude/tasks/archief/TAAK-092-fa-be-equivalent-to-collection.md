# TAAK-092 — `BeEquivalentTo` op een collectie zonder options → `ShouldBe(expected, ignoreOrder: true)`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (top-10 acties 1–4)

## Keuzes

Uitbreiding van `fa.be-equivalent-to-object` (geen eigen patroon): die Find vindt deze calls al en gaf Manual "is a collection"; een tweede patroon zou om dezelfde nodes concurreren. Alleen leaf-elementen (dezelfde `IsLeaf` als voor members, ook `Nullable<leaf>`) en exact hetzelfde elementtype aan beide kanten (anders kan `ShouldBe<T>` T niet afleiden). Records/klassen als element blijven Manual: FA vergelijkt die per member, Shouldly met Equals. `params`-overload blijft Manual.

## Bewijs

Probe FA 7.2.2 vs Shouldly 4.3.0 (`ShouldBe(e, ignoreOrder: true)`): zelfde oordeel bij andere volgorde, duplicaten ({1,1,2} vs {1,2,2} rood, vs {1,2,1} groen), andere lengte, leeg/leeg, leeg/één, null-elementen, hoofdletters, null-subject, null-verwachting, beide null, enum, int?, NaN, decimal-schaal, DateTime-Kind, DateTimeOffset-offset. Overload in Shouldly.dll: `ShouldBe<T>(IEnumerable<T>, IEnumerable<T>, bool ignoreOrder, string customMessage)`.

Probe-code: scratchpad `p092/Program.cs` (FA 7.2.2 + Shouldly 4.3.0). Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/` (zie `docs/projectinfo/architectuur/proofs.md`).

## Resultaat

`dotnet build`, `dotnet test` (579) en de SampleShop-tests (38) groen. Scan: zie `ANALYSE-fa-100.md` sectie "Na acties 1–4".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/README.md` (patroontabel)
