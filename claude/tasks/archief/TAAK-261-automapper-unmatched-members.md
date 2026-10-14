# TAAK-261 — automapper: unmatched and get-only destination members

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: the corpus scan had 72 Manual findings "B.X has no member with the same name in A, nor one AutoMapper flattens or reads through a Get method" (create-map 60, for-member-map-from 12) and about 13 "has no public setter but A has the member". Rewrite them the way AutoMapper 14 behaves.

## Evidence (AutoMapper 14.0.0, file-based `dotnet run` in a scratch folder)

| Destination member of `CreateMap<S, D>()` | Result |
| --- | --- |
| no source member, initializer `"init"` / no initializer | keeps `"init"` / `0`; only `AssertConfigurationIsValid` throws ("Unmapped properties") |
| `{ get; private set; }`, `{ get; protected set; }`, source has it | written from the source |
| get-only `List<int>` / `IReadOnlyList<int>` (backed by a list), source has it | cleared, ends with exactly the source items |
| get-only `int[]`, `string`, computed `=> ...`, `readonly` field, get-only object with its own map | keeps its value |
| `S? P` (struct) for `PX` | not flattened, keeps its initializer |

## Choices

- **Left out of the initializer**: a writable member without a source AutoMapper reads, and a get-only member without a setter that is not a collection. `new()` runs the same initializers AutoMapper's construction runs, `MapInto` leaves the member alone like `Map(source, destination)`, and a `ProjectTo` projection does not select it either.
- **`AssertConfigurationIsValid` keeps it Manual**: that check throws on an unmatched destination member, so if the solution calls it and its tests pass, AutoMapper must map the member in a way the fix does not see (a convention, a member list, a mapping elsewhere). Leaving it out would then change behaviour; Manual is the safe answer. Any call in the solution counts (not only one that provably reaches the map): simple and conservative; without the solution the compilation is searched. Corpus: only neozhu (9 findings) has such a call.
- **Manual**: a non-public setter or a get-only collection (any enumerable except an array or string) when the source has the member (also through a case-insensitive name, `GetX` or a flattening prefix).

## Steps

- [x] Measure AutoMapper 14.0.0
- [x] `CreateMapPattern`: unmatched member left out (Manual with `AssertConfigurationIsValid`), get-only rules, `Asserts`/`AssertsAsync`, `IsFilledCollection`
- [x] Proofs `CreateMap/unmatched-member` (Fixed), `unmatched-asserted`, `private-setter`, `get-only-collection` (Manual); `ForMemberIgnore/unmatched` became an `.after.cs`
- [x] Unit tests in `CreateMapPatternTests` (`Rewrite_MemberAutoMapperDoesNotWrite_LeftOut`, updated Manual cases)
- [x] REQ-8 in `docs/projectinfo/patterns/automapper-create-map-plan.md`, verify and models

## See also

- `docs/projectinfo/patterns/automapper-create-map-plan.md`
- `docs/projectinfo/patterns/automapper-create-map-verify.md`
- `claude/tasks/archief/TAAK-256-automapper-flattening.md`
