# Unshackle: projectidee

## In één zin
Een `dotnet tool` die .NET-code met Roslyn automatisch migreert weg van bibliotheken die
commercieel zijn geworden. Eerst een rapport, dan een preview, dan pas wijzigen.

## Het probleem
In 2025 stapten drie veelgebruikte .NET-bibliotheken over op een commerciële licentie:

| Bibliotheek | Laatste vrije versie | Daarna | Gangbaar alternatief |
|---|---|---|---|
| FluentAssertions | 7.2.2 (Apache-2.0) | vanaf 8: commercieel | Shouldly, AwesomeAssertions |
| MediatR | 12.5.0 (Apache-2.0) | vanaf 13: RPL-1.5 of commercieel | Mediator (source-generated), gewone DI |
| AutoMapper | 14.0.0 (MIT) | vanaf 15: RPL-1.5 of commercieel | Mapperly, handgeschreven mapping |

Teams kunnen de oude versie vastpinnen, maar krijgen dan geen updates meer. Wie migreert,
moet honderden tot duizenden plekken aanpassen. Find-and-replace werkt niet, omdat de
juiste herschrijving afhangt van types en overloads.

Bewijs dat de behoefte er is:
- [RoseMCP #334](https://github.com/AtomicBlom/RoseMCP/issues/334): 3.724 assertions in ~150 bestanden
  naar Shouldly. "No tool rewrites a code pattern across a solution", dus het plan was een
  Roslyn-script om daarna weg te gooien.
- [Altinn/dialogporten #3024](https://github.com/Altinn/dialogporten/issues/3024): MassTransit,
  FluentAssertions, AutoMapper en MediatR vervangen.

## Wat er al bestaat (onderzoek 10 oktober 2026)
| Tool | Waarom het het gat niet dicht |
|---|---|
| [FluentAssertionsToShouldlyConverter](https://github.com/kmtchl/FluentAssertionsToShouldlyConverter) | Eén bibliotheek, lijst- en dictionary-checks vaak handwerk |
| [Shouldly.FromFluentAssertions](https://github.com/galadril/Shouldly.FromFluentAssertions) | PowerShell, vervangt tekst en weet niks van types |
| AutoMapperToMapperly (mark-new) | Alleen .NET Framework (volgens zoekresultaten) |
| [AutoMap.Generator](https://github.com/Swevo/AutoMap.Generator) | Laat de AutoMapper-configuratie staan, `.ForMember` nog niet |
| [NuGet License Gate](https://marketplace.visualstudio.com/items?itemName=ReadyStack.nuget-commercial-license-gate) | Waarschuwt alleen |
| [RoseMCP](https://github.com/AtomicBlom/RoseMCP) `rose_replace_pattern` | Algemeen semantisch zoek-en-vervang in een MCP-server, zonder recepten |
| OpenRewrite / Moderne | C#-recepten zijn betaald |
| MediatR → Mediator | Niks geautomatiseerds gevonden |

**Het gat:** er bestaan losse scripts per bibliotheek, maar geen tool die meerdere bibliotheken
aankan, naar types kijkt en vooraf rapporteert wat automatisch kan en wat niet.

## Hoe het eruit moet zien
```
dotnet tool install -g unshackle

unshackle scan MijnApp.sln --fix fluentassertions --report rapport.csv
unshackle fix  MijnApp.sln --fix fluentassertions --dry-run
unshackle fix  MijnApp.sln --fix fluentassertions
unshackle list
```

## Voorbeelden van herschrijvingen

**FluentAssertions → Shouldly**
```csharp
result.Should().Be(42);                     →  result.ShouldBe(42);
users.Should().HaveCount(3);                →  users.Count.ShouldBe(3);        // List
                                            →  users.Count().ShouldBe(3);      // IEnumerable
name.Should().BeEquivalentTo("timme");      →  name.ShouldBe("timme", StringCompareShould.IgnoreCase);
act.Should().Throw<ArgumentException>();    →  Should.Throw<ArgumentException>(act);
```
Roslyn is hier nodig om twee redenen. Of `.Count` of `.Count()` goed is, hangt af van het type.
En `BeEquivalentTo` op strings negeert hoofdletters. Met tekstvervanging worden tests ongemerkt strenger.

**MediatR → Mediator**: `using MediatR` wordt `using Mediator`, `Task<T> Handle` wordt
`ValueTask<T> Handle` (alleen bij methodes die de MediatR-interface implementeren), en
`AddMediatR(...)` wordt `AddMediator()`.

**AutoMapper → handgeschreven mapping**: elk `CreateMap<A, B>()` met `.ForMember` wordt een
`ToDto()`-extensiemethode met alle properties uitgeschreven (ook de impliciet gemapte), en
`_mapper.Map<B>(a)` wordt `a.ToDto()`. `ProjectTo<>` komt in het rapport als handwerk.

## Roadmap
Stand op 2026-10-13 (zie `claude/docs/overdracht.md` voor het lopende werk).

| Stap | Doel | Stand |
| --- | --- | --- |
| 1 | Scan + rapport (CSV/JSON/Markdown), solution laden, aanroepen tellen | Klaar |
| 2 | FluentAssertions → Shouldly: de 15 meest gebruikte assertions, `--dry-run` met diff | ~97%: corpusronde 4: 16/16 bouwt na de fix, 13/16 groen; regressies daarna opgelost (TAAK-220..223), alleen Akka DData blijft (bug in Akka, waarschuwing). Open: push + corpusronde 5 ([corpus.md](architectuur/corpus.md)) |
| 3 | MediatR → Mediator: handlers, `ValueTask`, DI-registratie (behaviors → rapport) | 100%: 7 van 9 corpusprojecten volledig gemigreerd en groen; evo1/evo2 terecht Manual |
| 4 | Lancering: README met GIF, NuGet, testen op een echt open-source project, post op r/dotnet | Inhoudelijk klaar (TAAK-019); wacht op de gebruiker (publiceren, post) |
| 5 | AutoMapper → handgeschreven mapping (`ToB()`-extensies) | ~40%: patronen create-map, ignore, map-from, map-call, map-into, add-automapper klaar (TAAK-146, -155..159). Eerst verder uitwerken (rij 7 + veelgebruikte geavanceerde API's), daarna een grote corpusrun zoals bij FA/MediatR |
| 6 | Later: de motor los beschikbaar maken als algemeen zoek-en-vervang (werktitel SharpGrep) | Niet begonnen |

## Risico's
- **De hype zakt weg.** Daarom de motor generiek houden (zie stap 6).
- **Kleine verschillen in gedrag.** Daarom altijd eerst een rapport en `--dry-run`, en na de fix de tests van de gebruiker draaien.
- **Anderen komen met losse converters.** Snelheid telt.
