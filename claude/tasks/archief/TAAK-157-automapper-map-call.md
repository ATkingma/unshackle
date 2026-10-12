# TAAK-157 — automapper.map-call

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: rij 4 van `claude/tasks/automapper-queue.md`: `mapper.Map<B>(a)` → `a.ToB()` (aanroep van de door `CreateMapPattern` gegenereerde extensiemethode); `IMapper`-injectie weg als ongebruikt.

## Onderzoek

- Gegenereerde methode: `<Profile>Extensions.To<B>(this A source)` in een static class naast de profile (zelfde namespace, publiek of internal), alleen voor profiles die volledig herschreven zijn.
- `CreateMapPattern.Find` vindt nu elke `Map`/`ProjectTo`-aanroep uit de AutoMapper-assembly en `Rewrite` meldt die als Manual ("not migrated yet (automapper.map-call)"). `AutoMapperFix.AllOrNothing` is `true`: zolang dat Manual-finding bestaat, wordt niets toegepast.
- Fixer: elke bestand wordt tegen de originele solution gefixt; de extensiemethode bestaat dus niet in het semantic model van het aanroepende bestand. Het pattern moet de naam zelf afleiden (`To` + naam van B) en vaststellen dat A -> B een CreateMap in een herschrijfbare profile heeft (vraagt `ISolutionPattern` of scan van de solution).
- Aanroepende bestanden hebben een `using` nodig voor de namespace van de extensions-klasse als die afwijkt (`RequiredNamespaces` op `FixedResult`).
- Voorkomens in SampleShop: `Map<CustomerDto>`, `Map<OrderDto>`, `Map<List<OrderLine>>` (collectie: Manual), `Map<Address>(dto)` (via ReverseMap: Manual).

## Blokkade (opgeheven na TAAK-156)

Zonder wijziging van `CreateMapPattern` is dit patroon niet af te ronden:

1. `CreateMapPattern.Find` moet de `InvocationExpressionSyntax`-tak voor `Map` loslaten (en `Rewrite` de Manual daarvoor), anders blijft elke `Map<B>(a)` een Manual die door `AllOrNothing` alles tegenhoudt, ook als dit patroon de aanroep herschrijft. `ProjectTo` en `Map(src, dest)` (rij 5) moeten daar Manual blijven.
2. Volgorde in `AutoMapperFix.Patterns`: `MapCallPattern` na de CreateMap-patterns registreren.

Een andere agent (TAAK-156) wijzigt `CreateMapPattern.cs` en `ForMemberIgnorePattern.cs`; daarom is niets gewijzigd. Hervatten na TAAK-156: stap 1 hierboven, dan `MapCallPattern.cs` (+ registratie), proofs in `src/Unshackle.Tests/Proofs/AutoMapper/MapCall/` (simpel, namespace-using, collectie-Manual, onbekende map-Manual, `IMapper` blijft in gebruik), plan/modellen/verify, README-rij.

## Keuzes

- `MapCallPattern` is een `ISolutionPattern`: zonder solution (`Rewrite`) telt alleen een `ToB` die al bestaat.
- Eerst een bestaande `ToB` (in scope, of in de broncode van de compilatie met een `using`): dat dekt handgeschreven methoden én het profile in hetzelfde bestand, dat de Fixer dan al herschreven heeft (patterns draaien na elkaar op hetzelfde bestand).
- Anders: profiles in het eigen project en zijn afhankelijkheden, via `CreateMapPattern.Rewrite`; methodenaam en `internal` komen uit de gegenereerde klasse (statement i ↔ methode i). Internal in een ander project → Manual.
- `CreateMapPattern.Find` sluit `MapCallPattern.IsMapCall` uit (één argument, één of twee typeargumenten); `ProjectTo`, `Map(src, dest)` en `Map` met opties blijven daar Manual (proof `map-call` hernoemd naar `map-into`). `IsProfileWithMaps` werd `protected internal`.
- Manual: collectie, bron `object`/`dynamic`, bron MaybeNull, ontvanger met mogelijke bijwerkingen (alleen namen/`this`-ketens worden weggelaten), meerdere profiles, geen map.
- `IMapper`-injectie blijft: weghalen breekt aanroepers (tests, DI); naar rij 6.
- Fixed-proofs met een profile kunnen niet in `ProofTests` (één pattern; `ToB` ontbreekt dan): `CombinedProofTests` kreeg een AutoMapper-theorie op `Proofs/AutoMapper/_Combined/`. Profile in een ander bestand/project: unittests `MapCallPatternTests`.

## Stappen

- [x] 1. Onderzoek.
- [x] 2. `CreateMapPattern` aanpassen, `MapCallPattern`, registratie.
- [x] 3. Proofs, tests, `proofs-doc.py`.
- [x] 4. Plan, modellen, verify, README-rij.

## Zie ook

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-155-automapper-for-member-ignore.md`
- `docs/projectinfo/patterns/automapper-map-call-plan.md`
