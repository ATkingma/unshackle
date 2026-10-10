---
name: taak-oppakken
description: Hervatten van een bestaande taak na een accountwissel of nieuwe sessie. Gebruik wanneer de gebruiker zegt 'pak taak X op', 'ga verder met TAAK-003', of 'waar waren we gebleven'.
---

# Taak-Oppakken Skill

Leest een bestaand taakbestand uit `claude/tasks/` en hervatten de taak precies bij de laatste stap.

Project-root: `/mnt/dikkeschijf/documenten/Git/unshackle`

## Stap 1: Vind het taakbestand

Als de gebruiker een nummer of naam noemt:

```bash
ls /mnt/dikkeschijf/documenten/Git/unshackle/claude/tasks/*.md   # actief; voltooid staat in archief/
```

Als onduidelijk welke taak: toon de lijst en vraag welke.

## Stap 2: Lees het taakbestand volledig

```bash
cat /mnt/dikkeschijf/documenten/Git/unshackle/claude/tasks/TAAK-<NNN>-<slug>.md
```

Identificeer:

- De **huidige stap** (gemarkeerd met `[→]`, of de eerste `[ ]` na alle `[x]`)
- De **context** voor de volgende agent
- **Gewijzigde bestanden** tot nu toe

## Stap 3: Verifieer de huidige staat

Lees de relevante bestanden die al gewijzigd zouden moeten zijn:

```bash
git -C /mnt/dikkeschijf/documenten/Git/unshackle log --oneline -5
git -C /mnt/dikkeschijf/documenten/Git/unshackle status --short
```

Controleer of de staat overeenkomt met wat het taakbestand beschrijft. Als er discrepanties zijn: meld dit en vraag bevestiging voordat je verdergaat.

## Stap 4: Markeer de huidige stap als in uitvoering

Vervang `[ ]` van de huidige stap met `[→]` in het taakbestand.  
Update de **Status**-regel: `stap <N>/<totaal> — in uitvoering`.  
Voeg toe onder **Context**: welk account/model nu overneemt en wanneer.

## Stap 5: Voer de stap uit

Ga aan de slag. Na afronding van elke stap:

- Vervang `[→]` met `[x]`
- Zet de volgende stap op `[→]`
- Voeg gewijzigde bestanden toe aan de lijst
- Update **Status**: `stap <N+1>/<totaal> — in uitvoering`

## Stap 6: Bij taak voltooid

Vervang alle `[→]` met `[x]`, update Status naar `✅ voltooid — <datum>`.  
Verplaats het bestand naar `claude/tasks/archief/` als die map bestaat, anders laat staan.

## Stap 7: Bij vroegtijdig stoppen (account-wissel)

Schrijf in de **Context**-sectie:

- Wat er gedaan is in deze sessie
- Wat de volgende stap is en wat daarvoor nodig is
- Eventuele blokkades of openstaande vragen

De volgende agent pakt op via `/taak-oppakken TAAK-<NNN>`.

## Zie ook

- `.claude/skills/taak-start/SKILL.md` — nieuwe taak aanmaken
- `claude/docs/agent-werkwijze.md` — regels voor taakbeheer
