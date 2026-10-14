# TAAK-253 — automapper.convert-using

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 19 of `claude/tasks/automapper-queue.md`: `CreateMap<A, B>().ConvertUsing(...)` (lambda, `ITypeConverter<A, B>` by type or instance) becomes `B ToB(this A source) => <converter>`, member-level `IValueConverter` via `ForMember(d => d.X, o => o.ConvertUsing(converter, s => s.Y))` becomes `X = converter.Convert(source.Y, null)`, and other maps' members of a converted pair call the new `ToB`.

## Context for the next agent

- Before this task a profile with `ConvertUsing` was Manual ("is not a CreateMap<A, B>() without options"); a `ConvertUsing` outside a profile (`cfg.CreateMap<string, DateTime>().ConvertUsing(...)`) made built-in conversions of that pair Manual (TAAK-247) and still does.
- Corpus (TAAK-244 clones, `ConvertUsing` hits outside abp): letterbook (`ConvertUsing(id => new(id))`, `ConvertUsing<IdUuid7Converter>()`, `ConvertUsing<DateTimeOffsetMapper>()` with several `Convert` overloads, member-level `ConvertUsing<DateTimeOffsetMapper, DateTimeOffset?>()`), ombi (`ConvertUsing<ClaimsConverter>()`, `CreateMap<string, DateTime>().ConvertUsing<StringToDateTimeConverter>()`), kavita (`ConvertUsing<ServerSettingConverter>()`), skoruba (member-level `opts.ConvertUsing(AllowedSigningAlgorithmsConverter.Converter, x => x.AllowedSigningAlgorithms)`, a static instance), xipona (`ConvertUsing((src, _, ctx) => ...)` with the context, and `ConvertUsing<Converter>()` in tests). The proofs copy these shapes.

## Evidence (AutoMapper 14.0.0)

Measured with the file-based program `cu.cs` in `<scratch>/amcheck-cu` (`#:package AutoMapper@14.0.0`, `#:property PublishAot=false`; with AOT on, expressions run in the interpreter and throw `NotSupportedException`).

| Case | AutoMapper 14 | Hand-written form | Result |
| --- | --- | --- | --- |
| `Map<A, B>(null)` with `ConvertUsing(s => ...)`, `ConvertUsing((s, d) => ...)`, `ConvertUsing<TConverter>()`, `ConvertUsing(new C())` | the converter is **called with null** (a lambda dereferencing it throws `NullReferenceException`, unwrapped) | `ToB(null)` calls the same code | same |
| `Map<B>((object)null)` | null, the converter is not called | existing `Map` call rule (row `automapper.map-call`) | unchanged |
| member `A X` of another map, null | the converter is called with null; a `NullReferenceException` inside is wrapped in `AutoMapperMappingException` | `source.X.ToB()` (no `?.`) | same (innermost exception type) |
| collection element null (`List<Tag>` → `List<TagDto>`) | the converter is called with null | `x.ToTagDto()` in the `Select` | same |
| `Point?` → `PointDto?` member, null | null, not called | `source.Q?.ToPointDto()` | same |
| destination parameter | null for `Map<B>(a)` and for a member of a new object (also over an initializer), the existing value for `Map(a, b)` | `null`/`default` | same when unread (else Manual) |
| `ResolutionContext` | always set | `null` | same when unread (else Manual) |
| `ConvertUsing<TConverter>()` | a new converter per `Map` call (constructor count = calls) | `new TConverter()` | same |
| `ConvertUsing(new C())` (type and member level) | one instance, made at configuration | `new C()` per call | same for a converter without state |
| member `ConvertUsing(new V(), s => s.Child.Raw)` with `Child` null | **not null-safe**: `NullReferenceException` (wrapped), unlike `MapFrom` | `new V().Convert(source.Child.Raw, null)` | same |
| member `ConvertUsing(new V())` without a source expression | the same-named source member | `source.X` | same |
| member value null | the value converter is called with null | `Convert(source.X, null)` | same |
| `Map(a, existing)` with a type converter | the converter's result is returned, the destination is only passed in | — | Manual (`MapInto` would fill the destination) |

## Choices

- **Type level** (`Converters.TypeProblem`): an expression lambda becomes the body with its first parameter renamed to `source` (the `MapFrom` rules for names: no `this`, nothing local to the profile, no other `source`); a converter becomes `new C().Convert(source, null, null)` / `Instance.Convert(...)` (`default` for a value-type destination, `null!` in a nullable context). A block-bodied lambda stays Manual (rare in the corpus; it would need a method body).
- **Only safe converters**: `Convert` must implement the interface implicitly, be in source of this compilation (the body is checked for reads of the destination and context parameters) and the converter class must have only parameterless constructors (AutoMapper's `ServiceCtor`, with `AddAutoMapper` the service provider, would fill others). The call is bound speculatively; with several `Convert` overloads (letterbook's `DateTimeOffsetMapper`) the destination becomes `default(B)`.
- **Member level** (`Converters.MemberProblem`): `ForMember` with `o.ConvertUsing(...)` is collected next to `Ignore`/`MapFrom` options without changing which profile pattern claims the profile. The source expression is not run through the `MapFrom` null and conversion rules: AutoMapper passes its value as is and does not catch a `NullReferenceException` (measured); its type must be the converter's source type.
- **Nested**: `NestedMap.Converts` makes `MemberValues.Nested` call `source.X.ToB()` (no `?.`, `!` on a maybe-null member in a nullable context) and skip the cycle check (a converter does not map members).
- **Method name**: `MethodName` names keyword destinations by the .NET type (`CreateMap<Code, int>` → `ToInt32`); a source type that already has the name (`ToString` for `CreateMap<A, string>`) is Manual.
- **Layout**: a multi-line lambda body is re-indented relative to the line it starts on.

## Steps

- [x] 1. Research: corpus shapes, AutoMapper 14 behaviour measured.
- [x] 2. Implementation: `Converters.cs`, `CreateMapPattern` (`TryParseMap`, `Rewrite`, `Problem`, `Extensions`, `MethodName`), `NestedMaps`/`NestedMap` (`Converts`), `MemberValues.Nested`.
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.

## Result

- New: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/Converters.cs`. Changed: `CreateMapPattern.cs` (`TryParseMap` out `convertUsing`/`converters`, `Rewrite` converter branch + `MapInto` Manual, `Problem` member converters, `Extensions` `=> body;`, `MethodName` keywords, `IsReference`/`IsAvailableOutsideProfile`/`AllMembers` internal, doc comment), `NestedMaps.cs` (`Converts`), `NestedMap.cs` (`Converts`), `MemberValues.cs` (`Nested`).
- Proofs (Fixed): `CreateMap/convert-using-lambda`, `convert-using-type-converter`, `convert-using-member`, `_Combined/convert-using-map-call`. Proofs (Manual): `CreateMap/convert-using-context`, `convert-using-service`. `CreateMap/conversion-convert-using` (a `ConvertUsing` outside a profile) stays Manual.
- Unit tests in `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`: `Rewrite_ConvertUsingNotWritable_ReportsManual` (11 cases), `Rewrite_ConverterWithSeveralOverloads_PassesATypedDestination`, `Rewrite_ConverterInNullableContext_CallsItWithoutNullCheck`.
- `dotnet build` 0 warnings; `dotnet test` 1248/1248; SampleShop 38/38.
- Open: block-bodied lambdas; converters in another project (body not visible); `ConvertUsing` outside a profile; `ConvertUsing(Type)`; the `MapInto` Manual case has no test; queue row, INDEX and README rows are for the coordinator.

## See also

- `claude/tasks/automapper-queue.md` — row 19
- `docs/projectinfo/patterns/automapper-convert-using-plan.md`
- `docs/projectinfo/patterns/automapper-convert-using-verify.md`
- `claude/tasks/archief/TAAK-247-automapper-conversions.md`
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/Converters.cs`
