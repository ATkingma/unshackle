# Regressie-run corpus (2026-10-11)

Harnas: `claude/tools/corpus-regression.sh` (+ `.py`). Per solution: verse kopie → restore → build → test 2× (TRX, flaky-detectie) → `fix --force` → bin/obj weg → build → test → vergelijking **per test**. Tool gepubliceerd uit main `7edb002` (vóór TAAK-070/118). Uitvoer: `/mnt/dikkeschijf/unshackle-corpus/out/<naam>/`.

| Solution | status | tests vóór (pass/fail) | tests na (pass/fail) | regressies | gefaald→geslaagd | verdwenen | nieuw | flaky | duur |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| akka | fix-faalt | | | | | | | | 906s |
| ardalis | ok | 9 (9/0) | 9 (9/0) | 0 | 0 | 0 | 0 | 0 | 61s |
| aresult | ok | 741 (741/0) | 741 (741/0) | 0 | 0 | 0 | 0 | 0 | 83s |
| bogus | ok | 1074 (1072/0) | 1074 (1072/0) | 0 | 0 | 0 | 0 | 0 | 56s |
| csfe | ok | 4228 (4226/0) | 4228 (4226/0) | 0 | 0 | 0 | 0 | 0 | 96s |
| eshoponweb | ok | 74 (74/0) | 74 (74/0) | 0 | 0 | 0 | 0 | 0 | 66s |
| evo1 | BUILD-NA-FIX-FAALT | | | | | | | | 32s |
| evo2 | BUILD-NA-FIX-FAALT | | | | | | | | 69s |
| fresults | ok | 576 (576/0) | 576 (572/4) | 4 | 0 | 0 | 0 | 0 | 67s |
| litedb | baseline-build-faalt | | | | | | | | 23s |
| nuke | ok | 398 (388/3) | 398 (388/3) | 0 | 0 | 0 | 0 | 0 | 112s |
| polly | ok | 3790 (3790/0) | 3790 (3764/26) | 26 | 0 | 0 | 0 | 0 | 423s |
| powerapps | ok | 0 (0/0) | 0 (0/0) | 0 | 0 | 0 | 0 | 0 | 54s |
| smartenum | ok | 1293 (1293/0) | 1293 (1293/0) | 0 | 0 | 0 | 0 | 0 | 104s |
| steeltoe | fix-faalt | | | | | | | | 829s |


## Conclusie

**Van de 10 solutions waarvan tests vóór en na vergeleken konden worden, zijn 8 per test identiek** (ardalis, Ardalis.Result, Bogus, CSFE, eShopOnWeb, NUKE, SmartEnum; samen ruim 7 800 tests). Twee hadden regressies:

| Solution | Regressies | Oorzaak | Status |
| --- | ---: | --- | --- |
| FluentResults | 4 | Assertion in lambda met `Throw<XunitException>` | **opgelost** in TAAK-070 (na de run gemerged; harnas op FluentResults met de nieuwe tool: 576/576, 0 regressies) |
| Polly | 26 | Eigen helper `Within(timeSpan, Action)` vangt alleen `AssertionFailedException`/`XunitException` om opnieuw te proberen. Shouldly gooit `ShouldAssertException`, die niet gevangen wordt → geen retry → test faalt | **opgelost** in TAAK-140 (harnas op Polly 7.2.4, ongewijzigde bron van tag `7.2.4` in `src/polly`, tool uit main `d252749`: 3790/3790, 0 regressies; uitvoer `/mnt/dikkeschijf/unshackle-corpus/t140/polly/`) |

## Niet vergelijkbaar (geen regressie-oordeel mogelijk)

| Solution | Reden | Soort |
| --- | --- | --- |
| evo1, evo2 | Build na fix faalt (IDE0060: ongebruikte `assembly`-parameter na `AddMediator`; TAAK-117 gaf alleen waarschuwing) | **opgelost** in TAAK-141 (`_ = assembly;`); evo2 heeft daarna nog CS0104/CS0122/CS0436/CS0535 (los probleem) |
| Akka.NET, Steeltoe | Unshackle laadt de solution niet: AOT-/benchmark-/sample-projecten waarvan types niet resolven (302 resp. 502 fouten) | product: loader (TAAK-112/135 dekken dit nog niet) |
| LiteDB | Baseline-build faalt al (`LiteDB.Fuzz`) | omgeving/project |
| PowerApps-Tooling | 0 tests: project gebruikt Microsoft.Testing.Platform; het harnas dwingt VSTest af | harnas |

## Open vervolg (niet gestart, op verzoek gebruiker)

- Loader: Akka/Steeltoe laden (projecten met onopgeloste types overslaan als ze geen FA/MediatR gebruiken; regels uit TAAK-112/135 verbreden).
- evo1/evo2: IDE0060 oplossen in plaats van alleen waarschuwen.
- Harnas: MTP-testprojecten (PowerApps) ondersteunen (`dotnet test` met MTP + TRX-extensie).

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`, `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tools/corpus-regression.sh`
