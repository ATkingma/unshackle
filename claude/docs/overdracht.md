# Overdracht (bijgewerkt 2026-10-13)

Lees dit eerst als je deze sessie overneemt. Daarna `CLAUDE.md` en `claude/docs/INDEX.md`.

## Stand (2026-10-13, 16:10)

**Gepusht t/m `04f113b`.** Daarna staat in de werkmap **ongecommit** werk dat klaar en lokaal groen is (1093/1093, SampleShop 38/38):
TAAK-220, -221, -222 (FA-regressies Akka/Steeltoe/LiteDB), TAAK-223 (multi-TFM in `Fixer.cs`), TAAK-240 (MediatR-archtest-waarschuwing),
TAAK-224 (`TimeSpanExtensionsPattern`, nieuw) en TAAK-225 (`EqualSequencePattern` op dictionary). Taakbestanden staan al in `archief/`.

**Eerste actie voor de volgende agent:**
1. Controleer of de corpus-herhaling van TAAK-224/225 klaar is: `<corpus>/corpus-fa/_results/r5-224/` (akka, steeltoe). Ontbreekt de sectie in
   `claude/tasks/CORPUSRUN-fa.md`, vergelijk dan zelf `compare.json` met ronde 5 (`_results/r5/`): verwacht minder Manual, 0 nieuwe regressies.
   De agent die dit draaide is bij de sessiewissel gestopt; als `r5-224/akka` onvolledig is, draai `tools/corpus-regression-r4.sh` opnieuw voor Akka en Steeltoe.
2. `dotnet build` + `dotnet test` in de werkmap, daarna de push-procedure hieronder (worktree-check met LF), commit
   ("Fix FA regressions (aggregate/cancelled/assertion helpers), multi-target files, TimeSpan extensions, dictionary Equal, MediatR archtest warning"), push.
3. FA-corpusronde 5 op alle 16 solutions (`tools/corpus-regression-r4.sh`, tool publiceren uit een schone worktree van HEAD). Doel: 16/16 bouwt,
   0 regressies behalve Akka DData `ReplicatorMessageSerializerSpec` (TAAK-200, asymmetrische `Equals` in Akka; staat als waarschuwing in het rapport).
   Groen → FA = 100%, roadmap bijwerken in `docs/projectinfo/project.md`.
4. Daarna stap 5: AutoMapper (`claude/tasks/automapper-queue.md`, ~40% klaar). Besluit gebruiker: eerst verder uitwerken (rij 7 splitsen), dán een grote corpusrun. Prioriteit FA → MediatR → AutoMapper, elk eerst 100%.

**Roadmap:** 1 Scan 100% · 2 FA ~97% · 3 MediatR 100% (7/9; evo1/evo2 terecht Manual; kgrzybek-archtests: waarschuwing TAAK-240) · 4 Lancering ~80% (wacht op gebruiker) · 5 AutoMapper ~40% · 6 SharpGrep 0%.

**Corpusstand:** FA ronde 4: 16/16 bouwt, 13/16 groen; na TAAK-220..223 (ronde 5, deelrun) ook LiteDB en Steeltoe 0 regressies, Akka 1 (bekend).
MediatR ronde 6: jason, ardalis, amantin, nadirbad, eShopOnWeb, dotnet/eShop, kgrzybek gemigreerd.

**Gebruikersbesluiten vandaag:** TAAK-200 → "Fixed met waarschuwing" bij mogelijk asymmetrische `Equals`. Door agents zelf gekozen (bij twijfel voorleggen):
TAAK-220/221/222 → Manual (AggregateException-uitpak, geannuleerde task, meta-test op eigen helper); TAAK-214 → archtest-verschil legitiem.

**Werkwijze die werkte:** parallelle agents met strikt gescheiden bestanden (MediatR-code / FA-patronen / `Fixer.cs`+`UsingDirectives.cs`),
taaknummers per agent vooraf reserveren, `TEMP`/`TMP` op `D:`-map, nooit andermans dotnet-processen killen, één commit+push na alle agents.
## Lokale omgeving (Windows)

- Deze machine blokkeert het starten van `.exe` uit `%TEMP%` ("Access is denied"). De twee `SampleShopTests`-integratietests die een gefixte kopie bouwen en testen falen daardoor lokaal.
  Draai ze met `TEMP`/`TMP` op een map buiten `%TEMP%` (bijv. `TEMP=<map op D:> TMP=<map op D:> dotnet test`); in CI (Linux) speelt dit niet.
- `jq` ontbreekt in Git Bash: `.claude/hooks/check-pattern-docs.sh` faalt; doe de check met de hand (zie het script).

## Push-procedure

Taken naar `archief/` → `git add -A` → commit → checks op de commit in een losse worktree, uitgecheckt met LF zoals CI
(`git -c core.autocrlf=false worktree add --detach <map buiten %TEMP%> HEAD`; met de globale `autocrlf=true` falen 15 tests op CRLF) (`dotnet build`, `dotnet test`,
`dotnet test --solution samples/SampleShop/SampleShop.slnx`, docs-check) → alles groen → `git push origin main`. Iets rood → niet pushen, melden
(`claude/docs/agent-werkwijze.md` §6). Vóór elke commit grep op lokale paden (playground-map, gebruikersmap, `AppData`); schrijf `<probe-dir>/…` of `<corpus>/…`.

## Werkafspraken met de gebruiker (niet vergeten)

- Nederlands praten; code, namen en commits in het Engels. **Geen Claude-vermelding** in commits of PR's.
- **Nooit pushen naar `main` met een falende test.** Pushen na elk afgerond stuk werk is een staande opdracht, altijd na de worktree-check.
- Elk patroon: taak (`/taak-specifiek`) → KingmaDoc-plan → bouwen + proofs → `<slug>-modellen.md` (klasse, sequence, beslisboom) → verify → algemene modellen bijwerken → push. Zie `claude/docs/patroon-loop.md`.
- Bij een loop: na een afgerond patroon **meteen** het volgende starten, niet op de cron wachten.
- Autonoom werken met agents is gewenst; geef na elke agent een kort verslag. Ingrijpende externe acties (repo publiek, NuGet publiceren, posten) altijd eerst vragen.
- Code-standaarden: `claude/docs/code-standaarden.md` (o.a. nooit `sealed`, klassieke constructors, `CancellationToken` verplicht). Design patterns: GoF/refactoring.guru.
- Visuele modellen (C4, UML) worden gewaardeerd: zie `docs/projectinfo/`.

## Bekende open punten

- Beslisbomen van enkele patronen hebben >12 pijlen (niet gesplitst).
- `.vibe-wise/` (leernotities) staat in de repo; vóór publiek maken afstemmen.
- Repo is privé: GitHub dwingt branch protection niet af (geen Pro).

## Zie ook

- `claude/tasks/TAAK-019-lancering.md` — volgende stap
- `claude/docs/patroon-loop.md` — werkwijze patronen
- `claude/docs/agent-werkwijze.md` — taakbeheer, §6 main blijft groen
