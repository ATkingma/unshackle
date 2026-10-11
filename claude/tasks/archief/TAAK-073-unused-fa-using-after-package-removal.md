# TAAK-073 — ongebruikte `using FluentAssertions;` blijft staan als het pakket weggaat

**Status**: voltooid
**Aangemaakt**: 2026-10-11
**Gevonden in**: corpus-run Ardalis.Result (`docs/projectinfo/architectuur/corpus.md`)

## Probleem

In `Ardalis.Result.AspNetCore.UnitTests` hebben 2 bestanden `using FluentAssertions;` zonder één assertion. Die bestanden
geven geen findings, dus de using-swap draait er niet. Het project heeft verder geen Manual, dus `project-file`
verwijdert de FluentAssertions-referentie → CS0246, het project bouwt niet meer.

## Wat, waar, hoe

- **Waar**: de usings-stap in de `Fixer` / project-file-stap (geen pattern-code).
- **Hoe**: verwijdert de fix het pakket uit een project, dan ook ongebruikte `using FluentAssertions*;` (en
  `global using`) uit alle bestanden van dat project halen, ook zonder findings.
- Geen proof mogelijk (proofs eisen ≥1 finding); test in `src/Unshackle.Tests/Core/` met een in-memory solution.

## Stappen

- [x] Usings opruimen in bestanden zonder findings bij pakketverwijdering
- [x] Unittest
- [x] `dotnet build` + `dotnet test` groen

## Resultaat

`Fixer` draait de using-stap ook voor bestanden zonder findings met een ongebruikte oude using; zonder wijziging of swapped types wordt die alleen verwijderd (geen Shouldly-using). Test: `FixerTests.FixAsync_FileWithoutFindingsHasUnusedRemovedUsing_RemovesItWithoutAddingNamespaces`.

## Zie ook

- `docs/projectinfo/architectuur/corpus.md`
- `claude/tasks/archief/TAAK-044-fa-global-using.md`
