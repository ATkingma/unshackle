# TAAK-022 — Scan/fix telt bevindingen per target framework

**Status**: open
**Aangemaakt**: 2026-10-11
**Gevonden in**: TAAK-019 stap 1, casus microsoft/PowerApps-Tooling @ 630e6973

## Probleem

Bij multi-target projecten (`net48;net8.0;net10.0`) laadt MSBuildWorkspace elk project per TFM. Scan rapporteert daardoor elke bevinding 3x: tabel "778 fixed, 162 manual", werkelijk 262 Fixed / 53 Manual unieke regels. `fix` meldt "64 file(s) written" terwijl 27 bestanden wijzigen. Het rapport (`--report`) bevat dezelfde regel 3x.

Geen fout in een pattern, wel misleidende cijfers (README/lancering!) en mogelijk dubbel schrijven.

## Aanpak (voorstel)

Dedupliceer documenten op bestandspad (één TFM per bestand verwerken, of bevindingen op (pad, regel, pattern) dedupliceren) in de Fixer/workspace-laag. Test met een multi-target sample.

## Stappen

- [ ] Test: multi-target project geeft elke bevinding één keer
- [ ] Dedupliceren + bestanden één keer schrijven
- [ ] Casus opnieuw: cijfers kloppen met het rapport

## Zie ook

- `claude/tasks/TAAK-019-lancering.md`
- `claude/tasks/TAAK-021-fa-nullable-subject.md`
