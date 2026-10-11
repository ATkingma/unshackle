# TAAK-111 — Projectbestand niet aanpasbaar → code toch herschreven (Shouldly ontbreekt)

**Status**: open
**Aangemaakt**: 2026-10-11 (corpus-run, `claude/tasks/CORPUSRUN-fa.md` volgt)

## Context

Polly (308× CS0246 `Shouldly`) en Ardalis.Result (96× CS0246, 5 csproj's): finding `project-file` Manual "cannot be edited without changing unrelated text; migrate it by hand". FA-pakket blijft, Shouldly wordt niet toegevoegd, maar de `.cs`-bestanden krijgen wel `using Shouldly` → build kapot.
Polly.Specs.csproj begint met een UTF-8 BOM (`EF BB BF`); vermoedelijk faalt de byte-exacte round-trip-check (review B, L5) op de BOM.

## Stappen

1. Reproduceer met een csproj met BOM (en CRLF) in `XmlProjectFileStoreTests`.
2. Round-trip BOM- en regeleinde-behoudend maken (BOM terugschrijven).
3. Veiligheidsregel: als het projectbestand van een project niet aangepast kan worden, dan geen `.cs`-bestanden van dat project herschrijven (alles Manual met dezelfde reden) — nooit een project achterlaten dat niet bouwt.
4. Bewijs: Polly- en Ardalis.Result-kopie opnieuw `fix` + build groen.

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/corpus.md`
