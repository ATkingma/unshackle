# TAAK-159 — automapper.add-automapper

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-12
**Doel**: rij 6 van `claude/tasks/automapper-queue.md`: `services.AddAutoMapper(...)` → regel weg.

## Onderzoek

- `AddAutoMapper` registreert `IMapper` en `IConfigurationProvider` (`IMapper` erft `IMapperBase`; `IRuntimeMapper` hoort erbij). `MapCallPattern` laat `IMapper`-injectie bewust staan (TAAK-157).
- `AddMediatRPattern` is een `ISolutionPattern` en zet de aanroep om naar `AddMediator`; voor AutoMapper is er geen vervanger.

## Keuzes

- Manual zolang een AutoMapper-service ergens in de solution genoemd wordt (ook tests die `IMapper` uit DI halen): zonder registratie faalt resolven pas op runtime. De melding noemt `bestand:regel`.
- `ISolutionPattern` met `SymbolFinder.FindReferencesAsync`; de sync `Rewrite` kijkt alleen in de eigen compilatie.
- Resultaat gebruikt (expression body, keten) → Manual: zeldzaam en de keten herschrijven is foutgevoelig.
- Parameter die alleen naar `AddAutoMapper` ging → `_ = p;`, zodat signatuur en aanroepers blijven (IDE0060).
- Pakket blijft: lege Profiles houden AutoMapper nodig.
- REQ-8 in `automapper-map-call-plan.md` bijgewerkt (proof `CreateMap/map-into` bestaat niet meer).

## Stappen

- [x] Pattern + regel in `AutoMapperFix`
- [x] Proofs `simple`, `parameter`, `mapper-injected`, `result-used`; `proofs-doc.py`
- [x] Plan, modellen, verify, README-rij
- [x] Build 0 warnings; tests: alleen de bekende 35 CRLF-fouten
