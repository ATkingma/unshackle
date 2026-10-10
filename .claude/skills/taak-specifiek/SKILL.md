---
name: taak-specifiek
description: Maak een diepgaand taakbestand aan met expliciete redenering: wat wordt aangepast, waar, hoe, waarom, en wat de verbetering is ten opzichte van de huidige situatie. Gebruik wanneer een taak architectureel of risicovol is, of meerdere projecten raakt, en een koude agent direct moet kunnen begrijpen waarom elke keuze gemaakt is.
---

# Taak-Specifiek Skill

Variant van `/taak-start` voor taken die redenering vereisen. Onderzoekt de codebase, vergelijkt oud vs. nieuw, en legt beslissingen beknopt vast zodat elke volgende agent ze niet opnieuw hoeft af te leiden.

Project-root: `/mnt/dikkeschijf/documenten/Git/unshackle`

## Stap 1: Bepaal het volgende taaknummer

```bash
ls /mnt/dikkeschijf/documenten/Git/unshackle/claude/tasks/TAAK-*.md /mnt/dikkeschijf/documenten/Git/unshackle/claude/tasks/archief/TAAK-*.md 2>/dev/null \
  | grep -oP 'TAAK-\K\d+' | sort -n | tail -1
```

Nieuw nummer = laatste + 1 (of 001). Altijd driecijferig.

## Stap 2: Onderzoek — lees alleen wat nodig is

Voer gericht onderzoek uit op de relevante bestanden. Doel: begrijp de **huidige situatie** volledig voordat je het plan schrijft.

Richtlijnen:

- Lees alleen bestanden die direct geraakt worden door de taak
- Gebruik `grep` om patronen te vinden, lees geen volledige directories
- Noteer: wat bestaat al, wat ontbreekt, waar zit de pijn

## Stap 3: Redeneer — bepaal de aanpak

Beantwoord intern (niet in het taakbestand):

- Zijn er meerdere oplossingsrichtingen? Welke is beter en waarom?
- Wat zijn de risico's of afhankelijkheden?
- Raken de wijzigingen andere projecten (Interface/Core/DataAccess) of conventies?

## Stap 4: Schrijf het taakbestand

Bestandsnaam: `claude/tasks/TAAK-<NNN>-<slug>.md`

Schrijf beknopt. Elke sectie max 3-5 regels. Geen open deuren.

```markdown
# TAAK-<NNN> — <Naam>

**Status**: stap 1/<totaal> — nog te starten
**Aangemaakt**: <datum>
**Doel**: <één zin: wat is klaar aan het einde>

## Context voor de volgende agent

<Wat een koude agent moet weten om direct te beginnen — geen uitleg van bekende conventies, alleen het niet-voor-de-hand-liggende:>

- Welke bestanden al bestaan en relevant zijn
- Welke beslissingen al genomen zijn
- Blokkades of afhankelijkheden

## Analyse

### Huidige situatie

<Wat is er nu — concreet, met bestandspad:regel als relevant>

### Probleem

<Waarom dit niet werkt of suboptimaal is — één zin per punt>

### Nieuwe situatie

<Wat er na de taak anders is — concreet>

### Verbetering

<Meetbare of waarneembare verbetering: sneller / veiliger / minder foutgevoelig / consistenter>

## Aanpak

### Wat

<Exacte wijziging: welk bestand, welke functie, welk patroon>

### Waar

<Bestandspaden. Geen wildcards — exacte locaties op basis van onderzoek>

### Hoe

<Implementatiestrategie in 2-4 stappen. Verwijs naar bestaande patronen in het project>

### Waarom deze aanpak

<Waarom niet de alternatieven — kort, alleen als niet voor de hand liggend>

## Stappen

- [ ] 1. <eerste stap — uitvoerbaar en afgebakend>
- [ ] 2. <tweede stap>
- [ ] 3. <...>

## Risico's & randgevallen

| Risico   | Mitigatie         |
| -------- | ----------------- |
| <risico> | <hoe ondervangen> |

## Gewijzigde bestanden

_(wordt aangevuld tijdens uitvoering)_

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
```

## Stap 5: Meld aan de gebruiker

```
Taak aangemaakt: claude/tasks/TAAK-<NNN>-<slug>.md
Onderzocht: <N bestanden>
Kernbeslissing: <één zin over de gekozen aanpak>
Om op te pakken: /taak-oppakken TAAK-<NNN>
```

## Zie ook

- `.claude/skills/taak-start/SKILL.md` — lichtere variant zonder redenering
- `.claude/skills/taak-oppakken/SKILL.md` — taak hervatten na sessiewissel
- `claude/docs/agent-werkwijze.md` — taakbeheer-regels
