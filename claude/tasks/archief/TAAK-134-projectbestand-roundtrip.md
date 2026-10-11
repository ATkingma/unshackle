# TAAK-134 — Projectbestanden: "cannot be edited without changing unrelated text" bij gewone opmaak

**Status**: klaar
**Aangemaakt**: 2026-10-11
**Gevonden in**: `claude/tasks/CORPUSRUN-mediatr.md` — grootste blokkade: 6 van 9 solutions

## Probleem

`ProjectFile` (DataAccess) weigert elk bestand waarvan de `XDocument`-roundtrip niet byte-gelijk is. In het corpus
faalde dat op heel normale opmaak:

| Opmaak | Voorbeeld | Projecten |
| --- | --- | --- |
| `/>` zonder spatie | `<InternalsVisibleTo Include="X"/>` | evo2, kgrzybek |
| spatie vóór `>` | `<PackageReference Include="StyleCop.Analyzers" >` | amantin, nadirbad |
| `>` in attribuut | `Include="@(DistFiles->'%(FullPath)')"` | jason (`Web.csproj`) |
| attributen over meerdere regels | `<PackageReference`⏎`Include=...` | evo1 |
| gemengde CRLF/LF | test-csproj met twee LF-regels | amantin |

Bovendien wordt de dichtstbijzijnde `Directory.Build.props` altijd geladen, ook als er niets in hoeft te
veranderen (geen MediatR erin): de weigering raakt dan alle projecten eronder. Elke weigering is een Manual,
dus via alles-of-niets 0 wijzigingen.

Gemeten: met deze bestanden semantisch gelijk genormaliseerd (`scratchpad/scripts/norm.py`) verdwijnt deze
Manual overal; evo1 en evo2 worden dan volledig herschreven (en lopen tegen TAAK-130/131/135 aan).

## Wat, waar, hoe

- **Waar**: `src/Unshackle.DataAccess/Projects/ProjectFile.cs`, `XmlProjectFileStore.cs`.
- **Hoe** (kleinste eerst): (1) een bestand pas laden/valideren als er echt iets in verandert;
  (2) tekstuele splice: alleen de gewijzigde elementen opnieuw schrijven (posities via `IXmlLineInfo`), de rest
  van de tekst ongemoeid. Dan zijn `/>`, `->`, meerregelige attributen en gemengde regeleinden irrelevant.
- Geen proof (DataAccess): unittests in `src/Unshackle.Tests/DataAccess/` per rij uit de tabel.

## Stappen

- [ ] Rode tests per opmaakvorm + "Directory.Build.props zonder MediatR blijft onaangeraakt"
- [ ] Lazy laden
- [ ] Splice-schrijven
- [ ] `dotnet build` + `dotnet test` groen

## Uitkomst (2026-10-11)

**Status**: klaar. `ProjectFile` loopt de originele tekst naast het `XDocument` af en schrijft alleen gewijzigde nodes
(attribuutwaarden in place, nieuwe elementen ingevoegd, verwijderde weggeknipt); de reader normaliseert geen regeleinden.
Lazy laden bleek overbodig: een ongewijzigd bestand levert geen change op. Tests per opmaakvorm (BOM, `/>`, `" >`,
`->`, meerregelige attributen, gemengde CRLF/LF, comments, `'`-quotes) + ongewijzigde `Directory.Build.props`.
De oude test "closing tag in trailing comment gooit" is nu "blijft correct" (de scanner snijdt niet meer op `</Project>`).

Bewijs: evo1 (8 Fixed / 0 Manual) en evo2 (25 / 0) komen door alles-of-niets heen; Polly en Ardalis.Result zie TAAK-111.

## Zie ook

- `claude/tasks/CORPUSRUN-mediatr.md`
