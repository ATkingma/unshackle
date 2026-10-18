# Models: automapper.convert-using

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/Converters.cs`, `CreateMapPattern.cs`, `MemberValues.cs`, `NestedMaps.cs` and `NestedMap.cs`.

> No figures: `kingmadoc`/`d2` were not available in this environment; the tables carry the models.

## Figure 1: classes

| Element            | Role                                                                                                                                  |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| `CreateMapPattern` | `TryParseMap` splits off a trailing `.ConvertUsing(...)` and `ForMember` converter options; `Rewrite` asks `Converters` instead of `Problem` for a type converter map; `Extensions` writes `=> body;` |
| `Converters`       | `TypeProblem` (type level), `MemberOption` + `MemberProblem` (member level), `LambdaProblem`, `CreationProblem`, `InstanceProblem`, `ConvertCall` (interface, implementation, unread parameters, speculative binding) |
| `NestedMaps`       | Marks a map as `Converts` (ConvertUsing chained to the CreateMap, or a rewritten `To*` method without `new()`)                        |
| `NestedMap`        | `Converts`: the converter is called with a null source too                                                                            |
| `MemberValues`     | `Nested`: `source.X.ToB()` for a converter map (`!` for a maybe-null member in a nullable context), no cycle check                   |

## Figure 2: sequence

| Step      | Method in the code                                  | What happens                                                                                   |
| --------- | --------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Parse     | `CreateMapPattern.TryParseMap`, `Converters.MemberOption` | `ConvertUsing` on the map (nothing else configured) or per member                         |
| Type map  | `Converters.TypeProblem`                            | Name clash, lambda or converter (by type or instance) → body                                   |
| Converter | `Converters.ConvertCall`                            | Implements `ITypeConverter<A, B>`/`IValueConverter<S, M>` implicitly in this project, does not read destination/context, the call binds |
| Member    | `CreateMapPattern.Problem` → `Converters.MemberProblem` | Source expression (lambda or same-named member) → `converter.Convert(value, null)`        |
| Nested    | `NestedMaps.AddProfiles`/`AddRewritten` → `MemberValues.Nested` | Other maps call `source.X.ToB()`                                           |
| Write     | `CreateMapPattern.Extensions`                       | `public static B ToB(this A source) => body;` (multi-line bodies re-indented)                  |

## Figure 3: decision tree

| Leaf                                        | Condition                                                         | Evidence                                   |
| ------------------------------------------- | ----------------------------------------------------------------- | ------------------------------------------ |
| Manual "reads the destination" / "uses the ResolutionContext" | lambda or `Convert` uses that parameter          | `CreateMap/convert-using-context`, unit test `Rewrite_ConvertUsingNotWritable_ReportsManual` |
| Manual "has constructor parameters"         | generic converter with a constructor parameter                    | `CreateMap/convert-using-service`          |
| Manual "explicitly" / "not in this project" / "ambiguous" | the call cannot be written or checked               | unit test `Rewrite_ConvertUsingNotWritable_ReportsManual` |
| Manual "already has a member"               | a method name the source type already has (a `string` destination gets `ToMappedString`, TAAK-273) | code review; `_Combined/convert-using-string` |
| Manual "Map(source, destination) runs ConvertUsing" | the map needs `MapInto`                                   | code (`CreateMapPattern.Rewrite`)          |
| Fixed `=> body`                             | expression lambda                                                 | `CreateMap/convert-using-lambda`, `_Combined/convert-using-map-call` |
| Fixed `=> new C().Convert(source, null, null)` | type converter                                                 | `CreateMap/convert-using-type-converter`   |
| Fixed `X = converter.Convert(source.Y, null)` | value converter                                                 | `CreateMap/convert-using-member`           |

## See also

- [automapper-convert-using-plan.md](automapper-convert-using-plan.md)
- [automapper-convert-using-verify.md](automapper-convert-using-verify.md)
- [automapper-nested-maps-modellen.md](automapper-nested-maps-modellen.md)
