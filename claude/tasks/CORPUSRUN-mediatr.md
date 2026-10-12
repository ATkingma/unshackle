# Corpus-run MediatR → Mediator (2026-10-11, main 40cf91e)

Echte open-source MediatR-projecten door `unshackle fix --fix mediatr --force` gehaald, met baseline- en
na-fix build/test. Doel: meten hoeveel echte solutions de alles-of-niets-migratie (`MediatRFix.AllOrNothing`) volledig haalt.

## Opzet

- Corpus: `scratchpad/corpus-mediatr/`, kopieën in `scratchpad/run-mediatr/work/<naam>`, rapporten `run-mediatr/<naam>.json`,
  logs `run-mediatr/logs/<naam>[-trim|-sim]/`, scripts `scratchpad/scripts/run*.sh`.
- Omgeving: alleen .NET SDK 10.0.112 (runtimes 8/9/10). Daarom per kopie: `global.json` → `10.0.100/latestFeature`
  (andere sleutels behouden), en bij restore/build `-p:NuGetAudit=false` (NU1902 als error met SDK 10),
  `-p:AllowMissingPrunePackageData=true` (NETSDK1226, distro-SDK), `-p:SkipNSwag=True` (jason: NSwag-postbuild start de app).
  `DOCKER_HOST` naar een dode poort: geen Docker/Testcontainers/DB.
- **-trim**: bijprojecten die op Linux niet laden uit de kopie-solution gehaald (kgrzybek `_build`, `Database.*`;
  eShop MAUI `ClientApp`, `HybridApp`, `ClientApp.UnitTests`; eShopOnWeb `docker-compose.dcproj`).
- **-sim**: projectbestanden semantisch gelijk genormaliseerd (`scripts/norm.py`: `/>` → ` />`, `" >` → `">`,
  `->` → `-&gt;` in attributen, meerregelige attributen op één regel) om te meten wat er achter blokkade TAAK-134 zit.
  Dit is een simulatie, niet het gedrag van Unshackle zelf.
- nopCommerce staat in het corpus maar gebruikt geen MediatR: overgeslagen. evolutionary-architecture: hoofdstuk 1 en 2 (eigen solutions).

## 1. Resultaat per project (echte run)

| Project                           | Laadt?                                             | Alles-of-niets geblokkeerd? (door)                                                           | Fixed | Manual | Bouwt na fix?                                                           | Tests baseline → na fix                                        | MediatR weg?                      |
| --------------------------------- | -------------------------------------------------- | -------------------------------------------------------------------------------------------- | ----- | ------ | ----------------------------------------------------------------------- | -------------------------------------------------------------- | --------------------------------- |
| jasontaylor CleanArchitecture 8.0 | ja                                                 | ja: project-file (`Web.csproj`, `->` in attribuut)                                           | 0     | 27     | ja (ongewijzigd)                                                        | 16/41 → 16/41 (25 rood: SQL Server/Playwright)                 | nee                               |
| ardalis CleanArchitecture 9.1     | ja                                                 | **nee**                                                                                      | 15    | 0      | **nee**: CS0311 (`DomainEventBase` uit Ardalis.SharedKernel is MediatR) | 9/9 → n.v.t. (build faalt)                                     | nee (transitief via SharedKernel) |
| amantinband clean-architecture    | ja                                                 | ja: project-file (`Directory.Build.props` `" >`)                                             | 0     | 26     | ja (ongewijzigd)                                                        | 85/85 → 85/85                                                  | nee                               |
| nadirbad VerticalSlice            | ja                                                 | ja: project-file (`" >`), request-processor (`return Task.Run(...)`), generieke notification | 0     | 16     | baseline al rood (CA1873, SDK 10-analyzers + TWAE)                      | –                                                              | nee                               |
| evolutionary hfst. 1              | ja                                                 | ja: project-file (meerregelige attributen)                                                   | 0     | 6      | ja (ongewijzigd)                                                        | 45/45 → 45/45                                                  | nee                               |
| evolutionary hfst. 2              | ja                                                 | ja: project-file (`/>` zonder spatie)                                                        | 0     | 21     | ja (ongewijzigd)                                                        | 39/39 → 39/39                                                  | nee                               |
| kgrzybek modular-monolith         | nee → ja na trim                                   | ja: project-file (`"/>`), 12× generic message (10 vals, TAAK-132)                            | 0     | 90     | ja (ongewijzigd)                                                        | ~280 unit/arch groen, 40 integratie rood (SQL Server) → gelijk | nee                               |
| eShopOnWeb                        | **nee** (Razor-componenten, `.dcproj`)             | –                                                                                            | –     | –      | –                                                                       | 74/74 → 74/74 (niets gewijzigd)                                | nee                               |
| dotnet/eShop                      | **nee** (MAUI; na trim: Razor `WebApp.Components`) | –                                                                                            | –     | –      | baseline rood (ASPIRE009, geen Aspire CLI)                              | 64/64 (MTP)                                                    | nee                               |

**Volledig gemigreerd: 0 van 9.** Eén project (ardalis) is door alles-of-niets heen gekomen en bouwt dan niet.

### Simulatie: wat zit er achter de project-file-blokkade (-sim)

| Project  | Fixed/Manual | Bouwt? | Wat dan blokkeert of breekt                                                         |
| -------- | ------------ | ------ | ----------------------------------------------------------------------------------- |
| evo1     | 6 / 0        | nee    | marker-handlers gemist (TAAK-130, CS0738); MSG0005 (TAAK-136)                       |
| evo2     | 22 / 0       | nee    | `using Mediator` overschaduwd (TAAK-131, CS0246); handlers gemist (TAAK-130)        |
| amantin  | 0 / 26       | –      | nog een csproj met gemengde CRLF/LF (TAAK-134)                                      |
| jason    | 0 / 29       | –      | `#if`-templatecode in `Web` houdt `global using MediatR` vast ("inactive #if code") |
| nadirbad | 0 / 18       | –      | request-processor `Task.Run`, generieke `DomainEventNotification<T>`                |
| kgrzybek | 0 / 96       | –      | generic messages (2 echt: generieke decorator, `DomainNotificationBase<T>`)         |

## 2. Wat blokkeert alles-of-niets het vaakst (aantal projecten)

| #   | Oorzaak (Manual of laadfout)                                                                                                                                                          | Projecten                                                         | Taak     |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- | -------- |
| 1   | project-file: "cannot be edited without changing unrelated text" (`/>`, `" >`, `->`, meerregelige attributen, gemengde regeleinden; vaak in een ongewijzigde `Directory.Build.props`) | 6 (jason, amantin, nadirbad, evo1, evo2, kgrzybek)                | TAAK-134 |
| 2   | solution laadt niet: Razor-gegenereerde typen, `.dcproj`, niet-ladend bijproject zonder MediatR                                                                                       | 3 (eShopOnWeb, eShop, kgrzybek)                                   | TAAK-135 |
| 3   | `mediatr.unsupported` generic message/handler                                                                                                                                         | 2 (kgrzybek, nadirbad); bij kgrzybek 10/12 vals (abstracte basis) | TAAK-132 |
| 4   | `usings`: inactieve `#if`-code houdt `global using MediatR`                                                                                                                           | 1 (jason, `dotnet new`-template)                                  | –        |
| 5   | `mediatr.request-processor`: `Process` geeft geen `Task.CompletedTask` (`return Task.Run(...)`)                                                                                       | 1 (nadirbad)                                                      | actie 7  |
| 6   | extern pakket bouwt op MediatR (Ardalis.SharedKernel) — **niet gedetecteerd**, hoort Manual te zijn                                                                                   | 1 (ardalis)                                                       | TAAK-133 |

Andere categorieën kwamen niet voor. Conclusie: de waarde van de MediatR-fix wordt nu bepaald door
**infrastructuur** (projectbestanden, laden), niet door ontbrekende MediatR-patronen.

## 3. Bouw-/runtimefouten na fix

| Project    | Fout                                                              | Oorzaak                                                                                                   |
| ---------- | ----------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| ardalis    | CS0311 `ContributorDeletedEvent` is geen `Mediator.INotification` | basisklasse uit Ardalis.SharedKernel (hangt af van MediatR 12.1.1); fix ziet dat niet → TAAK-133          |
| evo1 (sim) | CS0738 handler implementeert `Handle` niet                        | marker-interface in eerder verwerkt bestand al geswapt; handler-bestand ziet geen MediatR meer → TAAK-130 |
| evo1 (sim) | MSG0005 message without handler (als error)                       | Mediator-generator + TreatWarningsAsErrors → TAAK-136                                                     |
| evo2 (sim) | CS0246 `INotification`, `IPublisher`; CS1961 (volgfout)           | `using Mediator;` binnen namespace bindt aan eigen `...Infrastructure.Mediator` → TAAK-131                |

Runtime: geen enkele solution kwam zo ver dat runtime-gedrag meetbaar was. ardalis test1 draaide op oude binaries (build faalde) en telt niet.
Omgevingsfouten (geen Unshackle): NU1902, NETSDK1226, NSwag, CA1873 (nadirbad), ASPIRE009 (eShop), MAUI-workloads, SQL Server/Docker-integratietests.

## 4. Bugtaken

- TAAK-130 cross-file marker-handler gemist door bestandsvolgorde (stil: geen Manual) — unittest, geen proof mogelijk
- TAAK-131 `using Mediator` overschaduwd — proof `Proofs/_Pending/MediatR/Usings/edge-namespace-shadow.*`
- TAAK-132 abstracte generieke basis onterecht Manual — proof `Proofs/MediatR/RequestHandler/edge-abstract-generic-base.*` (klaar) (gedrag van Mediator 3.0.2 gecontroleerd met een probe-project)
- TAAK-133 extern pakket op MediatR niet gedetecteerd — unittest met tweede assembly
- TAAK-134 projectbestand-roundtrip te streng + onnodig laden `Directory.Build.props` — DataAccess-tests
- TAAK-135 loader: Razor, `.dcproj`, niet-ladende bijprojecten — klaar: eShopOnWeb bouwt na de fix, eShop/kgrzybek laden
- TAAK-136 MSG0005 met TreatWarningsAsErrors

## 5. Top-10 acties (winst per inspanning)

| #   | Actie                                                                             | Winst                                             | Inspanning   |
| --- | --------------------------------------------------------------------------------- | ------------------------------------------------- | ------------ |
| 1   | TAAK-134 deel 1: `Directory.Build.props`/bestanden alleen laden als ze veranderen | ontgrendelt evo1, evo2, nadirbad, amantin (props) | klein        |
| 2   | TAAK-134 deel 2: splice-schrijven i.p.v. XDocument-roundtrip                      | haalt de #1-blokkade (6/9) volledig weg           | middel       |
| 3   | TAAK-130: findings op originele semantiek                                         | evo1/evo2 bouwen; voorkomt stille kapotte builds  | middel       |
| 4   | TAAK-133: extern MediatR-type → Manual                                            | herstelt de alles-of-niets-belofte (ardalis)      | klein        |
| 5   | TAAK-131: `global::Mediator` bij schaduw                                          | evo2 bouwt                                        | klein        |
| 6   | TAAK-132: `IsAbstract` uitzonderen                                                | 10 valse Manuals weg (kgrzybek)                   | zeer klein   |
| 7   | request-processor: `return <Task-expr>` → `return new ValueTask(<expr>)`          | nadirbad                                          | klein        |
| 8   | TAAK-136: MSG0005 NoWarn                                                          | evo1/evo2 bouwen met TWAE                         | klein        |
| 9   | TAAK-135: loader-tolerantie (Razor, `.dcproj`, bijprojecten)                      | eShopOnWeb, eShop, kgrzybek laden                 | middel       |
| 10  | `#if`-code: inactieve takken zonder MediatR-identifiers niet als gebruik tellen   | jason                                             | klein–middel |

## Oordeel

Op dit moment migreert Unshackle **0 van de 9** echte solutions volledig. Zes worden tegengehouden door de
projectbestand-check en drie laden niet. Het enige project dat alles-of-niets passeerde (ardalis) bouwt daarna
niet. De MediatR-patronen zelf zijn niet de bottleneck: in de simulatie zonder projectbestand-blokkade
herschreef de fix evo1 en evo2 volledig zonder Manual, en de builds braken op drie concrete bugs (TAAK-130/131/136).
Verwachting na acties 1–8: evo1, evo2 en vermoedelijk amantin migreren volledig (3/9). ardalis wordt dan een
eerlijke Manual. nadirbad en kgrzybek blijven terecht Manual (generieke messages/decorators). jason, eShopOnWeb en
eShop hangen af van acties 9–10. Dat moet een nieuwe corpus-run bevestigen, met build **en** tests.

## Run 2026-10-12 (main 2800f23)

Herhaalde run na de MediatR-wachtrij (TAAK-130..136 klaar). Werkmap `D:\playground\corpus-mediatr` (Windows, SDK 10.0.400),
harnas `claude/tools/corpus-regression.sh` (kopie met `py -3`; restore → build → test ×2 → `fix --fix mediatr --force` →
bin/obj weg → restore → build → test → per-test-vergelijking). Elke kloon kreeg een `nuget.config` met alleen nuget.org;
flags zoals eerder (global.json → 10.0.100/latestFeature, `NuGetAudit=false`, `AllowMissingPrunePackageData`, `SkipNSwag`).
Logs, rapporten en vergelijkingen in `D:\playground\corpus-mediatr\_results\<naam>\`.

Commits (MediatR ≤ 12): jason tag v8.0.0 `f2f0445` (12.1.1; HEAD heeft 14.2), ardalis tag v9.1.0 `ccb82ba` (12.2.0; HEAD
gebruikt al Mediator 3), amantin `5822811` (12.2.0), nadirbad `feb3a15` (12.4.1), evolutionary `441a7cd` (12.5.0),
kgrzybek `91c8ef2` (12.2.0), eShopOnWeb `4da8212` (12.0.1), dotnet/eShop `59f1a6c` (2025-03-10, 12.4.1; HEAD heeft 13.1).
Omgevingsingrepen: nadirbad `.editorconfig` + `CA1873 = none` (SDK 10-analyzer, TWAE); kgrzybek `_build` + `Database/*`
uit de solution (sqlproj op SDK 10); eShop `ClientApp`, `HybridApp`, `ClientApp.UnitTests` uit de solution (MAUI).

| Project        | Restore | Build vóór | Test vóór          | Fix   | Fixed | Manual (root)                          | Gemigreerd | Build na   | Test na  | Regressies |
| -------------- | ------- | ---------- | ------------------ | ----- | ----: | -------------------------------------- | ---------- | ---------- | -------- | ---------- |
| jason          | ok      | ok         | 39/41 (2 rood)     | ok    |     0 | 29 (usings `#if` + project-file)       | nee        | ok (ongew.) | 39/41   | 0          |
| ardalis        | ok      | ok         | 9/9                | ok    |     0 | 28 (10× SharedKernel, 1× add-mediatr)  | nee (terecht) | ok (ongew.) | 9/9  | 0          |
| amantin        | ok      | ok         | 85/85              | ok    |     0 | 28 (1× "still used", testproject)      | nee        | ok (ongew.) | 85/85   | 0          |
| nadirbad       | ok      | ok¹        | 30/30              | ok    |     0 | 18 (request-processor, generic notif.) | nee        | ok (ongew.) | 30/30   | 0          |
| evo hfst. 1    | ok      | ok         | 45/45              | ok    |     0 | 9 (1× add-mediatr testproject)         | nee        | ok (ongew.) | 45/45²  | 0          |
| evo hfst. 2    | ok      | ok         | 26/52 (26 DB)      | ok    |     0 | 27 (2× add-mediatr)                    | nee        | ok (ongew.) | 26/52   | 0          |
| kgrzybek       | ok³     | ok³        | 246/287 (40 DB)    | ok    |     0 | 242 (project-file + 32 generic)        | nee        | ok (ongew.) | 246/287 | 0          |
| **eShopOnWeb** | ok      | ok         | 74/74              | ok    | **9** | 0                                      | **ja**     | **ok**     | **74/74** | **0**    |
| dotnet/eShop   | ok³     | **faalt** (LIB002 libman/unpkg) | – | dry-run | 0 | 34 (1× generic `IdentifiedCommand<T,R>`) | nee (terecht) | – | – | – |

¹ na CA1873-onderdrukking. ² 7 data-gedreven testnamen met random waarden wisselen per run (niet echt verdwenen).
³ na trim. "ongew." = alles-of-niets liet de solution ongemoeid; de build/test na fix is dus de baseline-code.
Alles-of-niets hield in alle 8 andere projecten stand: geen enkele half gemigreerde solution, 0 regressies.

### Root-Manuals gegroepeerd

| Oorzaak                                                                       | Projecten          | Oordeel        | Taak     |
| ----------------------------------------------------------------------------- | ------------------ | -------------- | -------- |
| testcode gebruikt `RequestHandlerDelegate` (mock), geen finding, alleen "still used" op csproj | amantin | gat (stil) | TAAK-180 |
| zelfsluitend `<Project Sdk="..." />` → "cannot be edited without changing unrelated text" | kgrzybek | bug | TAAK-181 |
| inactieve `#if`-code (zonder MediatR) houdt `global using MediatR`            | jason              | gat            | TAAK-182 |
| `AddMediatR` over projectgrenzen (internal handler, assembly-param, testproject) | ardalis, evo1, evo2 | onderzoeken | TAAK-183 |
| request-processor `return Task.Run(...)`                                       | nadirbad           | gat            | TAAK-184 |
| pakket op MediatR (Ardalis.SharedKernel)                                      | ardalis            | terecht Manual | –        |
| generieke messages/decorators (`DomainEventNotification<T>`, `*CommandHandlerDecorator<T>`, `IdentifiedCommand<T,R>`) | nadirbad, kgrzybek, eShop | terecht Manual | – |

Build-fouten na fix: geen (eShopOnWeb, het enige herschreven project, bouwt). TAAK-134/135 werken: de vorige
blokkades (projectbestand-opmaak in 6 projecten, laden van eShopOnWeb/eShop/kgrzybek) zijn weg.

### Conclusie

**1 van 9 volledig gemigreerd en groen** (eShopOnWeb: 9 Fixed, 0 Manual, MediatR weg, 74/74). Vorige run: 0 van 9.
Vier projecten houden (ook) terechte Manuals (ardalis, nadirbad, kgrzybek, eShop). Na TAAK-180 en TAAK-182
zijn amantin en jason naar verwachting volledig; na TAAK-183 mogelijk evo1/evo2. "100% MediatR" is daarmee nog niet
bewezen: hooguit 5 van 9 is haalbaar, de rest gebruikt constructies die Mediator niet ondersteunt.

## Zie ook

- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` — frequenties uit dit corpus
- `claude/tasks/mediatr-queue.md` — patroon-wachtrij
- `docs/projectinfo/architectuur/core.md` — alles-of-niets
- TAAK-130 t/m TAAK-136 in `claude/tasks/`
