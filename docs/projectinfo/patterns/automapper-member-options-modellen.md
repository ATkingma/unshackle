# Models: automapper.member-options

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberOptions.cs` (`TryParse`) and `CreateMapPattern.cs` (`TryParseOptions`, `Rewrite`, `Problem`, `PathProblem`, `ConditionProblem`, `KeepsDefault`).

> The figure was created as a D2 source in `img/`; `kingmadoc`/`d2` were not available in this environment, so the PNG/SVG follow with the next `kingmadoc render`.

## Figure 1: decision tree

**Figure 1.** The questions for one `Condition`, `PreCondition` or `ForPath` option of a `CreateMap<A, B>`.

<!-- kingmadoc:diagram img/automapper-member-options-modellen-1.d2 -->
![Figure 1: decision tree](img/automapper-member-options-modellen-1.png)

| Step       | Method in the code                     | What happens | Proof |
| ---------- | -------------------------------------- | ------------ | ----- |
| Parse      | `MemberOptions.TryParse`               | Condition/PreCondition with a source lambda, MapFrom, ForPath of one or two members; other options set `Unsupported` | `CreateMap/null-substitute`, `CreateMap/condition-destination`, `CreateMap/for-path-deep` |
| ForPath    | `CreateMapPattern.PathProblem`         | `A = new() { B = expr }` when `A` is settable, a class with `new()`, and not mapped itself | `CreateMap/for-path`, `CreateMap/for-path-member` |
| Condition  | `CreateMapPattern.ConditionProblem`, `KeepsDefault` | The member keeps `default` on a new object, the condition passes `MapFromProblem` | `CreateMap/condition`, `CreateMap/condition-map-from`, `CreateMap/pre-condition`, `CreateMap/condition-initializer` |
| Guard      | `CreateMapPattern.Rewrite`             | With MapInto or ReverseMap: Manual | `CreateMap/reverse-map-options`, unit tests |
| Map call   | `MapCallPattern`                       | `Map<B>(a)` → `a.ToB()` | `_Combined/condition-map-call` |

## See also

- [automapper-member-options-plan.md](automapper-member-options-plan.md)
- [automapper-member-options-verify.md](automapper-member-options-verify.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MemberOptions.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/`
