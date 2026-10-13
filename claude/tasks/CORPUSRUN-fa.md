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

## Ronde 3: volledig corpus (2026-10-12, main 2800f23)

Shallow clones (`git clone --depth 1`) in `<corpus>/corpus-fa`, elk met een `nuget.config` met alleen nuget.org. Harnas: kopie van `claude/tools/corpus-regression.sh` (SDK 10, `NuGetAudit=false`, `AllowMissingPrunePackageData=true`, 3 jobs; test 2× vóór de fix, daarna `fix --force`, bin/obj weg, build, test, en vergelijking per test via TRX). De tool is gepubliceerd uit 2800f23. Alle HEADs gebruiken nog FA ≤ 7, dus er was geen oudere commit nodig; Polly, Mapperly en EventFlow staan op dezelfde tag als in ronde 1. Commits staan in `_results/commits.txt`. De kloons zijn verwijderd; rapporten en gefilterde logs staan in `<corpus>/corpus-fa/_results/<naam>/` (9 MB).

| Project | Commit | restore | build vóór | tests vóór (pass/fail) | fix | Fixed | Manual | build na | tests na | regressies |
| --- | --- | --- | --- | --- | --- | ---: | ---: | --- | --- | ---: |
| SmartEnum | 9bc3f7a | 0 | 0 | 1293 (1293/0) | 0 | 379 | 62 | 0 | 1293 (1293/0) | 0 |
| FluentResults | 36284d3 | 0 | 0 | 864 (864/0) | 0 | 564 | 76 | 0 | 864 (864/0) | 0 |
| Ardalis.Result | 14cf45a | 0 | 0 | 1068 (1068/0) | 0 | 292 | 49 | 0 | 1068 (1068/0) | 0 |
| CSharpFunctionalExtensions | e0fc908 | 0 | 0 | 4228 (4226/0) | 0 | 4502 | 49 | 0 | 4228 (4226/0) | 0 |
| Facility | 6e147bf | 0 | 0 | 1552 (1552/0) | 0 | 513 | 2 | 0 | 1552 (1552/0) | 0 |
| Ardalis.Specification¹ | d2fcf35 | 0 | 0 | 829 (823/0) | 0 | 828 | 45 | 0 | 829 (823/0) | 0 |
| Bogus | 6ece18c | 0 | 0 | 3155 (3149/0) | 0 | 844 | 115 | 0 | 3155 (3149/0) | 0 |
| NUKE | 27e8077 | 0 | 0 | 398 (391/0) | 0 | 339 | 34 | 0 | 398 (391/0) | 0 |
| Mapperly (v3.6.0) | 51aa1da | 0 | 0 | 1094 (1094/0) | 0 | 114 | 6 | 0 | 1094 (1094/0) | 0 |
| PowerApps-Tooling | 630e697 | 0 | 0 | 1767 (1767/0) | 0 | 293 | 103 | 0 | 1767 (1767/0) | 0 |
| Polly (7.2.4) | b1a5a17 | 0 | 0 | 5685 (5685/0) | 0 | 4449 | 964 | 0 | 5685 (5685/0) | 0 |
| EventFlow (v0.83.4713)² | e544b76 | 0 | 0 | 970 (642/326) | **1** → 0 | 483 | 59 | 0 | 970 (642/326) | 0 |
| modular-monolith³ | 91c8ef2 | 0 | 0 | 287 (246/40) | 0 | **0** | 82 | 0 | 287 (246/40) | 0 |
| LiteDB⁴ | 7b71bc4 | 0 | 0 | 4928×2 TFM groen | 0 | 6874 | 357 | **1** | – | – |
| Steeltoe | f13e514 | 0 | 0 | 7 assemblies rood (baseline) | 0 | 5071 | 529 | **1** | – | – |
| Akka.NET | 23a8566 | 0 | 0 | 2–3 assemblies rood (baseline) | 0 | 8488 | 1868 | **1** | – | – |

Fixed/Manual zonder `project-file`- en `usings`-findings. Testtellingen zijn over alle TFM's opgeteld; "fail" telt alleen echte falers (overgeslagen tests niet).

¹ `.sln` faalt als de map niet `Specification` heet (`ProjectReference ..\..\..\Specification\…`); opnieuw gedraaid met die mapnaam.
² De fix crashte op een Latin-1-bestand (TAAK-167). Na handmatige conversie naar UTF-8: groen. De 326 falers zijn integratietests zonder database/EventStore (hangende testhosts na 4 min gestopt).
³ Het sqlproj-project uit de solution gehaald (MSBuild.Sdk.SqlProj ondersteunt SDK 10 niet). Alle 82 findings Manual omdat het zelfsluitende `<Project Sdk=".." />` niet bewerkt kan worden (bestaande TAAK-181). Groen, maar er is niets gemigreerd.
⁴ Eerste poging faalde in de baseline door een file-lock (CS2012, LiteDB.Fuzz parallel gebouwd); opnieuw met `-m:1`.

Bouwfouten na de fix:

| Project | Fout | Oorzaak | Taak |
| --- | --- | --- | --- |
| Akka.NET | 4 984× CS0121 (+ enkele CS8130/CS0119/CS1061/CS0452) | `Akka.TestKit` heeft een eigen `ShouldBe`/`ShouldBeTrue`; met `using Shouldly;` erbij is die dubbelzinnig | TAAK-174 |
| LiteDB | 80× CS1929, 8× CS0411 (11 regels) | `BsonValue.ShouldBe(int/string)`: een eigen impliciete conversie, dus geen typeinferentie | TAAK-169 |
| Steeltoe | 58× CS0411 (11 regels) | `ShouldContainKey` op `IReadOnlyDictionary`/headers | TAAK-168 |
| EventFlow | fix exit 1 | niet-UTF-8-bestand → unhandled exception | TAAK-167 |

### Herhaling LiteDB na TAAK-223 (2026-10-13)

Zelfde harnas en commit, tool uit de werkboom (`EXTRA=-m:1`), resultaten `<corpus>/corpus-fa/_results/r5-litedb/LiteDB/`.
Fixed 6917 → 7649, Manual 291 → 297, unmatched-use 709 → 2 (`Index_Tests.cs`), usings 15 → 2. Build na fix groen;
tests 9893/26, de 26 regressies zijn dezelfde `Int32IdAssertions_Tests` (TAAK-222).

Grootste Manual-groepen (alle projecten samen):

| Reden | Aantal | Waar vooral | Taak |
| --- | ---: | --- | --- |
| Resultaat van `Throw<T>()` gebruikt (`.And/.Which/.Where/.WithInnerException`) | 499 | Polly 428 | TAAK-170 |
| `ContainKey(..)` met resultaat gebruikt (`WhoseValue`), `ContainKeys`/`ContainValues` | 377 | Steeltoe 249, Polly 120 | TAAK-171 |
| `BeEquivalentTo` op een collectie: volgorde | 207 | 13 projecten | TAAK-172 |
| `AssertionScope` (blokken met helpers/deconstructie) | 190 | LiteDB 140 | – (bewust Manual) |
| Subject gebouwd met `Awaiting`/`FluentActions` voor `NotThrowAsync` | 131 | Polly | TAAK-173 |
| Projectbestand niet bewerkbaar (zelfsluitend) | 61 (+21 cascade) | modular-monolith | TAAK-181 |

## Conclusie ronde 3

- **Fixed: 88,9%** van alle FA-findings (33 900 Fixed / 4 249 Manual, 16 solutions; zonder Akka, Steeltoe en LiteDB 92,5%). Calls die niet herkend werden zitten niet in deze noemer (zie `ANALYSE-fa-100.md`).
- **Volledig groen na de fix (bouwt, 0 testregressies, iets gemigreerd): 12 van 16**: SmartEnum, FluentResults, Ardalis.Result, CSFE, Facility, Specification, Bogus, NUKE, Mapperly, PowerApps-Tooling en Polly zonder ingreep, EventFlow na UTF-8-conversie. modular-monolith bouwt en heeft 0 regressies, maar er is niets omgezet.
- **0 testregressies** in alle 13 projecten waar vóór en na vergeleken kon worden (samen ruim 24 000 testuitvoeringen). In ronde 1 waren dat er 26 (Polly) en 4 (FluentResults).
- Rood: Akka.NET, LiteDB en Steeltoe bouwen niet na de fix (TAAK-174/169/168), en EventFlow crasht op encoding (TAAK-167). Daarmee is "100% FA" nog niet bewezen. Na TAAK-167..174 de run herhalen: met TAAK-170..173 komt Fixed op ongeveer 92%.

## Herhaling Polly na TAAK-170/173 (2026-10-13)

Polly 7.2.4 (`b1a5a17`, 2026-10-13, harnas zonder kloon-wijzigingen): Fixed 4449 → 5255, Manual 964 → 171, build 0 fouten, tests 5685/5685 → 5685/5685, 0 regressies. Resterende top-Manuals: Invoking-receiver niet stabiel (42), `ContainInOrder` (34), verschillende compile-time typen bij `BeEquivalentTo` (20).

## Herhaling Akka na TAAK-174 (2026-10-13)

Akka.NET `23a8566` (verse kloon, zelfde build-flags als het harnas): Fixed 8 488 → **5 463**, Manual 1 868 → **5 126** (3 449 daarvan: bestand Manual door botsing met `Akka.TestKit.AkkaSpecExtensions`), build **0 fouten** (was 4 984× CS0121 + 28 gevolgfouten). Tests: 2 echte regressies (TAAK-199: eigen `ShouldBe` in omsluitende namespace verdringt Shouldly stil; TAAK-200: `object`-subject `ShouldBe` in DData), 2 flaky (poortconflict, MultiNode-timeout). Conflict-Manuals verkleinen: TAAK-198.

## Herhaling Akka na TAAK-198..200 (2026-10-13)

Schone checkout `23a8566` (`<probe-dir>/probe-174`): Fixed 5 463 → 6 016, Manual 5 518 → 4 649 (conflict-Manuals nu per call: 2 943). Build 0 fouten. Akka.Tests 1553/0, Akka.TestKit.Tests 337/0, DData 201/1 (`ReplicatorMessageSerializerSpec`: asymmetrische `Equals` in Akka zelf, nu als waarschuwing in het rapport, TAAK-200). 173 waarschuwingen voor de `Equals`-volgorde.

## Ronde 4 (2026-10-13)

Volledig corpus opnieuw, main `04f113b`. De tool is gepubliceerd uit een schone worktree van HEAD (LF-checkout). Dezelfde 16 commits uit `_results/commits.txt` (shallow fetch per commit, `nuget.config` met alleen nuget.org). Harnas: `<corpus>/corpus-fa/tools/corpus-regression-r4.sh` (= ronde 3 met `py -3`; `NuGetAudit=false`, `AllowMissingPrunePackageData=true`, 3 jobs, test 2× vóór, `fix --force`, bin/obj weg, build, test, vergelijking per test via TRX). `TEMP`/`TMP` op een map buiten `%TEMP%`. Uitzonderingen zoals in ronde 3: Specification in een map `Specification`, LiteDB met `-m:1`, EventFlow met de testhost-watchdog. Bij modular-monolith zijn nu **beide** MSBuild.Sdk.SqlProj-projecten (`Database` en `Database.Build`) uit de `.sln` gehaald; met alleen de eerste faalde de baseline-build. EventFlow draaide **zonder** UTF-8-ingreep (TAAK-167). Resultaten: `<corpus>/corpus-fa/_results/r4/<naam>/` (rapporten, compare.json, gefilterde logs). De kloons zijn verwijderd.

| Project | Fixed | Fixed+waarsch. | Manual | unmatched-use | Fixed % | build na | tests vóór (pass/fail) | tests na (pass/fail) | regressies |
| --- | ---: | ---: | ---: | ---: | ---: | --- | --- | --- | ---: |
| SmartEnum | 342 | 36 | 47 | 1 | 88,7 | 0 | 1293/0 | 1293/0 | 0 |
| FluentResults | 614 | 0 | 20 | 59 | 88,6 | 0 | 864/0 | 864/0 | 0 |
| Ardalis.Result | 286 | 1 | 45 | 0 | 86,4 | 0 | 1068/0 | 1068/0 | 0 |
| CSharpFunctionalExtensions | 4494 | 32 | 23 | 4 | 99,4 | 0 | 4226/0 | 4226/0 | 0 |
| Facility | 512 | 0 | 0 | 0 | 100 | 0 | 1552/0 | 1552/0 | 0 |
| Ardalis.Specification | 840 | 0 | 25 | 9 | 96,1 | 0 | 823/0 | 823/0 | 0 |
| Bogus | 861 | 2 | 92 | 1 | 90,3 | 0 | 3149/0 | 3149/0 | 0 |
| NUKE | 334 | 11 | 20 | 0 | 94,5 | 0 | 389/2 | 389/2 | 0 |
| Mapperly | 112 | 1 | 3 | 3 | 95,0 | 0 | 1094/0 | 1094/0 | 0 |
| PowerApps-Tooling | 291 | 5 | 92 | 76 | 63,8 | 0 | 1767/0 | 1767/0 | 0 |
| Polly | 5238 | 17 | 170 | 1 | 96,8 | 0 | 5685/0 | 5685/0 | 0 |
| EventFlow¹ | 451 | 15 | 44 | 24 | 87,3 | 0 | 642/326 | 642/326 | 0 |
| modular-monolith² | 20 | 41 | 1 | 0 | 98,4 | 0 | 246/40 | 246/40 | 0 |
| LiteDB | 6787 | 130 | 291 | 709 | 87,4 | 0 | 9919/0 | 9893/26 | **26** |
| Steeltoe | 5060 | 2 | 469 | 41 | 90,8 | 0 | 6099/21 | 6081/39 | **18** |
| Akka.NET³ | 5797 | 175 | 4310 | 304 | 56,4 | 0 | 8981/4 | 8973/12 | **10** |
| **Totaal** | **32 039** | **468** | **5 652** | **1 232** | **82,5** | 16/16 | | | 54 |

Zonder `project-file`- en `usings`-findings. "unmatched-use" = FA-call die geen pattern kent (in ronde 3 nog niet als finding gerapporteerd, daardoor niet in de noemer). Fixed % = (Fixed + Fixed met waarschuwing) / alle findings. Testtellingen zijn over alle TFM's opgeteld; overgeslagen tests tellen niet mee.

¹ 326 falers = integratietests zonder database/EventStore (zelfde als ronde 3).
² De 40 falers zijn al rood in de baseline (integratietests). Eindelijk iets gemigreerd (TAAK-181).
³ Daarnaast 29 "missing" (testnamen met tijdstempel/GUID, geen echte verdwijning) en 61 flaky. Van de 10 regressies is 1 de bekende DData-`Equals`-asymmetrie (TAAK-200, wordt in het rapport als waarschuwing gemeld).

Testregressies (geen bouwfouten meer):

| Project | Tests | Oorzaak | Taak |
| --- | --- | --- | --- |
| Akka.NET | 9 in Akka.Streams.Tests (QueueSourceSpec ×5, FlowScan, FlowSupervision, FlowWatchTermination, SinkForeachAsync) | FA `Throw<T>()` pakt `AggregateException` uit, Shouldly niet; de Manual-regel voor blokkerende delegates mist lambda-parameters (`_ => _.Wait(..)`), delegates in lokalen en eigen `WaitAsync` | TAAK-220 |
| Akka.NET | 1 (DData `ReplicatorMessageSerializerSpec`) | asymmetrische `Equals` in Akka, bekend | TAAK-200 (waarschuwing) |
| Steeltoe | 6 × 3 TFM's | `ThrowExactlyAsync<OperationCanceledException>` / `ThrowAsync<TaskCanceledException>().WithInnerException<..>` op een geannuleerde task: Shouldly geeft een eigen `TaskCanceledException` (type en inner exception weg) | TAAK-221 |
| LiteDB | 13 × 2 TFM's (`Int32IdAssertions_Tests`) | meta-test: `Throw<XunitException>()` op een eigen assertion-helper in een ander bestand; die helper is naar Shouldly gemigreerd en gooit nu `ShouldAssertException` | TAAK-222 |

Grootste Manual-groepen (alle projecten samen):

| Reden | Aantal | Waar | Gerechtvaardigd? | Taak |
| --- | ---: | --- | --- | --- |
| Shouldly-call dubbelzinnig/verkeerd gebonden met eigen `Should*` (CS0121, niet geïmporteerd) | ≈ 2 900 | Akka | ja (TAAK-174/198/199) | – |
| `x.Should()` niet gemigreerd: bestand alleen in de eerste TFM (net462) bekeken, code staat in `#if !NETFRAMEWORK` | 709 (593 `Should()`) | LiteDB | **nee** | TAAK-223 |
| FA-tijdextensies `n.Seconds()`/`Milliseconds()` | 380 | Akka, Steeltoe | **nee** | TAAK-224 |
| `Equal(..)` op een dictionary | 344 | Akka 337 | **nee** | TAAK-225 |
| `BeEquivalentTo(params items)` met niet-leaf-items, `UniqueAddress`/`KeyValuePair`/`Member`/… | ≈ 520 | Akka | ja (structurele vergelijking) | – |
| `ContainKey(..)`-resultaat gebruikt / `WhoseValue` met berekende sleutel | 148 + 77 | Steeltoe | deels (berekende sleutel: kandidaat voor later) | – |
| `Be(..)` op `FluentAssertions.Types` (andere gelijkheid) | 82 | 7 projecten | ja | – |
| `ContainInOrder` | 67 | Polly, Akka | ja (geen equivalent) | – |
| `AssertionScope` (blok + asserts erin) | 47 + 102 | LiteDB, PowerApps | ja | – |

## Conclusie ronde 4

- **Alle 16 solutions bouwen na de fix** (ronde 3: 13). Akka, LiteDB en Steeltoe zijn opgelost door TAAK-167..200; EventFlow draait zonder ingreep.
- **Volledig groen (bouwt, 0 testregressies, iets gemigreerd): 13 van 16**: SmartEnum, FluentResults, Ardalis.Result, CSFE, Facility, Specification, Bogus, NUKE, Mapperly, PowerApps-Tooling, Polly, EventFlow en nu ook modular-monolith (62 omgezet). Rood door testregressies: LiteDB (26), Steeltoe (18), Akka (10; 9 nieuw, 1 bekend). Samen 54 regressies op ±48 800 testuitvoeringen, met 3 oorzaken: TAAK-220/221/222.
- **Fixed: 82,5%** van alle findings (32 507 van 39 391), zonder de nieuwe unmatched-use-rijen 85,2%. Zonder Akka is het 92,1%. Ten opzichte van ronde 3 (88,9%) lijkt dit een daling, maar die komt doordat de Akka-calls die in ronde 3 de build braken, nu eerlijk Manual zijn (≈ 2 900 conflict-Manuals) en doordat niet-herkende calls nu meetellen.
- "100% FA" is nog niet bewezen. Eerst TAAK-220..222 (regressies), dan TAAK-223..225 (samen ≈ 1 430 Manual, +3,6 procentpunt).

## Zie ook

- `claude/tasks/TAAK-220-throw-aggregateexception-uitpakken.md` t/m `claude/tasks/TAAK-225-equal-op-dictionary.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/ANALYSE-fa-100.md`
- `claude/tasks/archief/TAAK-167-niet-utf8-bestand-breekt-fix.md` t/m `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`

## Ronde 5 (2026-10-13): herhaling Akka, Steeltoe, LiteDB na TAAK-220..222

Tool gepubliceerd uit de **werkmap** (niet uit een commit): main `04f113b` plus de niet-gecommitte wijzigingen van TAAK-220..222 én de lopende TAAK-223-wijziging in `Fixer.cs` (multi-TFM-passes) van een andere agent. Zelfde harnas (`<corpus>/corpus-fa/tools/corpus-regression-r4.sh`), dezelfde commits, LiteDB met `-m:1`, 3 projecten tegelijk. Resultaten: `<corpus>/corpus-fa/_results/r5/<naam>/`.

| Project | Fixed | Manual | build na | tests vóór (pass/fail) | tests na (pass/fail) | regressies |
| --- | ---: | ---: | --- | --- | --- | ---: |
| LiteDB | 7649 | 299 | 0 | 9918/1 | 9919/0 | **0** (was 26) |
| Steeltoe | 5059 | 516 | 0 | 5984/21 | 6099/21 | **0** (was 18) |
| Akka.NET | 5962 | 4624 | 0 | 8976/6 | 8984/1 | **1** (was 10; alleen de bekende DData-`Equals`, TAAK-200) |

Zonder `project-file`/`usings`. LiteDB: Fixed 6917 → 7649 komt vooral door TAAK-223. Steeltoe "na" heeft 123 tests meer (testnamen met variabele delen, als "new"/flaky geteld, geen regressie). Nieuwe Manual-redenen: LiteDB 25 (helper + meta-tests, TAAK-222), Akka 23 + 2 (aggregate/`WaitAsync`, TAAK-220) en 6 (TAAK-222-regel), Steeltoe 6 (geannuleerde task, TAAK-221).

Conclusie: de drie oorzaken van ronde 4 zijn weg; met ronde 4 samen zijn nu 15 van 16 solutions groen, Akka houdt alleen de bekende TAAK-200-waarschuwing.
