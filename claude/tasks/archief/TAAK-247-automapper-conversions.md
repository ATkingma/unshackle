# TAAK-247 — automapper.conversions

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 11 of `claude/tasks/automapper-queue.md`: a same-named member (or `MapFrom` expression, or collection element) whose type differs from the destination's and has no map gets AutoMapper 14.0.0's built-in conversion written by hand, but only where the hand-written form gives the same result as AutoMapper for every measured value (or the same exception type); everything else stays Manual with a precise reason.

## Context for the next agent

- Before this task such a member made its map Manual ("X is A in S but B in D (AutoMapper converts)"); `MapFrom` only accepted implicit numeric/nullable conversions.
- Like nested maps (TAAK-245) this is a capability of the shared profile rewrite (`CreateMapPattern` → `MemberValues`), not a pattern class: a conversion can occur in a profile any of the three profile patterns claims. New: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberConversions.cs`. `MemberValues.Problem` calls it after the nested-map lookup (single members and collection elements); `NestedMaps.ConfiguredAt` knows every `CreateMap<A, B>` of the solution (also outside profiles).
- Corpus (TAAK-244 clones, own regex script over the same type index as the survey, abp left out): 402 same-named member pairs with different type text in 19 projects. 136 of them only differ in a nullable reference annotation (`string?` → `string`, `Uri?` → `Uri`): Roslyn sees one type, they were already copied. Ranking of the real conversions: `Guid?` → `Guid` 35 / 2, `DateTime?` ↔ `DateTime` 28 / 4, enum ↔ `int` 22 / 1 (nop), `float` ↔ `double` 16 / 1, `int?` → `int` 8 / 3, `decimal?` → `decimal` 8, `DateTime?` → `string` 7 / 2, `string` → `int` 5 / 3, enum → enum 5, enum ↔ enum? 8 / 4, enum ↔ `string` 5 / 2, `bool?` ↔ `bool` 6 / 2, the rest 1–4 each; 52 involve a type parameter (`TId` → `Guid`, cats/jhipster; mostly the same type after substitution) and 35 own types (strongly typed ids, `string` → `ItemTypeName` in xipona, `PartyUrn` → `UrnJsonTypeValue<PartyUrn>`).

## Evidence (AutoMapper 14.0.0)

Measured with file-based programs `conv.cs`, `conv2.cs`, `conv3.cs` in `<scratch>/amcheck-conv` (`#:package AutoMapper@14.0.0`, `TEMP`/`TMP` = `<temp-dir>`): `CreateMap<S<TFrom>, D<TTo>>()` per pair, every value through `mapper.Map` and through the hand-written candidate, run with the invariant culture and with `nl-NL` (decimal comma, `dd-MM-yyyy`). "same" = equal result, or the same innermost exception type (AutoMapper wraps it in `AutoMapperMappingException`). Mapper order in 14.0.0 (`c.Internal().Mappers`): Collection, Assignable, NullableSource, **ToString**, NullableDestination, **Convert**, StringToEnum, EnumToEnum, ParseString, UnderlyingTypeEnum, KeyValue, Constructor, ConversionOperator (implicit, explicit), dynamic/dictionary mappers.

| Conversion | Values | AutoMapper 14 | Hand-written form | Result |
| --- | --- | --- | --- | --- |
| `int?`/`Guid?`/`DateTime?`/`decimal?`/`bool?`/`long?`/enum?/struct? → `T` | null, value, undefined enum 99, enum without 0 | null → `default(T)` (also over a member initializer `= 7`), value → value | `source.X.GetValueOrDefault()` | same |
| `T` → `T?` (`int`, `DateTime`, enum, `Guid`) | 0, value | value | `source.X` | same |
| value type → `string` (`int`, `decimal`, `double`, `float`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `bool`, `char`, struct) | incl. `NaN`, 0.1+0.2, both cultures | `ToString()` in the current culture (`"1,5"`, `"02-01-2024 03:04:05"` in nl-NL) | `source.X.ToString()` | same |
| `T?`/class/`object`/`Uri`/`byte[]` → `string` | null, value | null → null (also over an initializer `= "init"`), else `ToString()` | `source.X?.ToString()` | same |
| class with `implicit operator string` → `string` | null, value | `ToString()`, **not** the operator (ToStringMapper comes first) | `source.X?.ToString()` | same |
| enum → `string` | defined, undefined 99, `[Flags]` combination, `None` | the name / `"99"` / `"A, B"` | `source.X.ToString()` | same |
| enum with `[EnumMember(Value = "first-one")]` → `string` | `First` | **`"first-one"`** | `ToString()` gives `"First"` | DIFF → Manual |
| enum → enum, same names and values | defined, 99 | by value | `(Colour)source.X` | same |
| enum → enum, same names, other values (`Shuffled { Blue, Red, Green }`) | Red, Blue, 99 | **by name** (Red → Shuffled.Red(1)); undefined value by number | cast: DIFF; `Enum.Parse(ToString(), true)`: same; switch: same | switch |
| enum → enum, names differ in case (`RED = 11`) | Red, Green, Blue, 7 | by name **ignoring case** | switch | same |
| enum → enum, a name missing in the destination (`Partial { Red, Other, Green = 2 }`) | Blue(2), Green(1) | Blue → `Partial.Green` (the number 2), Green → `Partial.Green` (by name) | switch with an arm for Green, `_ => (Partial)x` | same |
| enum → enum, aliases (`Green = 0, Red = 0`) | Red | `ToString()` picks "Green" → `Colour.Green(1)` | cast gives `Red(0)` | DIFF → Manual when the names of one value map differently |
| enum → enum, destination with `Red = 5, RED = 6` | Red | `Red(5)` | — | Manual (several case-insensitive matches) |
| `[Flags]` → `[Flags]` with other values | A, B, A\|B | single flags by name, a combination (not defined) by number | switch | same |
| `Color?` → `Shuffled` | null | `default(Shuffled)` (= Blue, 0) | switch over `GetValueOrDefault()` gives Shuffled.Red | DIFF → Manual |
| enum (int) ↔ `int` | 2, 99, -1 | the number | `(int)source.X`, `(Color)source.X` | same |
| enum (int) → `long`/`byte`, `long` → enum, byte enum → `int` | any | **throws** `AutoMapperMappingException` ("unsupported mapping") | — | Manual |
| `long` → `int`, `double` → `int`, `float`/`decimal` → `int`, `int` → `short`/`uint`, `uint` → `int` | 2.5, 3.5, -1.7, 1e20, NaN, MaxValue | `System.Convert`: half to even (3.5 → 4, -1.7 → -2), `OverflowException` | cast: DIFF (truncates, wraps); `Convert.ToInt32(source.X)`: same | Convert |
| `double` → `float`, `double` → `decimal`, `decimal` → `double` | 1.1, 1e300, 1/3, 1e30 | Convert (∞, 15 digits, `OverflowException`) | `Convert.ToSingle/ToDecimal/ToDouble` | same |
| widening `int` → `long`/`float`/`double`/`decimal`, `float` → `double`, `short` → `float`, `char` → `int` | 16777217, MaxValue | the implicit value (16777217 → 16777216f) | `source.X` | same |
| `bool` ↔ `int`/`double`, `decimal` → `bool`, `int` → `char` | true, 0, 2, 0.5 | Convert | `Convert.ToX(source.X)` | same |
| `double` → `char` | 65.0 | `InvalidCastException` | — | Manual (always throws) |
| `int?` → `long`, `long?` → `int`, `double?` → `int`, `bool?` → `int` | null, value, overflow | null → 0, value → Convert | `Convert.ToInt32(source.X.GetValueOrDefault())` (`source.X.GetValueOrDefault()` when widening) | same |
| `int?` → `long?`, `Color?` → `int?`, `int?` → `Color?`, `Color?` → `Colour?`, `int` → `long?`, `double` → `int?` | null, value | null → null, value → converted | `source.X`, `(int?)source.X`, `(Color?)source.X`, `Convert.ToInt32(source.X)` | same |
| `long?` → `int?` | `long.MaxValue` | `OverflowException` | `(int?)source.X` gives -1 | DIFF → Manual |
| `string` → `int`/`long`/`sbyte`/`ulong`/`decimal`/`double`/`float`/`bool` | "42", " 42 ", "1.000", "1,5", "", null, "abc", overflow, "true", "1" | Convert in the current culture ("1,5" → 15 invariant, 1.5 in nl-NL); null → 0/false; "" → `FormatException` | `Convert.ToInt32(source.X)` etc.: same; `int.Parse`: DIFF on null | Convert |
| `string` → `DateTime` | "2024-01-02", "02-01-2024", "", null | current culture; null → `MinValue` | `Convert.ToDateTime(source.X)` | same |
| `string` → enum | "Blue", "blue", "1", "99", " Blue", "Green ", "A, B", "Nope", "", null | `Enum.Parse(ignoreCase: true)`; **null and "" → default** | `string.IsNullOrEmpty(source.X) ? default : Enum.Parse<Color>(source.X, true)` | same |
| `string` → enum with `[EnumMember]` | "first-one" | **`First`** | Enum.Parse throws | Manual |
| `string` → `int?`/`Guid?`/`Color?` | null, "" | null → null, "" → `FormatException` / `Color.Red` | — | Manual |
| `string` → `Guid`/`TimeSpan`/`char` | null | `Guid.Empty`/`00:00:00`/`'\0'` | `Guid.Parse` throws on null | Manual |
| `string` → class with `ctor(string)` (`Uri`) | null | null | `new Uri(null)` throws | no rule (Manual) |
| `DateTime` → `DateTimeOffset` | Utc, Local, Unspecified, `MinValue` | the implicit operator (local offset; `MinValue` throws `ArgumentOutOfRangeException` at +01:00) | `source.X` | same |
| `DateTime?` → `DateTimeOffset?` | null, value | null → null | `source.X` | same |
| `DateTimeOffset` → `DateTime`, `DateTime` ↔ `DateOnly` | any | **throws** (unsupported mapping) | — | Manual |
| `int` → `DateTime` | 1 | `new DateTime(1)` (ConstructorMapper, 1 tick) | — | no rule (Manual) |
| struct with `implicit operator decimal` / `explicit operator int` | value | the operator | `source.X` / `(int)source.X` | not written (no rule; rare) |
| `MapFrom(s => s.X)` `int?` → `int` / `string`; `MapFrom(s => s.A + s.B)` `long` → `int`; `s.D * 2` `double` → `int` | null, overflow, 1.75*2 | the same rules as a member | as a member | same |
| `List<int?>` → `List<int>`, `List<int>` → `List<string>`, `List<decimal?>` → `List<string>`, `List<Color>` → `List<Shuffled>`, `List<string>` → `List<int>`/`List<Color>`, `int[]` → `long[]`, `List<long>` → `List<int>` | null elements, overflow | every element by the same rules (null `string` element → 0 / default enum) | `source.Xs?.Select(x => …).ToList() ?? []` | same |

End-to-end check: every `.after.cs` of the new Fixed proofs compiled together with its original profile; 12 generated inputs per map (nulls, edge values, overflow) through `mapper.Map` and through the generated `ToB` method, compared as JSON, invariant and nl-NL: 216 checks, 0 differences (a mutant with `(int)` instead of `Convert.ToInt32`, `int.Parse` and `?? ""` gave 16 differences, so the harness sees them).

## Choices

- **Readable forms** (one per conversion, the shortest that is exact):
  - `T?` → `T`: `source.X.GetValueOrDefault()`. Works for every struct (enum, `Guid`, `DateTime`, own structs) and says what AutoMapper does; `?? 0` needs a literal per type, `?? default` hides the type.
  - `T` → `T?` and widening numbers: `source.X` (the implicit conversion gives the same value).
  - to `string`: `source.X.ToString()` for a non-nullable value type, `source.X?.ToString()` otherwise; in a nullable-enabled context with a non-nullable `string` destination `!` is added (AutoMapper writes null there too; same choice as TAAK-245 for nested maps).
  - narrowing and other numeric/bool/char pairs: `Convert.ToInt32(source.X)` etc. AutoMapper's ConvertMapper calls `System.Convert` (half to even, `OverflowException`); a cast truncates and wraps. Also `Convert.ToSingle` for `double` → `float`, although a cast is the same there: one rule is easier to read and to check.
  - enum ↔ enum: `(StatusDto)source.Status` when every source name that the destination has (ignoring case) has the same number there; otherwise `source.Priority switch { Priority.Low => PriorityDto.LOW, …, _ => (PriorityDto)source.Priority }` with an arm only for the names that land on another number (AutoMapper's by-name rule written out, the cast as AutoMapper's fallback for undefined values and missing names).
  - enum ↔ its underlying type: `(int)source.X` / `(Status)source.X`.
  - `string` → number/`bool`/`DateTime`: `Convert.ToInt32(source.X)` (current culture, null → default, exactly AutoMapper); `int.Parse` would throw on null.
  - `string` → enum: `string.IsNullOrEmpty(source.X) ? default : Enum.Parse<Status>(source.X, true)` (`(Status)Enum.Parse(typeof(Status), source.X, true)` when the target framework has no generic `Enum.Parse`).
  - Nullable on one side: `T?` → `U` as the conversion of `source.X.GetValueOrDefault()` only when the conversion maps the default to the default without throwing (numbers, casts); `T` → `U?` as the nullable cast (`(int?)source.X`, typed as the destination); `T?` → `U?` only when C# has a lifted conversion with the same result (implicit, enum casts).
- **`System` using** is added for `Convert`/`Enum` (the generated class may be in a file without it).
- **Collections** (TAAK-245 copies): element conversions use the same rules with `x`; when the expression is not of the destination element type itself (implicit forms) the lambda casts it (`Select(x => (long)x)`), otherwise `ToList()`/`ToArray()` would give the wrong collection type.
- **A `CreateMap` of the pair anywhere in the solution** (also `cfg.CreateMap<string, DateTime>().ConvertUsing(...)` in a `MapperConfiguration`/`AddAutoMapper` lambda, or the nullable-stripped pair) replaces the built-in conversion → Manual with its location. The nested-map lookup still comes first (a profile map of the pair is called as `ToY()`).
- **`AllowNullCollections`/`AllowNullDestinationValues`** assigned anywhere → conversions Manual (the null handling differs), as for nested maps.
- **Evaluated twice**: the switch and the `IsNullOrEmpty` check read the value twice, so a `MapFrom` expression that is not a plain name/member chain stays Manual for those two forms.
- **Manual** (precise reason): `[EnumMember]` enums to/from `string` and enum ↔ enum; enum ↔ a number type that is not its underlying type, `double`/`float`/`decimal`/`bool` ↔ `char`, `DateTimeOffset` → `DateTime`, `DateTime` ↔ `DateOnly` (AutoMapper 14 throws at run time); enums with different underlying types; aliases that map differently; several case-insensitive destination names; `Color?` → `Shuffled` (switch) and `DateTime?` → `DateTimeOffset` (default would differ); `long?` → `int?` (cast has no overflow check); `string` → `T?`; `string` → `Guid`/`TimeSpan`/`char`/`DateTimeOffset`/`DateOnly`/`TimeOnly`. No rule (generic "(AutoMapper converts)" reason): own types with conversion operators or constructors (strongly typed ids), `int` → `DateTime`, type parameters, `dynamic`.
- **Existing proofs**: `CreateMap/conversion` (enum → `string`) and `ForMemberMapFrom/conversion` (`int` → `string` via `MapFrom`) changed from `.manual.txt` to `.after.cs`: the new output (`source.Status.ToString()`, `source.Id.ToString()`) is AutoMapper's behaviour (Evidence). The generic Manual reason "(AutoMapper converts)" is now proven by `CreateMap/conversion-operator`.
- **ReverseMap**: unchanged (a profile with `.ReverseMap()` stays Manual until row 10).
- **No new pattern class / ID**: as TAAK-245/246, the queue ID names the docs slug.

## Steps

- [x] 1. Research: corpus ranking, AutoMapper 14 behaviour measured (three programs, two cultures), mapper order.
- [x] 2. Implementation: `MemberConversions`, `MemberValues` (single members, elements, configured maps, null settings), `NestedMaps.ConfiguredAt`, `CreateMapPattern` docs.
- [x] 3. Proofs + unit tests; end-to-end check of the Fixed proofs against AutoMapper.
- [x] 4. Plan, models, verify, README row, INDEX row, `proofs-doc.py`; older docs that named the `conversion` proofs updated.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.
- [x] 6. Queue row `done`, task to archive.

## Result

- New: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberConversions.cs`. Changed: `MemberValues.cs` (`Conversion`, element conversions), `NestedMaps.cs` (`ConfiguredAt`, `AddConfigured`), `CreateMapPattern.cs` (doc comment, `Description`).
- Proofs (Fixed): `CreateMap/conversion` (was Manual), `conversion-nullable`, `conversion-to-string`, `conversion-enum`, `conversion-numbers`, `conversion-from-string`, `conversion-collection`, `ForMemberMapFrom/conversion` (was Manual), `ForMemberMapFrom/conversion-expression`, `_Combined/conversion-map-into` (`Map<B>` + `Map(src, dest)` → `ToB()` + `MapInto`). Proofs (Manual): `CreateMap/conversion-enum-member`, `conversion-enum-number`, `conversion-string-nullable`, `conversion-string-guid`, `conversion-nullable-narrowing`, `conversion-nullable-enum-switch`, `conversion-operator`, `conversion-convert-using`.
- Unit tests in `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`: `Rewrite_BuiltInConversion_WritesIt` (15 cases), `Rewrite_BuiltInConversionNotWritable_ReportsManual` (13), `Rewrite_ConversionOfMapFromCall_OnlyWhenReadOnce` (3), `Rewrite_ConversionWithNullSetting_ReportsManual`, `Rewrite_ConversionToNonNullableString_SuppressesTheWarning`.
- `dotnet build` 0 warnings; `dotnet test` 1229/1229; SampleShop 38/38.
- Corpus estimate (402 member pairs, abp excluded): 136 only nullable reference annotations (already copied), about 179 now rewritten (`T?` ↔ `T` 116, number/enum 42, to `string` 13, from `string` 8), 52 with a type parameter (mostly the same type after substitution), 35 stay Manual (own types: strongly typed ids, value objects). Of the 266 real conversions about 67% is covered (plus most of the 52), 13% stays Manual.
- Open: own value types with conversion operators/constructors (xipona, letterbook ids) stay Manual (rows 19/20 may cover them via `ConvertUsing`); `string` → `Guid?` (cats, 1) and other `string` → `T?` could get a `source.X is null ? null : …` form if the corpus run shows more; the non-generic `Enum.Parse` fallback for old target frameworks has no test (the test references are .NET 10 only); PNG/SVG of the models not rendered (no `kingmadoc`/`d2`).

## See also

- `claude/tasks/automapper-queue.md` — row 11
- `docs/projectinfo/patterns/automapper-conversions-plan.md`
- `docs/projectinfo/patterns/automapper-conversions-verify.md`
- `claude/tasks/archief/TAAK-245-automapper-nested-maps.md`
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberConversions.cs`
