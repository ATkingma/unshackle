# TAAK-156 — automapper.for-member-map-from

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: rij 3 van `claude/tasks/automapper-queue.md`: `.ForMember(d => d.X, o => o.MapFrom(s => expr))` → `X = expr` in de `ToB`-methode, samen met Ignore in hetzelfde profiel.

## Onderzoek

- AutoMapper 14: `MapFrom(Expression<Func<TSource, TMember>>)` is de enige overload met een lambda van één parameter; overloads met `(s, d)`, resolvers en een stringnaam bestaan ook.
- AutoMapper voert MapFrom-expressies null-veilig uit (een NullReferenceException geeft de standaardwaarde) en converteert typen. Een gewone toekenning doet geen van beide: dat bepaalt wat Manual wordt.
- Expression trees staan geen `?.` toe; een expressie met `x != null ? x.Y : ...` kan wel.

## Keuzes

- **Subklasse `ForMemberMapFromPattern : CreateMapPattern`** met gedeelde `Rewrite`, zoals bij TAAK-155 (een profile wordt als geheel vervangen). `Find` verdeelt exclusief: MapFrom-aanroep (welke vorm ook) → MapFrom, anders Ignore → Ignore, anders CreateMap. Zo vindt precies één pattern een profile, ook bij Ignore + MapFrom.
- **Ledenlijst `(Name, Value)`**: `source.X` of de herschreven expressie, in de volgorde van B.
- **Alleen `s => expr`** (expressie-body, één parameter, AutoMapper-symbool); `s` wordt via het symbool vervangen door `source`. Overige vormen: Manual met de bestaande "not a CreateMap<A, B>() without options".
- **Null-veiligheid via nullable-flow**: een ontvanger van `.lid`/`[ ]` mag `s`, een type of een expressie met `NullableFlowState.NotNull` zijn; anders Manual ("may be null"). Zonder nullable-context dus bijna altijd Manual; bewust, een stille gedragswijziging is erger.
- **Type**: gelijk aan het type van X, of impliciet numeriek/nullable; anders Manual (AutoMapper converteert). Collectie-X: Manual.
- **Buiten het profiel**: `this`, instantievelden, statics van het profiel, en een naam `source` die al iets anders is: Manual. Lokale parameters van geneste lambda's mogen.
- X moet publiek schrijfbaar zijn en maar één keer geconfigureerd.
- De Ignore-proof `map-from` (Manual) is vervallen.

## Stappen

- [x] 1. Onderzoek en keuzes.
- [x] 2. Plan `docs/projectinfo/patterns/automapper-for-member-map-from-plan.md` (handmatig).
- [x] 3. `ForMemberMapFromPattern`, `CreateMapPattern` uitgebreid, registratie in `AutoMapperFix`.
- [x] 4. Proofs (`simple`, `ignore-and-nullable`, 5x Manual), `ForMemberMapFromPatternTests`, `proofs-doc.py`.
- [x] 5. Modellen (D2-bronnen), verify, README-rij, queue.

## Resultaat

- `dotnet build` 0 warnings (met `-p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507`); 736/771 tests groen, ProofTests en pattern-tests 568/568. De 35 falers (XmlProjectFileStoreTests, FixerTests, CommandTests, 2 SampleShop-integratietests) zijn de bekende CRLF-falers.

## Zie ook

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-155-automapper-for-member-ignore.md`
- `docs/projectinfo/patterns/automapper-for-member-map-from-plan.md`
- `docs/projectinfo/patterns/automapper-for-member-map-from-verify.md`
