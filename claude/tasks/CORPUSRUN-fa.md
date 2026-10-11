# Corpus-run FluentAssertions (2026-10-11, main 40cf91e)

15 open-source solutions, kopieën in `scratchpad/run-fa/src`. Per project: restore → build/test (baseline) → `fix --fix fluentassertions --force` → build/test. Exitcodes (0 = gelukt). Alleen SDK 10.0.112, `DOTNET_ROLL_FORWARD=Major`.

| Project | restore | build vóór | test vóór | fix | build na | test na | Oorzaak / opmerking |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SmartEnum | 0 | 0 | 0 | 0 | 0 | 0 | ✅ volledig groen |
| FluentResults | 0 | 0 | 1 | 0 | 0 | 1 | bouwt na fix; tests faalden al vooraf (regressies niet vergeleken) |
| CSharpFunctionalExtensions | 0 | 0 | 0 | 0 | **1** | 0* | 42× CS1061 → TAAK-110 |
| Polly | 0 | 0 | 0 | 0 | **1** | 0* | 308× CS0246 Shouldly → TAAK-111/134 |
| Ardalis.Result | 0 | 0 | 1 | 0 | **1** | 1 | 96× CS0246 Shouldly → TAAK-111/134 |
| LiteDB | 0 | 0 | 1 | 0 | **1** | 1 | 4040× CS0246 Shouldly → TAAK-111/134 |
| NUKE | 0 | 0 | 1 | 0 | **1** | 1 | 58× CS0246 Shouldly → TAAK-111/134 |
| PowerApps-Tooling | 0 | 0 | 1 | 0 | **1** | 1 | `Equal([..])` → `new T[] { [..] }` → TAAK-113 |
| Bogus | 0 | 0 | 0 | **1** | 0 | 0 | laadt niet in Unshackle → TAAK-112 |
| Akka.NET | 1 | – | – | – | – | – | omgeving: NETSDK1226 (prune data); opnieuw met `-p:AllowMissingPrunePackageData=true` |
| Steeltoe | 1 | – | – | – | – | – | omgeving: NETSDK1226, idem |
| Mapperly | 1 | – | – | – | – | – | omgeving: MSB3073 in restore (eigen build-stap; `global.json` met SDK 8 verwijderd) |
| Ardalis.Specification | 0 | 1 | – | – | – | – | project ontbreekt in tarball |
| EventFlow | 1 | – | – | – | – | – | restore faalt (EventFlow.PostgreSql e.a.) |
| modular-monolith | 155 | – | – | – | – | – | gevraagde SDK ontbreekt |

\* build na fix faalde → tests liepen op oude binaries; zegt niets.

## Conclusie

- Van de 9 solutions die we konden draaien, bouwt na `fix`: **2** (SmartEnum, FluentResults).
- **TAAK-111/134** (projectbestand niet aanpasbaar → code toch herschreven, Shouldly ontbreekt) breekt er alleen al **4** (Polly, Ardalis.Result, LiteDB, NUKE). Hoogste prioriteit; in uitvoering.
- TAAK-110 (CSFE), TAAK-113 (PowerApps) en TAAK-112 (Bogus) zijn de overige blokkades.
- Na deze vier taken: run herhalen, inclusief Akka/Steeltoe met `-p:AllowMissingPrunePackageData=true`, en testregressies vergelijken.

Niet gedaan: Manual-redenen samengevoegd en testlogs vóór/na vergeleken (regressie-analyse). Rapporten per project: `scratchpad/run-fa/<naam>.json`.

## Herhaling na TAAK-110/112/113/114 (2026-10-11)

Verse kopieën in `scratchpad/v110/work/<naam>` (bron `corpus/src`), alleen restore → fix → build (`-m:2`); script `scratchpad/v110/run.sh`.

| Project | fix | build na | Opmerking |
| --- | --- | --- | --- |
| CSharpFunctionalExtensions | 0 | 0 | 4481 Fixed / 70 Manual; 21 `usings` (niet-gebonden `Should()`, TAAK-110) |
| Bogus | 0 | 0 | laadt; 'Builder' weggelaten met waarschuwing (TAAK-112) |
| Polly | 0 | 0 | 4446 Fixed / 966 Manual; Guid-`NotBeEmpty` → `ShouldNotBe(Guid.Empty)` (TAAK-114) |
| PowerApps-Tooling | – | – | niet herhaald: geen schone kopie beschikbaar (TAAK-113 alleen met proofs bewezen) |

evo1 (MediatR, TAAK-115): geen IDE0005/IDE0065 meer; build nog rood op IDE0060 en MSG0005 (TAAK-136).

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/ANALYSE-fa-100.md`
