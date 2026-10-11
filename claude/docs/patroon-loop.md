# Patroon-loop

Elke 15 minuten start de hoofdsessie een achtergrond-agent die één patroon uit
`claude/tasks/patroon-queue.md` oppakt: eerst onderzoek in een taak (`/taak-specifiek`-formaat), dan realisatie.
Agents draaien parallel. De voortgang en keuzes staan in de taakbestanden; de wachtrij zegt welke taak bij welk patroon hoort.

## Tick (hoofdsessie, elke 15 min)

1. Lees `claude/tasks/patroon-queue.md`.
2. Alle rijen `klaar`? → loop stoppen (`CronDelete`), gebruiker melden. Klaar.
3. Kies de eerste rij met `open`, of `geblokkeerd` waarvan de blokkerende taak klaar is, of `mislukt` met poging < 3.
   Geen kandidaat (alles `bezig`/geblokkeerd)? → niets starten.
4. Zet de rij op `bezig (wordt geclaimd)` zodat de volgende tick hem overslaat.
5. Start één achtergrond-agent met de **Agent-brief** hieronder plus het rijnummer.
   Model: rij 0 (fundament) → `opus`; patronen → `sonnet`; `mislukt` poging 2 → `opus`, poging 3 → `fable`.
6. Als een agent klaar is met een patroon (rij `klaar`): start eerst één achtergrond-agent (`sonnet`) met de
   **Modellen-brief** hieronder en wacht op zijn verslag. Daarna controleer zelf `dotnet build`, `dotnet test` en
   `dotnet test --solution samples/SampleShop/SampleShop.slnx`. Alles groen → `git add -A` (zonder bestanden van nog lopende patroon-agents), commit
   (`Add <patroon-ID> pattern`, Engels, geen Claude-vermelding) en `git push origin main` (staande opdracht van de
   gebruiker, 2026-10-10). Draai de checks op de commit zelf in een losse worktree (`git worktree add --detach <scratchpad>/verify HEAD`), zodat half werk van lopende agents niet meetelt. Iets rood → niet pushen (`claude/docs/agent-werkwijze.md` §6), melden.
7. Geef de gebruiker een kort verslag (wat, waar, keuzes, testresultaat, commit-hash).
8. **Meteen door**: na de push niet wachten op de volgende tick. Is er een kandidaat (stap 3), claim en start die
   direct (stap 4–5). De cron blijft daarnaast elke 15 min een extra agent starten (parallel). (Gebruiker, 2026-10-11.)

## Agent-brief

Je werkt in `/mnt/dikkeschijf/documenten/Git/unshackle`. Lees eerst `CLAUDE.md`, `docs/projectinfo/README.md` (+ `architectuur/core.md`),
`claude/docs/code-standaarden.md` en `claude/docs/agent-werkwijze.md`. Docs in het Nederlands, code in het Engels.
Niet committen of pushen (zie `claude/docs/agent-werkwijze.md` §6: main blijft groen). Andere agents werken tegelijk in dezelfde map.

1. **Claim**: zet jouw rij in `claude/tasks/patroon-queue.md` op `bezig (TAAK-NNN)`. Was er al een taakbestand
   voor deze rij (geblokkeerd/mislukt)? Hervat dat volgens `.claude/skills/taak-oppakken/SKILL.md` in plaats van een nieuwe.
2. **Onderzoek → taak**: maak de taak volgens `.claude/skills/taak-specifiek/SKILL.md`. Onderzoek:
    - de FluentAssertions 7.2.2-API (overloads, `because`-argumenten, ketens met `.And`/`.Which`);
    - het Shouldly 4.3.0-equivalent en verschillen in gedrag;
    - gevallen die van het type afhangen, en gevallen die `Manual` moeten worden;
    - voorkomens in `samples/SampleShop/SampleShop.Tests/`.
      Leg elke keuze met reden vast in de taak (sectie **Keuzes**).
3. **KingmaDoc-plan** (slug = patroon-ID met `.` → `-`, bv. `fa-be-null`): als `docs/projectinfo/patterns/<slug>-plan.md`
   nog niet bestaat, `kingmadoc plan --no-input -o docs/projectinfo/patterns/<slug>-plan.md "<patroon-ID>: <één zin>"`.
   Vul daarna scope, requirements (EARS, één per proof-geval of groep), aannames, risico's en open vragen in vanuit
   je taak; geen `_TODO_` laten staan als de taak het antwoord heeft. `files_expected` = je pattern-bestand,
   `FluentAssertionsFix.cs` en de proof-map. Zet in de frontmatter `feature: "<slug>"` (de CLI maakt anders een lange slug uit de omschrijving). Dan `kingmadoc check <slug>` en `kingmadoc approve <slug>` (de loop is
   door de gebruiker goedgekeurd). Voeg een rij toe aan de patroontabel in `docs/projectinfo/README.md`.
4. **Blokkade**: is rij 0 (fundament) nog niet `klaar`? Zet de realisatiestap op `[!]`, de rij op
   `geblokkeerd (TAAK-NNN, wacht op fundament)` en stop.
5. **Realiseer** (alleen je eigen bestanden):
    - `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/<Naam>Pattern.cs`;
    - één regel in de `Patterns`-lijst van `FluentAssertionsFix`;
    - proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/<Naam>/` (`*.before.cs` + `*.after.cs` of `*.manual.txt`),
      minimaal: simpel geval, `because`-argument, elk type-afhankelijk geval, elk Manual-geval.
      Contracten in `Abstractions/`, `Models/` of de `Fixer` wijzigen mag **niet**; nodig? → stap `[!]` met reden, rij `mislukt`.
6. **Verifieer**: `dotnet build` en `dotnet test` groen. Faalt het door een file-lock of een half-af bestand van een
   andere agent? Wacht 30 s en probeer opnieuw (max 5×) voordat je het als eigen fout ziet.
7. **KingmaDoc-verify**: schrijf `docs/projectinfo/patterns/<slug>-verify.md` volgens de KingmaDoc-skill (verify-modus,
   `~/.claude/skills/kingmadoc/SKILL.md`). `kingmadoc verify` mag als start, maar beoordeel alleen jouw eigen bestanden
   (andere agents wijzigen tegelijk). Elke REQ: gehaald (noem proof) / deels / niet. Vul de echte build/test-uitkomst in.
8. **Afronden**: taak `✅ voltooid`, naar `claude/tasks/archief/`, rij `klaar (TAAK-NNN)`. Lukt het niet:
   context in de taak, rij `mislukt (TAAK-NNN, poging N)`. De SubagentStop-hook blokkeert je stop zolang plan, verify
   of de README-rij van jouw patroon ontbreekt.
9. **Verslag** (max 8 regels): patroon, gewijzigde bestanden, kernkeuzes, proofs, testresultaat.

### Modellen-brief (na elk afgerond patroon)

Werk de visuele modellen van het hele project bij in `/mnt/dikkeschijf/documenten/Git/unshackle`, volgens de
explaining-code-skill (`~/.claude-timme/skills/explaining-code/SKILL.md` + alleen de benodigde secties van
`reference/c4-model.md` en `reference/models.md`). Nederlands. Alleen docs en afbeeldingen; geen code wijzigen,
niet committen.

- Feiten: `kingmadoc explain facts` (en `--only` voor delen). Niets verzinnen; tekst moet bij de figuren passen.
- Plek: **niet** `docs/explain/`, maar `docs/projectinfo/`:
  - `README.md`: C4 Context, Container en Component (Core) als D2-figuren (vervang de Mermaid-blokken), plus een
    tabel "Modellen" met links naar de bestanden hieronder.
  - `modellen/klassen.md`: UML-klassendiagram van contracten en modellen (`Abstractions/`, `Models/`).
  - `modellen/fix-flow.md`: UML-sequence van `FixAsync`: Fixer → IFix → IPattern.Find/Rewrite → ReplaceNode → usings.
  - `modellen/pattern-beslissing.md`: UML-activity: hoe een pattern kiest tussen Fixed en Manual (generiek, met de
    gedeelde stappen uit `FluentAssertionsSyntax`).
  - `modellen/patronen.md`: overzicht van alle patterns (tabel + figuur: welke FA-vorm → welke Shouldly-vorm,
    welke gevallen Manual), bijgewerkt met het nieuwste patroon.
  - Andere modellen alleen als het signaal in de code duidelijk is (zie de beslistabel in `reference/models.md`).
  - Figuren genummerd per document, max ~15 elementen per figuur, elke figuur met tabel; `## Zie ook` onderaan;
    nieuwe bestanden in `claude/docs/INDEX.md`.
- Render: `kingmadoc render docs/projectinfo/README.md docs/projectinfo/modellen/*.md` (PNG + SVG + D2 in `img/`).
  Bij een D2-fout: `.d2` repareren en opnieuw renderen. Bestaat het al: alleen bijwerken wat het nieuwe patroon raakt.
- Verslag (max 5 regels): gewijzigde bestanden, aantal figuren, wat er nieuw is.

### Extra voor rij 0 (fundament)

Bouw wat `docs/projectinfo/architectuur/core.md` beschrijft, minimaal en volgens de code-standaarden:

- `Abstractions/`: `IPattern`, `IFix`, `IFixer` (met XML-docs);
- `Models/`: `Finding`, `PatternResult` + `FixedResult` + `ManualResult`, `FixRequest`, `FixReport`;
- `Fixer`: draait alle patterns van de gekozen fix op een `Solution`, vangt exceptions per pattern (Manual), geeft een
  nieuwe `Solution` + rapport terug;
- `Fixes/FluentAssertions/FluentAssertionsFix.cs` met een `Patterns`-lijst;
- tests: `Helpers/TestReferences` (echte assemblies per fix), een in-memory solution-helper, `ProofTests` die alle
  proofs vindt en de compileercheck (before met FA, after met Shouldly) doet;
- het eerste patroon `fa.should-be` met proofs, zodat de hele keten bewezen is;
- unittests voor de `Fixer` met NSubstitute.
  Leg de vorm van `IPattern` en hoe herschrijvingen in de `Solution` komen vast onder **Keuzes**, met reden.

## Zie ook

- `claude/tasks/patroon-queue.md` — de wachtrij
- `.claude/skills/taak-specifiek/SKILL.md` — taakformaat
- `claude/docs/code-standaarden.md` — codestijl
