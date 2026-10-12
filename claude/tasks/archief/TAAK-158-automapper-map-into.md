# TAAK-158 — automapper.map-into

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: rij 5 van `claude/tasks/automapper-queue.md`: `mapper.Map(src, dest)` → `src.MapInto(dest)` of Manual.

## Onderzoek

- AutoMapper 14 `IMapper`: `Map<TSource, TDestination>(TSource, TDestination)` is de map-into-vorm; `Map<TSource, TDestination>(TSource, Action<...>)` heeft ook twee argumenten (opties) en hoort er niet bij. Herkenning: tweede parameter heeft het type van `TDestination`.
- `Map(a, b)` geeft `b` terug; AutoMapper maakt een nieuw object als `b` null is en geeft bij een null-bron null terug.
- `CreateMapPattern` meldde `Map(a, b)` als Manual ("map-into"); de Fixer fixt elk bestand tegen de originele solution, dus een `MapInto` uit een ander bestand bestaat nog niet voor het semantic model.
- SampleShop heeft geen `Map(a, b)`.

## Keuzes

- `MapInto` wordt door `CreateMapPattern` geschreven, maar alleen voor maps die een `Map(a, b)` in de solution gebruikt: `CreateMapPattern` werd een `ISolutionPattern` (`RewriteAsync` → `MapIntoPattern.TargetsAsync`). Zo blijven alle bestaande proofs gelijk. `Rewrite` zonder solution schrijft geen `MapInto`.
- `MapInto` staat na alle `ToB`-methoden: `MapCallPattern.GeneratedAsync` koppelt methode i aan CreateMap i (en gebruikt de sync `Rewrite`, dus ziet `MapInto` niet).
- `MapInto` wijst dezelfde members toe als `ToB` en geeft `destination` terug, zodat een gebruikt resultaat werkt.
- `MapIntoPattern` volgt `MapCallPattern` (bestaand → gegenereerd); `GeneratedAsync`, `HasNoSideEffects`, `NeedsParentheses`, `IsCollection` werden `internal` voor hergebruik.
- Manual: ontvanger met bijwerkingen, `object`/`dynamic`, collectie, bron of doel MaybeNull, geen map, meerdere profiles, internal in ander project, eigen member `MapInto`.
- Proof `CreateMap/map-into` verwijderd (CreateMapPattern vindt die aanroep niet meer); Manual-proofs in `Proofs/AutoMapper/MapInto/`, Fixed met profile in `_Combined/map-into-profile`; profile in ander bestand: `MapIntoPatternTests`.

## Stappen

- [x] 1. Onderzoek.
- [x] 2. `MapIntoPattern`, `CreateMapPattern.RewriteAsync`/MapInto, registratie.
- [x] 3. Proofs, unittest, `proofs-doc.py`.
- [x] 4. Plan, modellen, verify, README-rij.

## Zie ook

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-157-automapper-map-call.md`
- `docs/projectinfo/patterns/automapper-map-into-plan.md`
