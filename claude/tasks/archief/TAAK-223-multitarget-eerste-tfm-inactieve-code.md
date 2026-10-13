# TAAK-223 — Multi-target project: alleen de eerste TFM wordt gemigreerd, code onder `#if !NETFRAMEWORK` blijft liggen

**Status**: ✅ klaar (2026-10-13)
**Aangemaakt**: 2026-10-13 (FA-corpusrun ronde 4)
**Doel**: bestanden in multi-target projecten migreren in een TFM waarin de code actief is.

## Context voor de volgende agent

- LiteDB `7b71bc4`: 709 `unmatched-use`-Manuals (593× `x.Should()`), vooral in `LiteDB.Tests/Engine/Coordinator*_Tests.cs` en `SharedCoordination*_Tests.cs`. Daarnaast 14 `usings`-findings "Code in an inactive #if branch".
- `LiteDB.Tests.csproj` heeft `TargetFrameworks net462;net481;net8.0;net10.0`, met `NETFRAMEWORK` gedefinieerd voor net4x. De betreffende bestanden staan helemaal in `#if !NETFRAMEWORK`.
- `Fixer.FixAsync` (`src/Unshackle.Core/Fixer.cs`, "A multi-target project is loaded once per target framework") groepeert documenten per pad en neemt `file.First()`. Dat is hier de net462-kopie, waarin de code inactief is. `ProjectPackages.RemainingUsesAsync` vindt de calls wel in een andere TFM, en dat geeft `unmatched-use`.

## Stappen

- [x] 1. Per bestand de projectkopie kiezen waarin de takken met FA-calls actief zijn (bijv. de kopie met de meeste actieve FA-calls), of per TFM fixen en alleen schrijven als de uitkomst gelijk is.
- [x] 2. Unittest met een multi-target project en `#if !NETFRAMEWORK`.
- [x] 3. LiteDB-herhaling: unmatched-use ≈ 0, build en tests groen.

## Uitkomst

- `Fixer.PassCopiesAsync`: per bestand één pass per kopie met een afwijkende set inactieve `#if`-code (bestand zonder directives: alleen de eerste kopie). Pass 2+ werkt op de tekst tot dan toe en fixt alleen nodes die in alle eerdere passes inactief waren (`Fixer.NewCode`, herparse met de eerdere `ParseOptions`); gedeelde code wordt dus één keer gefixt en gerapporteerd. Conflictcontroles (TAAK-174/198/199) per pass (`FixCopyAsync`). De usings-rij "inactive #if branch" van een eerdere pass vervalt als een latere pass de usings bijwerkt.
- Tests: `FixerTests.FixAsync_CodeOnlyActiveInLaterTargetFramework_IsFixedOnceInEveryBranch` (compileert in beide TFM's), `FixAsync_ManualInSharedCode_IsReportedOnceAcrossTargetFrameworks`. Geen proof: dit is Fixer-gedrag, geen pattern.
- LiteDB (`<corpus>/corpus-fa/_results/r5-litedb/`): unmatched-use 709 → 2, usings 15 → 2, Fixed 6917 → 7649; build groen; alleen de 26 bekende regressies van TAAK-222.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md` (ronde 4)
- `claude/tasks/patroon-queue.md`
