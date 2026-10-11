# Overdracht (accountwissel 2026-10-11)

Lees dit eerst als je deze sessie overneemt. Daarna `CLAUDE.md` en `claude/docs/INDEX.md`.

## Stand

- `main` = commit `f9b1a27` (alles gepusht, CI groen tot en met `d5b8778`). Tool werkt: `unshackle list|scan|fix`.
- Patroon-wachtrij (`claude/tasks/patroon-queue.md`): alle 15 FA → Shouldly-patronen **klaar**. De patroon-loop (cron) is gestopt.
- Roadmap: stap 1 klaar, stap 2 ~90%, stap 3 (MediatR) / 5 (AutoMapper) / 6 nog niet begonnen. Besluit: **eerst lanceren** (TAAK-019), dan MediatR.

## Lopend werk (achtergrond-agents, niet gecommit)

| Taak     | Wat                                                                                                                                                              | Als de agent niet meer draait                                                                                                                                                    |
| -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| TAAK-017 | `fix` past ook projectbestanden aan (FA-pakket/`<Using>` weg, Shouldly 4.3.0 erbij, CPM), met unittests + integratietest die de gefixte SampleShop bouwt en test | `/taak-oppakken TAAK-017`; staat de taak nog niet in `claude/tasks/`, opnieuw starten met dezelfde opdracht (zie verslag van de vorige sessie of maak hem met `/taak-specifiek`) |
| TAAK-018 | ✅ Klaar (in `archief/`): `CombinedProofTests` + `Proofs/FluentAssertions/_Combined/` (6 cases), 245 tests groen. Nog niet gepusht. | — |

## Openstaande opdracht van de gebruiker

> "Als je deze 2 dingen gefixt hebt, push alles wat we tot nu toe hebben."

Dus: zodra TAAK-017 en TAAK-018 klaar zijn → taken naar `archief/` → `git add -A` → commit → **checks op de commit in een
losse worktree** (`git worktree add --detach <scratchpad>/verify HEAD`; `dotnet build`, `dotnet test`,
`dotnet test --solution samples/SampleShop/SampleShop.slnx`, `echo '{}' | bash .claude/hooks/check-pattern-docs.sh subagent-stop`)
→ alles groen → `git push origin main`. Iets rood → niet pushen, melden (`claude/docs/agent-werkwijze.md` §6).
Daarna: TAAK-019 (lancering) voorstellen.

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
