# Analyse: FluentAssertions → Shouldly naar 100%?

Gemeten 2026-10-11 op `main` 194ea0f. Alleen analyse; geen code gewijzigd.

## Kort antwoord

**Nee, 100% kan niet.** Het realistische plafond is **~97–98% Fixed**. Nu zitten we op **92,5%**.

- De laatste ~2–3% zijn eigen assertion-extensies van de gebruiker (bv. `HaveSingleMethodBody` bij Mapperly en `BeFailure` bij FluentResults). Ook `BeEquivalentTo` hoort erbij als het iets vergelijkt waar Shouldly niets voor heeft: opties, cyclische grafen, verschillende typen of runtime-typen. Hier versoepelt een automatische rewrite de test of breekt hij hem. Volgens onze eigen regel ("een pattern gooit nooit; twijfel → Manual") blijven die Manual.
- Van de rest is ongeveer twee derde haalbaar zonder semantiekverschil. Het meeste valt in drie goedkope acties: `BeEquivalentTo` op collecties met `ignoreOrder`, `BeSameAs` en `NotThrowAsync`.

## Werkwijze

- **A (herkenning)**: elke `.Should().X` over regels heen geteld (`grep -Pzo`) in de 14 corpus-tarballs (38 950 calls). Die zijn vergeleken met de FA-methodenamen die de patronen in `src/Unshackle.Core/Fixes/FluentAssertions/` herkennen.
- **B (Fixed-ratio)**: `dotnet restore` + `unshackle scan <sln> --fix fluentassertions --report …json` op 10 solutions. Rapporten staan in `scratchpad/fa100/`.
- Shouldly-doelen gecontroleerd op methodenaam in `~/.nuget/packages/shouldly/4.3.0/lib/net8.0/Shouldly.dll` (metadata-grep, geen reflectie op overloads).

## A. Herkenning: FA-API's zonder patroon

Herkend: **94,9%** (36 948 / 38 950). In `corpus.md` stond nog 90,5%; dat cijfer is van vóór TAAK-038/039/050/051/052. Er blijven **2 002 calls** over:

| API                                                                                                      | aantal | #proj | cat | Shouldly-equivalent / alternatief                                                      |
| -------------------------------------------------------------------------------------------------------- | -----: | ----: | :-: | -------------------------------------------------------------------------------------- |
| `BeSameAs`                                                                                               |    482 |     9 |  1  | `ShouldBeSameAs` ✓                                                                     |
| `HaveSingleMethodBody` (Mapperly)                                                                        |    433 |     1 |  3  | custom                                                                                 |
| `NotThrowAsync`                                                                                          |    150 |     4 |  1  | `await Should.NotThrowAsync(act)` ✓                                                    |
| `HaveMapMethodBody` / `HaveDiagnostic` / `BeJson` (Mapperly)                                             |    409 |     1 |  3  | custom                                                                                 |
| `ContainInOrder`                                                                                         |     67 |     4 |  3  | geen equivalent → Manual of shim                                                       |
| `NotBeSameAs`                                                                                            |     54 |     8 |  1  | `ShouldNotBeSameAs` ✓                                                                  |
| `HaveElementAt`                                                                                          |     36 |     1 |  1  | `x.ElementAt(i).ShouldBe(v)`                                                           |
| `BeFailure` / `BeSuccess` / `HaveValue` (FluentResults-eigen)                                            |     45 |     1 |  3  | custom                                                                                 |
| `BeOnOrAfter` / `BeOnOrBefore` / `BeAfter` / `BeBefore`                                                  |     54 |     2 |  1  | `ShouldBeGreaterThanOrEqualTo` / `…LessThanOrEqualTo` / `…GreaterThan` / `…LessThan` ✓ |
| `HaveSameCount`                                                                                          |     21 |     1 |  1  | `x.Count().ShouldBe(y.Count())`                                                        |
| `ContainEquivalentOf`                                                                                    |     22 |     5 |  2  | value-equality-type → `ShouldContain(item)`; anders 3                                  |
| `MatchRegex` / `NotMatchRegex`                                                                           |     19 |     3 |  1  | `ShouldMatch` / `ShouldNotMatch` ✓                                                     |
| `Match` (wildcard)                                                                                       |     14 |     4 |  1  | constante → regex + `ShouldMatch`; niet-constant → 3                                   |
| `CompleteWithinAsync`                                                                                    |     10 |     1 |  2  | `Should.CompleteIn` ✓ (blokkeert sync, andere timeout-melding)                         |
| `HaveCountGreaterThan` / `…OrEqualTo` / `…LessOrEqualTo`                                                 |     10 |     3 |  1  | `x.Count().ShouldBeGreaterThan(n)`                                                     |
| `BeOneOf`                                                                                                |      9 |     3 |  1  | `ShouldBeOneOf` ✓                                                                      |
| `BeSubsetOf`                                                                                             |      8 |     2 |  1  | `ShouldBeSubsetOf` ✓                                                                   |
| `OnlyHaveUniqueItems`                                                                                    |      7 |     1 |  1  | `ShouldBeUnique` ✓                                                                     |
| `HaveValue` / `NotHaveValue` (Nullable)                                                                  |     11 |     2 |  1  | `ShouldNotBeNull` / `ShouldBeNull`                                                     |
| `BeInAscendingOrder` / `BeInDescendingOrder`                                                             |      6 |     1 |  1  | `ShouldBeInOrder(SortDirection…)` ✓; met comparer → 3                                  |
| `Satisfy` / `SatisfyRespectively`                                                                        |      5 |     2 |  1  | lambda-body inline op het subject                                                      |
| `AllBeEquivalentTo`                                                                                      |      4 |     2 |  2  | `ShouldAllBe(i => i == v)` alleen voor value-equality                                  |
| `HaveFlag` / `NotHaveFlag`                                                                               |      4 |     1 |  1  | `ShouldHaveFlag` / `ShouldNotHaveFlag` ✓                                               |
| `AllBeOfType` / `AllBeAssignableTo`                                                                      |      4 |     2 |  1  | `ShouldAllBe(i => i.GetType() == typeof(T))` / `i is T`                                |
| `NotIntersectWith` / `NotContainAny`                                                                     |      4 |     2 |  1  | `x.Intersect(y).ShouldBeEmpty()`                                                       |
| `Equals`                                                                                                 |      2 |     1 |  3  | `object.Equals` op `ObjectAssertions` is een bug in de test → Manual                   |
| overige custom (git-warnings, NetArchTest `NotHaveDependencyOn`, `HaveNameEndingWith`, `NotBePublic`, …) |   ~150 |   1–2 |  3  | custom                                                                                 |

Samengevat over de herkenning (corpus, 38 950 calls):

|                                                     |  calls |     % |
| --------------------------------------------------- | -----: | ----: |
| nu herkend                                          | 36 948 | 94,9% |
| cat 1 erbij                                         |   +898 | 97,2% |
| cat 2 erbij                                         |    +36 | 97,3% |
| cat 3 (custom 999, `ContainInOrder` 67, `Equals` 2) |  1 068 |  2,7% |

Mapperly alleen al is 842 van de 999 custom calls. Zonder Mapperly ligt het herkenningsplafond rond 99%.

## B. Fixed-ratio

| Project                                           | geladen |     Fixed | Manual* | niet herkend |   Fixed % |
| ------------------------------------------------- | ------- | --------: | ------: | -----------: | --------: |
| CSharpFunctionalExtensions                        | ja      |     4 448 |      52 |           36 |     98,1% |
| FluentResults                                     | ja      |       567 |      61 |           50 |     83,6% |
| Ardalis.Result                                    | ja      |       275 |      44 |           17 |     81,8% |
| PowerApps-Tooling (casus)                         | ja      |        29 |     109 |           45 |     15,8% |
| SampleShop                                        | ja      |        62 |      22 |            5 |     69,7% |
| **totaal (5)**                                    |         | **5 381** | **285** |      **153** | **92,5%** |
| SmartEnum, Specification, Bogus, Polly, EventFlow | **nee** |           |         |              |           |

\* zonder `project-file`-findings (14; die volgen uit andere Manuals).
Noemer = Fixed + Manual + niet-herkende calls. Een `.And`-keten telt als één finding, dus de cijfers zijn ±1%.

**Laadfouten (bevinding los van patronen)**: 5 van de 10 solutions laden niet. Bij Polly, EventFlow en SmartEnum komt dat door NuGet-audit (`NU1902`/`NU1903`, kwetsbaar pakket, "Warning As Error"). Bogus faalt op `NU1701` (net4x-pakket in een net9-project). Specification mist een project in de tarball, en SmartEnum heeft ook nog "no Compile target". `dotnet build` slaagt op Polly en EventFlow, maar onze loader breekt af op deze warnings. Voor echte gebruikers is dat erger dan elke Manual. Zie actie 0.

### Manual-redenen (285)

| Reden                                                                                                                                                                          | aantal | patroon                           |                   cat                    | Beste alternatief                                                                                                                                         |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -----: | --------------------------------- | :--------------------------------------: | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BeEquivalentTo(params)` / collectie: volgorde genegeerd                                                                                                                       |     93 | be-equivalent-to-object           | 1 (strings/ints/enums/records), 3 (rest) | `x.ShouldBe(expected, ignoreOrder: true)` ✓ (multiset, net als FA). Gemeten: vrijwel allemaal `string`/`int`.                                             |
| Binnen `AssertionScope` (be-true-false 24, should-be 6, have-count 1, be-greater-less 1) + de scope zelf 17                                                                    |     49 | assertion-scope e.a.              |                    2                     | hele `using`-blok → `ShouldSatisfyAllConditions(() => …, …)` ✓. Waarschuwing: niet-assertion-code in het blok valt binnen de lambda's.                    |
| Resultaat gebruikt (`.Which`/`.And`/waarde): ContainSingle 9, Throw/Invoking 11, Be 4, BeOfType 4, NotBeNull 2, NotBeEmpty 2, BeEquivalentTo 2, HaveCount/Contain/ContainKey 3 |     37 | diverse                           |                    1                     | queue #19 `fa.which-chain`: statement splitsen, waarde-teruggevende Shouldly-call (`ShouldBeOfType`, `ShouldHaveSingleItem`, `Should.Throw`) in een local |
| Niet-plain type (object/interface/BCL)                                                                                                                                         |     14 | be-equivalent-to-object           |                    3                     | Manual (runtime-leden bepalen de vergelijking)                                                                                                            |
| Cyclische graaf                                                                                                                                                                |     12 | be-equivalent-to-object           |                    3                     | Manual; Shouldly is hier soepeler                                                                                                                         |
| Verschillende typen / anoniem verwacht                                                                                                                                         |     12 | be-equivalent-to-object           |                    3                     | Manual met advies: per property `ShouldBe`. Een shim is mogelijk, maar dan bouw je FA na.                                                                 |
| becauseArgs / named args                                                                                                                                                       |     13 | should-be, have-count, be-null, … |                    1                     | queue #22: `string.Format(because, args)`                                                                                                                 |
| Niet in vorm `x.Should().X` (na `.And`), `.And` met onbekende X, subject met side-effects                                                                                      |     12 | and-chain, all-satisfy, …         |                    1                     | subject eerst in `var tmp = …;` (evalueert één keer, net als FA); volgt verder de andere patronen                                                         |
| `BeNullOrEmpty` / `NotBeNullOrEmpty` op collectie                                                                                                                              |     11 | string-start-end                  |                    1                     | `x?.ShouldBeEmpty();` / `x.ShouldNotBeNull(); x.ShouldNotBeEmpty();`                                                                                      |
| `Contain(IEnumerable)`                                                                                                                                                         |      6 | contain                           |                    2                     | `expected.ShouldBeSubsetOf(x)` ✓ (zelfde semantiek, melding omgedraaid)                                                                                   |
| `WithMessage` niet-constant                                                                                                                                                    |      4 | throw                             |                    3                     | Manual; eventueel een runtime-wildcardhelper (shim)                                                                                                       |
| Invoking-receiver met side-effects                                                                                                                                             |      4 | invoking                          |                    1                     | receiver eerst in een local (FA evalueert hem ook één keer)                                                                                               |
| Expected-type ≠ subject-type `double?`/`double`                                                                                                                                |      3 | should-be                         |                    1                     | subject casten: `((double?)x).ShouldBe(y)`                                                                                                                |
| Dictionary `HaveCount`                                                                                                                                                         |      3 | have-count                        |                    1                     | `dict.Count.ShouldBe(n)` (FA telt paren, dus identiek)                                                                                                    |
| Throw-subject geen Action/Func\<ref\>                                                                                                                                          |      3 | throw, throw-async                |                    1                     | in een lambda wikkelen: `() => { _ = f(); }`                                                                                                              |
| `BeEquivalentTo(…, options)`                                                                                                                                                   |      3 | be-equivalent-to-object           |                    3                     | Manual per optie (queue #32)                                                                                                                              |
| `Task<Result>` vergeleken met `Result`                                                                                                                                         |      2 | not-be                            |                    3                     | testbug: Manual                                                                                                                                           |
| `FluentAssertions.Types` `Be`                                                                                                                                                  |      1 | should-be                         |                    3                     | Manual                                                                                                                                                    |
| Overig (onbekend type, async-ketens)                                                                                                                                           |      4 |                                   |                   1/3                    |                                                                                                                                                           |

Telling: **cat 1 ≈ 178**, **cat 2 ≈ 55**, **cat 3 ≈ 52**.

De niet-herkende 153 in deze vijf projecten:

- cat 1 ≈ 55: `BeSameAs` 37, `Match` 7, `HaveSameCount` 5, `Satisfy` 4, `NotThrowAsync` 2
- cat 2 ≈ 19: `ContainEquivalentOf` 16, `AllBeEquivalentTo` 3
- cat 3 ≈ 79: custom van FluentResults en PowerApps (`BeFailure`, `HaveEntry`, `HaveValueEqual`, `ContainName`, …)

### Plafond

|                            |         Fixed |         % |
| -------------------------- | ------------: | --------: |
| huidig                     | 5 381 / 5 819 | **92,5%** |
| + cat 1                    |         5 614 |     96,5% |
| + cat 2 (met waarschuwing) |         5 688 | **97,7%** |
| cat 3 blijft Manual        |           131 |      2,3% |

Per project verschilt dat sterk. CFE gaat naar ~99,5%. PowerApps blijft rond 75%, want de casus heeft veel AssertionScope en eigen extensies.

## Categorie 3: wat dan wel?

| Geval                                                                                       | Advies                                                                                                                                                                                                                                                                                                                                                            |
| ------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Eigen extensies (`ReferenceTypeAssertions`-subklassen, `HaveSingleMethodBody`, `BeFailure`) | **Manual** met de reden "custom FA-assertion `X` in `<bestand>`: herschrijf de extensie naar Shouldly; de aanroepen kunnen blijven" (queue #34). Geen shim: de body gebruikt `Execute.Assertion`, dat kan Unshackle niet generiek vertalen. Wel goedkoop: het rapport groepeert per extensie. Dan is het één handmatige fix per extensie in plaats van honderden. |
| `ContainInOrder`                                                                            | Kandidaat voor een **shim**: een gegenereerde `ShouldContainInOrder`-extensie (±15 regels, subreeks-check) in het testproject. Dat is semantisch exact, maar het is code die Unshackle in de repo van de gebruiker zet. Advies: eerst Manual met concrete code in de reden, en pas een shim als gebruikers erom vragen.                                           |
| `BeEquivalentTo` met options / cyclisch / ander type / runtime-type                         | **Manual**. Shouldly `ShouldBeEquivalentTo` is hier soepeler of strenger op een andere manier, en een shim zou FA's equivalency-engine nabouwen.                                                                                                                                                                                                                  |
| `WithMessage` met niet-constant patroon                                                     | Manual. Een shim (wildcard → regex tijdens runtime) kan, maar levert weinig op (4×).                                                                                                                                                                                                                                                                              |
| `Equals` op assertions, `Task` vs waarde                                                    | Manual: het is een bug in de test, meld dat.                                                                                                                                                                                                                                                                                                                      |

## Top-10 acties (winst per inspanning)

Winst = calls die van Manual of niet-herkend naar Fixed gaan (B = 5 projecten, A = corpus).

| #   | Actie                                                                                                                                     | winst                                   | inspanning                                   | cat |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------- | -------------------------------------------- | :-: |
| 0   | **Loader**: NuGet-warnings (`NU1701`/`NU190x`) niet als fatale laadfout behandelen                                                        | 5 van 10 corpus-solutions laden nu niet | S                                            |  —  |
| 1   | `BeEquivalentTo` op collectie met value-equality-elementen → `ShouldBe(exp, ignoreOrder: true)`                                           | B 93 (1,6%-punt)                        | S (uitbreiding)                              |  1  |
| 2   | `fa.be-same-as` (+ `NotBeSameAs`)                                                                                                         | A 536, B 37                             | XS                                           |  1  |
| 3   | `fa.not-throw-async`                                                                                                                      | A 150                                   | S (kopie van not-throw)                      |  1  |
| 4   | Vergelijkingen: `BeOnOrAfter`/`Before`/`After`/`Before`, `HaveCountGreaterThan…`, `HaveSameCount`, `HaveElementAt`                        | A 136                                   | XS (mapping in be-greater-less / have-count) |  1  |
| 5   | `fa.which-chain` (#19): resultaat via local                                                                                               | B 37                                    | M                                            |  1  |
| 6   | `fa.because-args` (#22)                                                                                                                   | B 13 (+ in elk patroon)                 | S                                            |  1  |
| 7   | Subject of receiver met side-effects naar `var tmp` hoisten (and-chain, invoking, all-satisfy, "niet in vorm")                            | B 16                                    | S–M                                          |  1  |
| 8   | `fa.match` (#31): `MatchRegex` + constante wildcard-`Match`                                                                               | A 33                                    | S                                            |  1  |
| 9   | `AssertionScope` → `ShouldSatisfyAllConditions`                                                                                           | B 49 (vooral PowerApps)                 | M–L (blok-herschrijving)                     |  2  |
| 10  | Kleine set: `BeOneOf`, `BeSubsetOf`, `OnlyHaveUniqueItems`, `HaveValue`, `HaveFlag`, `BeNullOrEmpty` op collectie, `Contain(IEnumerable)` | A ~60, B ~17                            | XS per stuk                                  | 1/2 |

Acties 1–4 halen samen ~3 procentpunt van de ~5 haalbare, voor weinig werk.

## Na acties 1–4 (TAAK-092 t/m 095)

Gebouwd: `BeEquivalentTo` op collecties van leaves → `ShouldBe(…, ignoreOrder: true)` (uitbreiding `fa.be-equivalent-to-object`),
`fa.be-same-as`, `fa.not-throw-async`, en datum-/count-vergelijkingen in `fa.be-greater-less` en `fa.have-count`.
Scan (`unshackle scan --fix fluentassertions`) met main bcf1156 (vóór) en deze wijziging (na):

| Project                               | Fixed vóór | Manual vóór | Fixed na | Manual na | Verschil                                |
| ------------------------------------- | ---------: | ----------: | -------: | --------: | --------------------------------------- |
| SampleShop                            |         62 |          24 |       62 |        24 | geen: gebruikt geen van deze API's      |
| PowerApps-Tooling (casus, na restore) |         29 |         109 |       33 |       109 | +4 `fa.be-same-as` (waren niet herkend) |

Manual telt hier inclusief `project-file`-findings (SampleShop 2, PowerApps 3). De corpus-winst (A) zit vooral in projecten
die hier niet laden (actie 0).

## Na acties 5–10 (TAAK-096 t/m 101)

Gebouwd: `.Which`/`.Subject` na BeOfType/ContainSingle/Throw (096), becauseArgs centraal als `string.Format(InvariantCulture, …)` (097),
subject met bijwerkingen → `var subject = …;` in and-chain/only-contain/all-satisfy (098), `fa.match` (099),
`AssertionScope` met alleen assertions → `ShouldSatisfyAllConditions` (100), `fa.direct-mapping` + ContainInOrder-Manual met code (101).
Scan met main 0079d44 (vóór, = "na" hierboven) en deze wijziging (na), Manual inclusief `project-file`/`usings`:

| Project                               | Fixed vóór | Manual vóór | Fixed na | Manual na |
| ------------------------------------- | ---------: | ----------: | -------: | --------: |
| SampleShop                            |         62 |          24 |       69 |        16 |
| PowerApps-Tooling (casus, na restore) |         33 |         109 |       55 |        93 |

Niet gedaan (blijft Manual): `ThrowAsync(..).Which`, `.WithMessage(..).Which`, `ContainKey(..).WhoseValue`, Invoking-receiver hoisten,
AssertionScope met context-argument of andere statements, named arguments.

## Zie ook

- `docs/projectinfo/architectuur/corpus.md` — bron A; het percentage daar (90,5%) is verouderd
- `claude/tasks/patroon-queue.md` — #19, #22, #31–#34 en de rijen zonder nummer sluiten aan op de acties hierboven
- `docs/projectinfo/architectuur/proofs.md`
