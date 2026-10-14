# TAAK-245 — automapper.nested-maps

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 8 of `claude/tasks/automapper-queue.md`: a destination member whose type pair has its own `CreateMap` (also inside a collection) becomes `X = source.X?.ToY()` / `Xs = source.Xs?.Select(x => x?.ToY()).ToList() ?? []` in the generated `ToB` method, with AutoMapper 14's null behaviour.

## Context for the next agent

- The profile rewrite lives in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (shared by `ForMemberIgnorePattern` and `ForMemberMapFromPattern`). Nested maps are a capability of that shared rewrite, not a pattern class of their own: a profile is claimed by exactly one of the three profile patterns, so a fourth pattern cannot claim "the profiles with nested members".
- AutoMapper behaviour was measured, not remembered (see **Evidence**).

## Evidence (AutoMapper 14.0.0, `dotnet run nested.cs` / `existing.cs` / `same.cs` in `<scratch>/amcheck*`)

| Case | AutoMapper 14 result |
| --- | --- |
| `Address` → `AddressDto` (own map), source non-null | new `AddressDto`, not the same reference |
| same, source member null | `null` (also when the destination property has an initializer `= new …`) |
| `Tag` (class) → `TagDto` (struct), source null | `default(TagDto)` (also when the struct has a parameterless constructor that sets a value) |
| `Point` (struct) → `PointDto` (struct) | mapped |
| `Point?` → `PointDto?` | null → null, value → mapped |
| `List<Address>` → `List<AddressDto>` with a null element | `List[AddressDto, null]`: a null element stays null |
| `A[]` → `B[]` | `B[]` |
| `IEnumerable<A>`/`ICollection<A>`/`IReadOnlyList<A>`/`List<A>` → `IEnumerable<B>`/`ICollection<B>`/`IReadOnlyList<B>`/`IList<B>`/`IReadOnlyCollection<B>` | runtime type `List<B>` |
| null source collection (any of the above) | empty `List<B>` / empty `B[]` (`AllowNullCollections` is null = off) |
| `List<int>`/`List<string>`/`int[]`/`List<Address>` → same type | a new collection (not the same reference); elements are the same references |
| same type with `CreateMap<Address, Address>` | the map runs: member and elements are new instances |
| `MapFrom(s => s.Address)` to `AddressDto` | via the map; null → null |
| `MapFrom(s => s.Numbers)` `List<int>` → `List<int>` | new list; null → empty list |
| Map into an existing destination (`Map(src, dest)`) | maps *into* the existing nested object and the existing list (same instances) |
| New destination whose nested member has an initializer | a new nested object (the initializer's instance is not reused) |
| Cycle `Node.Parent`/`Node.Children` | AutoMapper detects it and preserves references (`child.Parent` is the mapped root) |
| Nested pair without a `CreateMap` | `AutoMapperConfigurationException` ("cannot be mapped") |
| `AllowNullDestinationValues` | null (= default on) |

## Choices

- **Where the nested map may live**: in any profile one of the three profile patterns claims (`IsProfileWithMaps`), in the profile's own project or a project it depends on (only those can be called from the generated code). The fix is all-or-nothing (`AutoMapperFix.AllOrNothing`): if that other profile ends Manual, nothing is applied, so a nested call never points to a method that is not written. An internal extension class in another project, two maps of the same pair, or a source type that already has a member `ToB` → Manual. A profile in the same file that an earlier pattern already rewrote is found through its generated extension method (the patterns run one after another on the same file).
- **Method name and namespace** come from the CreateMap syntax the same way `Extensions` names them (`To` + last name of the destination type as written); a using for the other profile's namespace is added when needed.
- **Single member**: reference → reference `source.X?.ToY()`; struct → anything `source.X.ToY()`; `A?` → `B?` (structs) `source.X?.ToY()`; class → struct `source.X?.ToY() ?? default`. In a nullable-enabled context with a non-nullable destination the result gets `!`: AutoMapper writes null there too, and a warning would break builds with warnings-as-errors.
- **Collections**: source and destination each one of `T[]`, `List<T>`, `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`. Destination array → `ToArray()`, otherwise `ToList()` (AutoMapper's runtime type is `List<B>`); `?? []` for the null source (`new List<B>()` / `new B[0]` below C# 12); `System.Linq` is added. Elements: own map → `x => x?.ToB()` (null elements stay null), struct element `x => x.ToB()`; same element type without map → `source.Xs?.ToList() ?? []` (a new collection, same element references). The existing proofs `CreateMap/collection` and `ForMemberMapFrom/collection` (same element type) become `.after.cs`: the new output is AutoMapper's behaviour.
- **Same type with its own `CreateMap<T, T>`**: the map is called (deep copy), as AutoMapper does; before this task it was copied by reference.
- **Cycles**: Manual when the nested destination type can reach the outer destination type again through member types (conservative: AutoMapper preserves references in a cycle, recursion would overflow).
- **MapInto**: a map that needs `MapInto` (a `Map(src, dest)` call) and has a nested or collection member is Manual: AutoMapper maps into the existing nested object/list, a plain assignment replaces it.
- **Null settings**: an assignment to AutoMapper's `AllowNullCollections` or `AllowNullDestinationValues` anywhere in the solution (typically `AddAutoMapper(cfg => ...)` in the host project) changes the null behaviour above, so nested and copied members are Manual then (inside a profile constructor such a statement already makes the profile Manual).
- **Index cache**: `NestedMaps.FromAsync` is cached per `Solution` instance and project (a `Solution` never changes), because `MapCallPattern.GeneratedAsync` rewrites every profile per `Map` call.
- **Manual**: dictionaries, `HashSet`/`Collection<T>`/custom collections, nested pair without a map, generic/open maps (not a `CreateMap<A, B>()`), `A?` → `B` and other nullability mixes of structs.

## Steps

- [x] 1. Research (AutoMapper behaviour measured), choices.
- [x] 2. Implementation: `NestedMaps`/`NestedMap`/`MemberValue` helpers, `CreateMapPattern` uses them, `MapCallPattern.GeneratedAsync` uses the solution-aware rewrite.
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify, README row, `proofs-doc.py`.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.
- [x] 6. Queue row `done`, task to archive, INDEX row.

## Result

- New: `NestedMaps.cs`, `NestedMap.cs`, `MemberValues.cs`; changed `CreateMapPattern.cs` (uses them, `MethodName`, `RewriteMethodsAsync`), `MapCallPattern.cs` (`GeneratedAsync` static, rewrites profiles with the solution), `MapIntoPattern.cs` (unused field gone).
- Proofs: `CreateMap/nested`, `nested-collections`, `nested-nullable`, `nested-same-type`, `nested-other-profile` (Fixed), `nested-no-map`, `nested-cycle`, `nested-several`, `collection-dictionary` (Manual), `ForMemberMapFrom/nested`, `_Combined/nested-map-call`, `_Combined/nested-profile-rewritten-earlier`. `CreateMap/collection` and `ForMemberMapFrom/collection` changed from `.manual.txt` to `.after.cs` (a new list is AutoMapper's behaviour). Unit tests: `src/Unshackle.Tests/Core/NestedMapsTests.cs` (cross-project, internal, other file, C# 11, null setting, MapInto).
- `dotnet build` 0 warnings; `dotnet test` 1166/1166; SampleShop 38/38.
- Open: PNG/SVG of the models not rendered (no `kingmadoc`/`d2`). `Map<List<B>>(list)` calls stay Manual (`automapper.map-call`). The cycle check is conservative (self-referencing DTOs stay Manual).

## See also

- `claude/tasks/automapper-queue.md` — row 8
- `claude/tasks/archief/TAAK-156-automapper-for-member-map-from.md`
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`
