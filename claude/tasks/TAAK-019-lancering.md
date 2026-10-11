# TAAK-019 — Lancering FluentAssertions → Shouldly (roadmap stap 4)

**Status**: casus gedaan (stap 2+3); lancering wacht op TAAK-021 (casus bouwt nog niet)
**Aangemaakt**: 2026-10-11
**Doel**: Unshackle 0.1 staat publiek op GitHub en NuGet, bewezen op een echt open-source project, met een README die in 30 seconden laat zien wat het doet.

## Context voor de volgende agent

- Roadmap-stand 2026-10-11: stap 1 klaar, stap 2 ~90% (TAAK-017 projectbestanden, TAAK-018 samenwerkingstest), stap 3/5/6 nog niet begonnen. Besluit gebruiker: **eerst lanceren, dan MediatR** (PROJECT.md: "Snelheid telt").
- Tool werkt: `unshackle list|scan|fix` (`--dry-run`, `--force`, `--report`), `dotnet pack` + `dotnet tool install` getest (TAAK-005, `0.1.0-preview` in `src/Unshackle.Interface/Unshackle.Interface.csproj`). README heeft al een sectie Usage (PowerShell + bash).
- Repo `ATkingma/unshackle` is **privé**: branch protection en issues voor buitenstaanders werken pas als hij publiek is.
- `.vibe-wise/` (persoonlijke leernotities) staat in de repo; vóór publiek maken met de gebruiker afstemmen (waarschijnlijk naar `.gitignore` + uit de historie of gewoon weg).

## Analyse

### Huidige situatie

Werkende tool, 15 patronen, docs in `docs/projectinfo/`, CI groen, issue-formulieren (`.github/ISSUE_TEMPLATE/`, `docs/issues.md`). Niet gepubliceerd, niet getest op andermans code.

### Probleem

- Niemand weet dat het bestaat; anderen kunnen intussen losse converters uitbrengen.
- Gedrag op grote, echte codebases (honderden bestanden, multi-target, analyzers) is onbewezen.

### Nieuwe situatie

Publieke repo met branch protection, NuGet-pakket `Unshackle`, README met GIF en een echte-wereld-casus, post op r/dotnet.

### Verbetering

Eerste gebruikers en feedback via issues; bewijs dat de tool op echte code werkt.

## Aanpak

### Wat / Waar / Hoe

1. **Echte casus**: kies een open-source project met veel FluentAssertions 7.x (kandidaten: zie PROJECT.md "Bewijs", bv. RoseMCP; of zoek op GitHub naar `FluentAssertions` 7.x + veel `.Should()`). Clone in een scratchmap, `unshackle scan` → `fix` → `dotnet build` + `dotnet test`. Leg aantallen Fixed/Manual, bouwfouten en testfouten vast in deze taak; elke fout in een pattern = nieuw issue/taak, niet hier oplossen.
2. **Publiek maken**: alleen met akkoord van de gebruiker (`.vibe-wise/` eerst regelen). Daarna branch protection op `main` (PR + groene check "Build and test"), zie `claude/docs/agent-werkwijze.md` §6.
3. **NuGet**: versie `0.1.0`, `PackageReadmeFile`, `RepositoryUrl`, `PackageLicenseExpression` (MIT, zie LICENSE), `PackageTags`. Publiceren via een release-workflow (`.github/workflows/release.yml` op tag `v*`, `NUGET_API_KEY` als secret: de gebruiker zet de secret zelf).
4. **README**: GIF of asciinema van `scan` + `fix --dry-run` op SampleShop, de casus uit stap 1 met cijfers, lijst van ondersteunde patronen (link naar `docs/projectinfo/modellen/patronen.md`), bekende beperkingen (Manual-gevallen).
5. **r/dotnet-post**: concept in `docs/projectinfo/lancering/reddit-post.md`; de gebruiker plaatst hem zelf.

### Waarom deze aanpak

Eerst de echte casus: als die faalt, wil je dat weten vóór de publieke lancering.

## Resultaat echte casus (2026-10-11)

- **Project**: `microsoft/PowerApps-Tooling` @ `630e6973e5c81ddc196283cde26dd696d619d6c7`, `src/PASopa.sln`, FluentAssertions 7.2.0, MSTest, ~403 `.Should()`, TFM's `net48;net8.0;net10.0`.
- Lokaal aangepast voor de baseline: `global.json` SDK 10.0.401 → 10.0.100 (alleen 10.0.112 geïnstalleerd). net48-tests draaien niet op Linux (geen mono), in beide runs.
- **Baseline**: build groen (0 warnings). Test: 1178 totaal, 1172 geslaagd, 6 gefaald (`StressTestApps` x3 apps x2 TFM's in PAModelTests, ook vóór de fix; omgeving).
- **Scan/fix**: tool meldt 778 Fixed / 162 Manual, maar telt elke bevinding per TFM (3x). Uniek: **262 Fixed, 53 Manual** in 25 gewijzigde .cs-bestanden + `Directory.Packages.props` + Testing-csproj. Zie TAAK-022.
- Grootste Manual-redenen: `Contain(IEnumerable)` (6), `Be(...)` met becauseArgs (6), onbekend type/null-literal (5), resultaat van `Throw<T>()` gebruikt (5), `ThrowExactly` (3), `.And/.Which` na NotBeNull/Be (3+3), BeEquivalentTo verschillende typen (3). Project-file: Persistence.Tests en PAModelTests houden FA (er blijven Manual-gevallen).
- **Build na fix**: 8 unieke fouten (x3 TFM = 21+), allemaal in `Persistence.Tests`: CS8602 na fa.have-count (`x.Count` op nullable) en CS8604 na fa.contain op `string?`. Zie TAAK-021.
- **Tests na fix**: PAModelTests 530 totaal, 524 geslaagd, dezelfde 6 `StressTestApps` gefaald → geen regressie. Persistence.Tests (648 in baseline) niet te draaien: bouwt niet.
- Conclusie: nog niet lanceerbaar met deze casus; eerst TAAK-021, dan casus opnieuw.

## Stappen

- [ ] 1. Wachten tot TAAK-017 + TAAK-018 gepusht zijn (checks groen)
- [x] 2. Echte casus: scan/fix/build/test op een open-source project, resultaten hier vastleggen
- [x] 3. Gevonden pattern-fouten als issues/taken vastleggen (niet in deze taak fixen)
- [ ] 4. NuGet-metadata + release-workflow (`v*`-tag), lokaal `dotnet pack` controleren
- [ ] 5. README: GIF, casus-cijfers, ondersteunde patronen, beperkingen
- [ ] 6. Met gebruiker: `.vibe-wise/` regelen, repo publiek, branch protection aan
- [ ] 7. Release `v0.1.0` taggen (gebruiker zet `NUGET_API_KEY`), installatie vanaf nuget.org testen
- [ ] 8. Concept r/dotnet-post; gebruiker plaatst

## Risico's & randgevallen

| Risico                                                        | Mitigatie                                                                             |
| ------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| Echte casus levert veel Manual of bouwfouten                  | Eerlijk in README; fouten worden issues; lancering pas als `fix` de casus laat bouwen |
| Persoonlijke notities publiek                                 | Stap 6 met de gebruiker, vóór publiek maken                                           |
| NuGet-key in repo                                             | Alleen als GitHub-secret, nooit in bestanden                                          |
| Publiek maken / posten / publiceren is extern en onomkeerbaar | Elke stap expliciet laten bevestigen door de gebruiker                                |

## Gewijzigde bestanden

_(wordt aangevuld tijdens uitvoering)_

## Zie ook

- `claude/tasks/TAAK-021-fa-nullable-subject.md` — patroonfout uit de casus
- `claude/tasks/TAAK-022-scan-telt-per-target-framework.md` — telling per TFM

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `docs/projectinfo/project.md` — roadmap, risico's, bewijs van vraag
- `claude/docs/overdracht.md` — stand bij accountwissel
