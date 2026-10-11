# Testcorpus: FluentAssertions in echte projecten

Welke FA-API's echte open-source testcode gebruikt, hoe vaak, en of een patroon ze dekt. Stuurt de prioriteit van
`claude/tasks/patroon-queue.md`. Gemeten op 2026-10-11 met tarballs van GitHub (alleen grep; twee projecten ook
gebouwd en gefixt, zie onder). Telling: elke `.Should().X` (ook over regels heen), per X.

## Projecten

| Project                      | Soort                               | FA   | Bestanden |  Calls | Gedekt |
| ---------------------------- | ----------------------------------- | ---- | --------: | -----: | -----: |
| Akka.NET                     | actor-framework, groot              | 5.10 |       711 | 10 300 |    94% |
| LiteDB                       | embedded database                   | 6.12 |       540 |  7 915 |    86% |
| Steeltoe                     | cloud-native libs (Spring-achtig)   | 7.2  |       265 |  5 751 |    94% |
| Polly (tag 7.2.4)            | resilience-lib                      | 6.11 |        81 |  5 326 |    94% |
| CSharpFunctionalExtensions\* | Result/Maybe                        | 5.10 |       248 |  4 548 |    99% |
| Bogus                        | fake-data generator                 | 6.12 |       104 |    945 |    87% |
| Ardalis.Specification\*      | EF-specificaties                    | 7.2  |        85 |    889 |    85% |
| Mapperly (tag v3.6.0)        | source generator, custom assertions | 6.12 |        85 |    837 |    14% |
| FluentResults                | Result-lib + eigen FA-extensies     | 6.7  |        24 |    675 |    93% |
| EventFlow\* (tag v0.83.4713) | CQRS/ES                             | 5.6  |        91 |    518 |    96% |
| Ardalis.SmartEnum\*          | enums                               | 6.12 |        52 |    419 |    78% |
| NUKE\*                       | build-tool                          | 6.12 |        33 |    367 |    87% |
| Ardalis.Result               | Result-lib + eigen FA-extensies     | 7.0  |        17 |    335 |    95% |
| modular-monolith-with-ddd    | DDD-voorbeeldapp (NUnit)            | 6.12 |        29 |    125 |    50% |

\* al gebruikt in `edge-cases.md`. EventFlow-main en Refit gebruiken geen FA meer (Shouldly); Refit is weggelaten.
Afgevallen (geen FA ≤ 7 in HEAD): Polly-main, Nethermind, Dapr, Jellyfin, IdentityServer4, Orleans (AwesomeAssertions),
ErrorOr (FA 8), ardalis/CleanArchitecture.

**Totaal**: 14 projecten, 38 950 calls, **94,9%** (36 948) naar API door een bestaand patroon herkend (Fixed of bewuste
Manual), gemeten na TAAK-038/039/050/051/052 (`claude/tasks/ANALYSE-fa-100.md`; eerder 90,5%). De kolom "Gedekt" per
project hierboven is nog de oude meting. Plafond ~97%: de rest is vooral eigen assertion-extensies (Mapperly alleen 842 calls).
Lage dekking bij Mapperly en modular-monolith komt door eigen assertion-extensies (`HaveSingleMethodBody`,
`HaveDiagnostic`, `BeJson`; rij #34).

## Frequentie (≥ 3 projecten of ≥ 100 calls)

| API                     | aantal | #projecten | gedekt       |
| ----------------------- | -----: | ---------: | ------------ |
| `Be`                    | 17 444 |         14 | ja           |
| `BeTrue`                |  3 691 |         14 | ja           |
| `BeFalse`               |  2 225 |         13 | ja           |
| `Throw`                 |  1 871 |         11 | ja           |
| `BeEquivalentTo`        |  1 865 |         13 | ja           |
| `Equal`                 |  1 520 |          7 | ja           |
| `Contain`               |    953 |         10 | ja           |
| `BeOfType`              |    862 |         12 | ja           |
| `BeNull`                |    846 |         14 | ja           |
| `BeEmpty`               |    802 |         11 | ja           |
| `NotBeNull`             |    784 |         11 | ja           |
| `HaveCount`             |    595 |         12 | ja           |
| `ThrowAsync`            |    561 |          5 | ja           |
| `BeSameAs`              |    482 |          9 | ja           |
| `ContainSingle`         |    453 |          8 | ja           |
| `HaveSingleMethodBody`  |    433 |          1 | nee (custom) |
| `NotThrow`              |    379 |         10 | ja           |
| `ContainKey`            |    342 |          6 | ja           |
| `BeGreaterThan`         |    269 |          7 | ja           |
| `NotBe`                 |    216 |          8 | ja           |
| `NotContain`            |    179 |          6 | ja           |
| `NotThrowAsync`         |    150 |          4 | ja           |
| `HaveMapMethodBody`     |    139 |          1 | nee (custom) |
| `HaveDiagnostic`        |    138 |          1 | nee (custom) |
| `BeJson`                |    132 |          1 | nee (custom) |
| `OnlyContain`           |    131 |          6 | ja           |
| `StartWith`             |    105 |          7 | ja           |
| `NotBeEmpty`            |     98 |          7 | ja           |
| `ThrowExactly`          |     75 |          5 | ja           |
| `BeLessThan`            |     72 |          7 | ja           |
| `BeGreaterOrEqualTo`    |     68 |          5 | ja           |
| `NotBeNullOrEmpty`      |     67 |          8 | ja           |
| `ContainInOrder`        |     67 |          4 | Manual+code  |
| `BeInRange`             |     60 |          4 | ja           |
| `NotBeSameAs`           |     54 |          8 | ja           |
| `BeAssignableTo`        |     52 |          5 | ja           |
| `BePositive`            |     23 |          5 | ja           |
| `BeLessOrEqualTo`       |     23 |          4 | ja           |
| `ContainEquivalentOf`   |     22 |          5 | open #33     |
| `EndWith`               |     21 |          5 | ja           |
| `NotBeNullOrWhiteSpace` |     21 |          3 | ja           |
| `MatchRegex`            |     18 |          3 | ja           |
| `BeNullOrEmpty`         |     18 |          4 | ja           |
| `BeCloseTo`             |     16 |          5 | ja           |
| `Match`                 |     14 |          4 | ja           |
| `HaveLength`            |     13 |          3 | ja           |
| `NotEqual`              |     11 |          3 | ja           |
| `NotBeOfType`           |      9 |          3 | ja           |
| `BeOneOf`               |      9 |          3 | ja           |
| `HaveCountGreaterThan`  |      4 |          3 | ja           |

Ketens na een assertion: `.And` 726, `.Where` 558 (meestal `Throw<T>().Where(...)`), `.WithMessage` 460, `.Which` 301,
`.WhoseValue` 257, `.Subject` 184 (63× na `ContainSingle`, 47× na `BeOfType`), `.WithInnerException` 46.
"Gedekt = ja" betekent: het patroon herkent de API; afhankelijk van type en keten kan het Manual worden.

## Fix-run op twee projecten

Werkwijze: `dotnet restore` + baseline build/test (net8.0) → `unshackle fix <sln> --fix fluentassertions --force` →
build/test. Tests in de map met een eigen `global.json` (VSTest i.p.v. MTP).

| Project        | Baseline         | Fixed | Manual | Bouwfouten na fix                | Regressies na fix        |
| -------------- | ---------------- | ----: | -----: | -------------------------------- | ------------------------ |
| FluentResults  | 288 groen        |   567 |     64 | geen                             | 2 (TAAK-070)             |
| Ardalis.Result | 373 groen (\*\*) |   281 |     47 | CS0246 in 2 bestanden (TAAK-073) | geen (na handmatige fix) |

(\*\*) `SampleWeb` net6.0 bouwt al in de baseline niet (geen .NET 6-runtime voor de OpenAPI-tool); niet meegeteld.

Manual-redenen (samen 111): `BeEquivalentTo` met "becauseArgs" 50 (onterecht, TAAK-071), runtime-/BCL-type 14,
cyclische graaf 12, verschillende typen 10, `project-file` 6, `WithMessage` niet-constant 4, `AssertionScope` 4,
collectie-volgorde 3, rest ≤ 2.

Bevindingen:

- **Regressie** (TAAK-070): `Action act = () => x.Should().Be(y); act.Should().Throw<XunitException>();` — de binnenste
  assertion wordt Shouldly en gooit een ander exception-type.
- **Foute Manual-reden** (TAAK-071): `coll.Should().BeEquivalentTo("a", "b")` (params-overload) heet "becauseArgs". Opgelost: collectie-reden.
- **Bouwfout** (TAAK-073): bestand met alleen een ongebruikte `using FluentAssertions;` houdt die using terwijl het
  pakket uit het project verdwijnt. Opgelost: de Fixer verwijdert die using ook zonder findings.
- **Laden**: een solution met een niet-gerestored `net48`-project faalt in zijn geheel ("reference assemblies for
  .NETFramework 4.8 not found"). Na `dotnet restore` werkt het. Kandidaat voor een duidelijkere melding in de CLI.

## Fixed-ratio (scan, `claude/tasks/ANALYSE-fa-100.md`)

Herkend is niet hetzelfde als Fixed. Scan met `--fix fluentassertions` op de solutions die laden; Fixed % = Fixed / (Fixed + Manual + niet herkend):

| Project                    |     Fixed |  Manual | niet herkend |   Fixed % |
| -------------------------- | --------: | ------: | -----------: | --------: |
| CSharpFunctionalExtensions |     4 448 |      52 |           36 |     98,1% |
| FluentResults              |       567 |      61 |           50 |     83,6% |
| Ardalis.Result             |       275 |      44 |           17 |     81,8% |
| PowerApps-Tooling (casus)  |        29 |     109 |           45 |     15,8% |
| SampleShop                 |        62 |      22 |            5 |     69,7% |
| **totaal**                 | **5 381** | **285** |      **153** | **92,5%** |

Na TAAK-092..101 (which-ketens, becauseArgs, hoisten, `fa.match`, AssertionScope → `ShouldSatisfyAllConditions`,
direct mappings): SampleShop 62 → **69** Fixed (Manual 24 → 16), PowerApps 29 → **55** Fixed (Manual 109 → 93).
De andere projecten zijn daarna niet opnieuw gescand. Cijfers overgenomen uit de bron; de Manual-kolom telt op tot 288, niet 285 (afwijking in de bron).

## Corpus-run (fix + build + test)

FA (`claude/tasks/CORPUSRUN-fa.md`, main 40cf91e): van 15 solutions konden er 9 draaien; daarvan bouwden er na `fix` 2
(SmartEnum, FluentResults). Vier braken omdat de code herschreven was terwijl het projectbestand niet aangepast kon worden
(TAAK-111/134, opgelost in 2962f95); verder TAAK-110 (CSFE), TAAK-112 (Bogus laadt niet), TAAK-113 (PowerApps). De run is
na die fixes nog niet herhaald.

MediatR (`claude/tasks/CORPUSRUN-mediatr.md`): 0 van 9 volledig gemigreerd, vooral door projectbestand-blokkades
(TAAK-134, inmiddels textueel splicen) en TAAK-130/131/132/133/136.

## Zie ook

- `docs/projectinfo/architectuur/edge-cases.md` — afwijkende vormen → `edge-*` proofs
- `claude/tasks/ANALYSE-fa-100.md`, `claude/tasks/CORPUSRUN-fa.md`, `claude/tasks/CORPUSRUN-mediatr.md` — bronnen van de cijfers
- `claude/tasks/patroon-queue.md` — wachtrij, prioriteit volgt deze tabel
- `claude/tasks/TAAK-070-fa-assertion-in-throw-lambda.md` t/m `TAAK-073-unused-fa-using-after-package-removal.md`
