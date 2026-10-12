# Patroon-wachtrij (AutoMapper ≤14 → handgeschreven mapping)

**Besluit (2026-10-11, gebruiker)**: handgeschreven mapping (`ToDto()`-extensiemethoden), geen Mapperly.

Rij 0 en 1 klaar (TAAK-146): handgeschreven mapping gekozen. Zelfde werkwijze als `claude/tasks/mediatr-queue.md`; rij 0 krijgt bij claimen een TAAK-nummer.
Volgorde op frequentie in `docs/projectinfo/architectuur/corpus-mediatr-automapper.md` (corpus klein, vooral nopCommerce).

| #   | Patroon-ID                       | AutoMapper                                                                                                                                                                            | Verwacht                                                                | Corpus (treffers / projecten) | Status |
| --- | -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- | ----------------------------- | ------ |
| 0   | _infrastructuur_                 | `AutoMapperFix`, proof-runner, typen AutoMapper 14.0.0                                                                                                                                | —                                                                       | —                             | klaar (TAAK-146) |
| 1   | `automapper.create-map`          | `Profile` + `CreateMap<A,B>()` zonder opties                                                                                                                                          | `static B ToB(this A a) => new() { ... }` met alle gelijknamige members | 243 / 3                       | klaar (TAAK-146) |
| 2   | `automapper.for-member-ignore`   | `.ForMember(d => d.X, o => o.Ignore())`                                                                                                                                               | member weglaten in de mapping                                           | 1015 / 1                      | klaar (TAAK-155) |
| 3   | `automapper.for-member-map-from` | `.ForMember(d => d.X, o => o.MapFrom(s => expr))`                                                                                                                                     | `X = expr` (lambda-parameter vervangen)                                 | 6 / 3                         | klaar (TAAK-156) |
| 4   | `automapper.map-call`            | `mapper.Map<B>(a)`                                                                                                                                                                    | `a.ToB()`; `IMapper`-injectie weg als ongebruikt                        | 9 / 2                         | geblokkeerd (TAAK-157, wacht op TAAK-156) |
| 5   | `automapper.map-into`            | `mapper.Map(src, dest)`                                                                                                                                                               | `src.MapInto(dest)` of Manual                                           | 2 / 2                         | open   |
| 6   | `automapper.add-automapper`      | `services.AddAutoMapper(...)`                                                                                                                                                         | regel weg                                                               | 4 / 3                         | open   |
| 7   | `automapper.unsupported`         | `ProjectTo<T>`, `MapperConfiguration`/`AssertConfigurationIsValid`, `ReverseMap`, resolvers, converters, `AfterMap`, `Include(Base)`, `ConstructUsing`, `Condition`, `NullSubstitute` | Manual met reden (rapport)                                              | 3+5 / 2; rest 0               | open   |

Rij 7 splitsen zodra een groter corpus de geavanceerde API's wél laat zien.

## Zie ook

- `docs/projectinfo/project.md` — doel AutoMapper → handgeschreven mapping
- `docs/projectinfo/architectuur/corpus-mediatr-automapper.md`
- `claude/tasks/mediatr-queue.md` — voorbeeld-wachtrij
