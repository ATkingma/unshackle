# TAAK-098 — Subject met bijwerkingen → `var subject = …;`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (acties 5–10)

## Keuzes

Nieuwe helper `FluentAssertionsSyntax.Hoist`: als `IsSafeToEvaluateTwice` weigert en het statement direct in een blok staat, komt er `var subject = <expr>;` vóór de statements (FA evalueert het subject ook één keer, vóór de assertions). Gebruikt in `fa.and-chain`, `fa.only-contain`, `fa.all-satisfy`. Naam is uniek in de member; meerdere hoists in één pass krijgen subject, subject2, … (k, k+n, …).
Niet gedaan: de Invoking-receiver (`fa.invoking`, eigen ThrowSyntax-pad) blijft Manual.

## Bewijs

Geen semantiekverschil: één evaluatie, zelfde volgorde (subject vóór argumenten).
Probe-code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflectie op `Shouldly.dll`).

## Proofs

AndChain/subject-call, subject-indexer, nieuw subject-call-twice; AllSatisfy/subject-call; OnlyContain/subject-call (Manual → Fixed)

## Resultaat

`dotnet build`, `dotnet test` (621) en de SampleShop-tests (38) groen. Scan: `ANALYSE-fa-100.md` sectie "Na acties 5–10".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
