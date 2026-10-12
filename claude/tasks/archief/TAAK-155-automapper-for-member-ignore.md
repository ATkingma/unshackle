# TAAK-155 — automapper.for-member-ignore

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: rij 2 van `claude/tasks/automapper-queue.md`: `.ForMember(d => d.X, o => o.Ignore())` → lid weglaten in de `ToB`-methode.

## Onderzoek

- AutoMapper 14: `ForMember(Expression<Func<TDest, TMember>>, Action<IMemberConfigurationExpression<TSrc, TDest, TMember>>)`; ook een overload met stringnaam. `o.Ignore()` laat het bestemmingslid ongemoeid (ook als de bron een gelijknamig lid heeft).
- Corpus: 1015 treffers in 1 project; SampleShop-profiles gebruiken `ForMember` met `MapFrom` en `ReverseMap` (blijven Manual).

## Keuzes

- **Subklasse in plaats van los pattern met eigen `Rewrite`**: een profile wordt als geheel vervangen (`[profile, <Profile>Extensions]`). Twee patterns met elk een `Rewrite` op dezelfde klasse overschrijven elkaar. `ForMemberIgnorePattern : CreateMapPattern` deelt dus `Rewrite`; `Find` verdeelt de profiles (met Ignore → dit pattern, rest → `CreateMapPattern`), zodat precies één pattern een profile vindt. Contracten onaangeroerd.
- **Alleen de lambda-vorm `d => d.X` + `o => o.Ignore()`** met symboolcontrole op de AutoMapper-assembly. Stringnaam, geneste leden, blok-lambda's, `MapFrom` en `ReverseMap` → Manual met de bestaande "not a CreateMap<A, B>() without options".
- **Genegeerd lid wordt vóór de naam/type-controle overgeslagen**: ook een lid zonder bronlid of met een ander type is dan geen probleem.
- **Genegeerd lid dat B niet publiek heeft → Manual**: AutoMapper zou de configuratie afkeuren; niets zinnigs om weg te laten.
- Alleen `public` leden tellen; `AllOrNothing` en `Map`-Manual blijven zoals bij rij 1.

## Stappen

- [x] 1. Onderzoek en keuzes.
- [x] 2. Plan `docs/projectinfo/patterns/automapper-for-member-ignore-plan.md` (approved, handmatig: `kingmadoc` niet beschikbaar).
- [x] 3. `ForMemberIgnorePattern`, `CreateMapPattern` uitgebreid, registratie in `AutoMapperFix`.
- [x] 4. Proofs (`simple`, `mixed`, 4× Manual), `ForMemberIgnorePatternTests`, `proofs-doc.py`.
- [x] 5. Modellen (D2-bronnen; niet gerenderd), verify, README-rij, queue.

## Resultaat

- `dotnet build` 0 warnings; ProofTests en pattern-tests 556/556. Volledige run 723/759: 36 falers buiten dit patroon (FixerTests, XmlProjectFileStoreTests, CommandTests, SampleShop-integratie; regeleinden en een tweede NuGet-bron `afas` in deze omgeving).
- Build en test vragen in deze omgeving `-p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507` (NU1507 door twee NuGet-bronnen).

## Zie ook

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-146-automapper-create-map.md`
- `docs/projectinfo/patterns/automapper-for-member-ignore-plan.md`
- `docs/projectinfo/patterns/automapper-for-member-ignore-verify.md`
