# Overdracht (bijgewerkt 2026-10-13)

Lees dit eerst als je deze sessie overneemt. Daarna `CLAUDE.md` en `claude/docs/INDEX.md`.

## Stand

- FA → Shouldly en MediatR → Mediator: alle wachtrijtaken t/m TAAK-196 klaar en gepusht (zie `claude/tasks/patroon-queue.md`, `claude/tasks/mediatr-queue.md`).
- Open taken: TAAK-174 (FA, Akka: stap 3, corpusbewijs), TAAK-019 (lancering), TAAK-046 (semantische audit). TAAK-170, -173 en -197 zijn klaar.
- Corpus-herhaling na TAAK-193..196 (kgrzybek, eShop, ardalis) staat nog open.

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
