# Agent-werkwijze — regels voor documentatie en taakbeheer

Regels die **alle agents** in dit project volgen voor taakbeheer, cross-referenties en documentsplitsing.

Zie `claude/docs/INDEX.md` voor de volledige documentatie-index.

---

## 1. Taakbeheer

Gebruik het taaksysteem (`claude/tasks/`) voor elke taak die:

- meer dan één sessie kan duren
- mogelijk midden in de taak van account of model wisselt
- meerdere aaneengesloten stappen heeft

| Actie                          | Skill             | Bestand                                  |
| ------------------------------ | ----------------- | ---------------------------------------- |
| Nieuwe taak                    | `/taak-start`     | `claude/tasks/TAAK-<NNN>-<slug>.md`      |
| Taak met analyse en redenering | `/taak-specifiek` | idem                                     |
| Taak hervatten                 | `/taak-oppakken`  | idem; voltooid → `claude/tasks/archief/` |

### Stap-statussen

| Markering | Betekenis                                   |
| --------- | ------------------------------------------- |
| `[ ]`     | Nog te doen                                 |
| `[→]`     | In uitvoering (huidige stap)                |
| `[x]`     | Klaar                                       |
| `[!]`     | Geblokkeerd — reden staat in context-sectie |

### Bij vroegtijdig stoppen

Schrijf altijd in de **Context**-sectie: wat gedaan is, wat nog openstaat, welke bestanden gewijzigd zijn, en eventuele blokkades.

---

## 2. Cross-referenties

Elke keer dat een agent documentatie schrijft of bijwerkt, **moeten** cross-referenties gelegd worden.

- Nieuw document → toevoegen aan `claude/docs/INDEX.md`
- Document beschrijft iets dat een skill gebruikt → `## Zie ook` in die skill
- Verwijzing naar code → exact pad (`src/Unshackle.Core/Fixer.cs`)
- Skill aangemaakt of uitgebreid → verwijzing in `INDEX.md`

### Formaat `## Zie ook` (onderaan elk document of skill)

```markdown
## Zie ook

- `claude/docs/INDEX.md` — volledige documentatie-index
- `docs/projectinfo/architectuur/testen.md` — proofs en types
- `src/Unshackle.Core/Fixer.cs` — implementatie
```

Regels:

- Altijd **relatieve paden** vanaf de project-root
- **Geen decoratieve links**: alleen als het een toekomstige agent tokens scheelt

---

## 3. Documentsplitsing

| Type              | Overweeg splitsen | Hard maximum  |
| ----------------- | ----------------- | ------------- |
| CLAUDE.md (root)  | > 80 regels       | 120 regels    |
| claude/docs/\*.md | > 100 regels      | 150 regels    |
| Skill SKILL.md    | > 80 regels       | 120 regels    |
| Implementatieplan | > 200 regels      | splits altijd |

Hoe splitsen:

1. Nieuw bestand op de meest logische plek (zie `INDEX.md`)
2. Origineel: korte samenvatting + `Zie claude/docs/<onderwerp>.md voor de volledige uitleg.`
3. Nieuw bestand toevoegen aan `INDEX.md`
4. `## Zie ook` in het nieuwe bestand dat terugverwijst

Niet splitsen: korte stukken die altijd samen gelezen worden, `## Zie ook`-secties, taakbestanden (die groeien bewust).

---

## 4. Gedragsregels

### Communicatie

- Nederlands met de gebruiker; code, namen en commitberichten in het Engels
- Geen bullet points bij korte antwoorden of afwijzingen: gebruik proza
- Zegt de gebruiker dat het klaar is of "stop", vraag dan niet of je nog iets kunt doen

### Geheugen en context

- Haal **geen oude taakcontext op** tenzij de gebruiker ernaar verwijst

### Token-efficiëntie

- `grep`/`find` **vóór** `Read` als je één regel of patroon zoekt
- Geen samenvatting van wat je net deed: de gebruiker ziet de diff
- Geen uitgebreide uitleg van net geschreven code tenzij gevraagd

---

## 5. Documentatie aanmaken — checklist

- [ ] Al ergens gedocumenteerd? (check `INDEX.md` eerst)
- [ ] Juiste locatie (root `CLAUDE.md` / `docs/projectinfo/` / `claude/docs/`)?
- [ ] `## Zie ook` toegevoegd?
- [ ] `INDEX.md` bijgewerkt?
- [ ] Skills bijgewerkt die dit onderwerp raken?
- [ ] Binnen de splitsingsdrempel?

## 6. Main blijft groen — harde regel

Er wordt **nooit** naar `main` gepusht of gemerged als er ook maar één test faalt, oud of nieuw.

Vóór elke push of merge naar `main`, lokaal en allemaal groen:

```bash
dotnet build
dotnet test
dotnet test --solution samples/SampleShop/SampleShop.slnx
```

- Faalt er iets: niet pushen. Eerst de oorzaak oplossen, ook als de fout in een test zit die je niet zelf raakte.
- Een test overslaan, verwijderen of afzwakken om groen te worden mag niet zonder akkoord van de gebruiker.
- Dezelfde stappen draaien in CI (`.github/workflows/ci.yml`). GitHub dwingt dit nog niet af (privé-repo zonder
  Pro), dus deze regel is de bewaking.

## Zie ook

- `claude/docs/INDEX.md` — documentatie-index
- `.claude/skills/taak-start/SKILL.md` — taak aanmaken
- `.claude/skills/taak-oppakken/SKILL.md` — taak hervatten
