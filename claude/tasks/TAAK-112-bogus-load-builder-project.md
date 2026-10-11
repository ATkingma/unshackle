# TAAK-112 — Bogus laadt niet in Unshackle terwijl `dotnet build` slaagt

**Status**: open
**Aangemaakt**: 2026-10-11 (corpus-run, `claude/tasks/CORPUSRUN-fa.md` volgt)

## Context

Bogus: `fix` stopt met "The solution could not be loaded (6 errors)": 3× "Builder: The type or namespace name 'System' could not be found" en NU1701 (Rant 3.0.530, Z.ExtensionMethods.WithTwoNamespace, net4x-pakketten in net9.0-project) in Bogus.Tests.csproj. TAAK-090 vangt NU1701 als warning, maar deze meldingen komen als MSBuild-failure met een andere tekst/vorm binnen.

## Stappen

1. Reproduceer op de Bogus-kopie (scratchpad/run-fa/src/bchavez_Bogus).
2. NU1701 in deze vorm ('Msbuild failed when processing … Package … was restored using …') als waarschuwing behandelen.
3. 'Builder': waarschijnlijk een build-project (NUKE e.d.) dat niet gerestored is of geen test/FA bevat — onderzoeken; projecten zonder FA-referentie die niet laden mogen een waarschuwing geven i.p.v. fataal (motiveren, zie TAAK-090-regels).
4. Bewijs: Bogus laadt, fix + build groen.

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
