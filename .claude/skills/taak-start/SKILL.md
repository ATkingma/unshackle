---
name: taak-start
description: Maak een nieuw taakbestand aan in claude/tasks/ voor een project of feature. Gebruik wanneer de gebruiker zegt "maak een taak", "maak een taak aan voor dit project", "beschrijf dit in een md", "leg dit vast", of een lijst van te doen dingen geeft die bijgehouden moeten worden. Maakt een genummerd TAAK-NNN bestand zodat elke agent de taak kan oppakken en doorzetten.
---

# Taak-Start Skill

Maakt een genummerd taakbestand aan in `claude/tasks/` zodat elke agent — ook na een accountwissel — precies weet waar de taak staat en hoe verder te gaan.

Project-root: `/mnt/dikkeschijf/documenten/Git/unshackle`

## Stap 1: Bepaal het volgende taaknummer

```bash
ls /mnt/dikkeschijf/documenten/Git/unshackle/claude/tasks/TAAK-*.md /mnt/dikkeschijf/documenten/Git/unshackle/claude/tasks/archief/TAAK-*.md 2>/dev/null \
  | grep -oP 'TAAK-\K\d+' | sort -n | tail -1
```

Nieuw nummer = laatste + 1 (of 001 als er nog geen zijn). Gebruik altijd driecijferig: 001, 002, …

## Stap 2: Vraag de gebruiker (of gebruik opgegeven context)

Wat je nodig hebt:

- **Naam/omschrijving** van de taak (kort, slug-vriendelijk)
- **Doel**: wat moet er aan het einde klaar zijn?
- **Stappen**: werk ze uit op basis van de taakbeschrijving — nummereer ze, wees concreet

Als de gebruiker al een beschrijving heeft gegeven: gebruik die direct, vraag niet opnieuw.

## Stap 3: Schrijf het taakbestand

Bestandsnaam: `claude/tasks/TAAK-<NNN>-<slug>.md`

```markdown
# TAAK-<NNN> — <Naam>

**Status**: stap 1/<totaal> — nog te starten  
**Aangemaakt**: <datum>  
**Doel**: <wat moet er klaar zijn aan het einde>

## Context voor de volgende agent

<Beschrijf hier de achtergrond die een koude agent nodig heeft:>
- Welke bestanden al bestaan / al gewijzigd zijn
- Welke beslissingen al genomen zijn en waarom
- Openstaande vragen of blokkades
- Relevante docs: zie `claude/docs/INDEX.md`

## Stappen

- [ ] 1. <eerste stap — concreet en uitvoerbaar>
- [ ] 2. <tweede stap>
- [ ] 3. <...>

## Wijzigingen

| Veld       | Inhoud                                                                 |
| ---------- | ---------------------------------------------------------------------- |
| **Waar**   | _(welke bestanden / projecten worden geraakt)_                          |
| **Hoe**    | _(wat voor soort wijziging: nieuw endpoint, refactor, migratiefix, …)_ |
| **Waarom** | _(waarom is dit nodig — bug, feature-request, technische schuld, …)_   |

_(tijdens uitvoering mogen extra rijen worden toegevoegd voor aanvullende wijzigingen)_

## Zie ook

- `claude/docs/INDEX.md` — documentatie-index
- `docs/projectinfo/README.md` — architectuuroverzicht
```

## Stap 4: Meld aan de gebruiker

Druk af: taaknummer, bestandspad, aantal stappen, en hoe de taak opgepakt kan worden:

```
Taak aangemaakt: claude/tasks/TAAK-<NNN>-<slug>.md
Om op te pakken in een nieuwe sessie: /taak-oppakken TAAK-<NNN>
```

## Zie ook

- `.claude/skills/taak-oppakken/SKILL.md` — taak hervatten na wissel
- `claude/docs/agent-werkwijze.md` — regels voor taakbeheer, cross-references, documentsplitsing
