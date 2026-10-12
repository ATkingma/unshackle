# TAAK-165 — `fa.custom-assertions`: eigen `Should()`-extensie + eigen assertion-klasse

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Bron**: `claude/tasks/patroon-queue.md` rij 34

## Onderzoek

Rij 34 is al gerealiseerd door TAAK-091 (`fa.custom-assertion`, `CustomAssertionPattern`, enkelvoud). Niet stil
overgeslagen: elke aanroep van een niet-FA-methode op een `*Assertions`-type (FA-subklasse) wordt Manual met reden
"Custom assertion `Klasse.Methode` (bestand): rewrite this FluentAssertions extension to Shouldly once; its calls can stay."
De reden noemt de klasse en het definitiebestand (of de assembly), dus het rapport wijst naar de definitie-plek.
De `Should()`-extensie zelf (eigen statische klasse) wordt niet apart gemeld: de Manual-regel staat op de assertion
(`BeCancelled`), en die reden noemt de klasse. Wat de oorspronkelijke `using FluentAssertions` betreft: de Fixer laat een
old using staan als een Manual-finding de file nog FA-types laat gebruiken (TAAK-073/115); het pakket blijft dus nodig
tot de gebruiker de extensie herschrijft.

## Keuzes

- Geen nieuw pattern: dubbel werk met `fa.custom-assertion`; wel een extra proof voor precies het scenario uit de
  rij: `order.Should().BeCancelled(..)` met eigen `Should()` + `ObjectAssertions`-subklasse.
- Proof `Proofs/FluentAssertions/CustomAssertion/should-extension.{before.cs,manual.txt}`.

## Resultaat

`dotnet test --filter-class *ProofTests`: 652 groen. Plan/modellen/verify en README-rij bestaan al (`fa-custom-assertion-*`).

## Zie ook

- `claude/tasks/archief/TAAK-091-custom-assertions-gegroepeerd.md`
- `docs/projectinfo/patterns/fa-custom-assertion-plan.md`
