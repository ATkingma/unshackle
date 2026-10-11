# TAAK-152 — Hernoemde using niet opnieuw gesorteerd (SA1210)

**Status**: klaar
**Aangemaakt**: 2026-10-11 (`claude/tasks/REGRESSIE-corpus-2.md`, klasse 1)

## Context

Facility (FA 7.2.2, StyleCop met warnings-as-errors): na `fix` faalt de build met 96× SA1210 in 16 testbestanden. `using FluentAssertions;` wordt in place `using Shouldly;` (`src/Unshackle.Core/UsingDirectives.cs:52`, `WithName`), dus `using Shouldly;` staat vóór `using NUnit.Framework;`. Zelfde risico voor `using MediatR;` → `using Mediator;` (toevallig goed bij MODiX).

## Stappen

1. [x] Proof: bestand met `using FluentAssertions;` gevolgd door `using NUnit.Framework;` → after met `using NUnit.Framework;` / `using Shouldly;`.
2. [x] Na hernoemen alleen opnieuw sorteren als de oorspronkelijke lijst al gesorteerd was (respecteer `dotnet_sort_system_directives_first`); anders niets aan de volgorde doen.
3. [x] Bewijs: harnas op `src2/facility.tgz` → status ok, 1164 tests, 0 regressies.

## Zie ook

- `claude/tasks/archief/TAAK-115-usings-stijl-en-overbodig.md`
- `src/Unshackle.Core/UsingDirectives.cs`

## Verslag (2026-10-11)

Harnas `out5` (src2 ongewijzigd): Facility ok 1164/1164, MODiX ok 477/477 (23 Fixed), 0 regressies. `dotnet test` 747/747.
