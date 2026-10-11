# TAAK-091 — eigen assertion-extensies: Manual, gegroepeerd per extensie

**Status**: voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md`, categorie 3 (queue #34)

## Probleem

Eigen FA-extensies (`BeFailure`, `HaveSingleMethodBody`, …) werden niet herkend: geen finding, en de gebruiker ziet
niet dat één herschreven extensie honderden aanroepen oplost.

## Wat, waar, hoe

- **Herkennen** (Core, eigen nieuw bestand `Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs`):
  methode niet uit `FluentAssertions.*`, receiver (extensie) of declarerend type (instance) is of erft van een FA-type
  met naam `*Assertions`. Altijd Manual: de body gebruikt `Execute.Assertion`, niet generiek te vertalen.
  Reden per extensie gelijk: "Custom assertion `Type.Methode` (bestand): rewrite … once; its calls can stay."
- **Registratie**: één regel `new CustomAssertionPattern()` in `FluentAssertionsFix.Patterns`, na `AssertionScopePattern`.
  Bestaande FA-patroonbestanden, `Fixer.cs`, `IFix` en MediatR niet aangeraakt.
- **Groeperen**: `CustomAssertionPattern.Group(findings)` (op exacte reden). `ReportRow.FromAll` → één rij per extensie
  met "N call(s), first here." (csv/json/md). `SpectreConsoleView.ShowSummary` → advieskop + `Nx reden`.
- **Waarom geen extra JSON-sectie of CSV-kolom**: rijvorm blijft gelijk voor alle formaten; aantal staat in de reden.

## Resultaat

Corpus FluentResults: 7 extensies (BeFailure 31x, HaveReason 25x, BeSuccess 8x, …) in de console.
Proofs `Proofs/FluentAssertions/CustomAssertion/{extension,subclass}`; unittest `FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount`.
KingmaDoc: `docs/projectinfo/patterns/fa-custom-assertion-{plan,modellen,verify}.md`.

## Zie ook

- `docs/projectinfo/architectuur/rapportage.md` — sectie TAAK-091
- `docs/projectinfo/architectuur/core.md`
