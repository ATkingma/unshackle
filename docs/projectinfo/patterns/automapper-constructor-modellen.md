# Models: automapper.constructor

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`Rewrite`, `Problem`, `ConstructorProblem`, `Extensions`) and `MemberValues.cs`.

> The figure was created as a D2 source in `img/`; `kingmadoc`/`d2` were not available in this environment, so the PNG/SVG follow with the next `kingmadoc render`.

## Figure 1: decision tree

**Figure 1.** The questions for one `CreateMap<A, B>` whose destination may need a constructor.

<!-- kingmadoc:diagram img/automapper-constructor-modellen-1.d2 -->
![Figure 1: decision tree](img/automapper-constructor-modellen-1.png)

| Step         | Method in the code                       | What happens |
| ------------ | ---------------------------------------- | ------------ |
| Constructor  | `CreateMapPattern.Problem`               | No public parameterless constructor: the single constructor (implicit record copy constructor skipped) must be public |
| Arguments    | `CreateMapPattern.ConstructorProblem`    | Per parameter the single source member matching in any case, through `MemberValues.Problem`; `creation` = `new(...)` |
| Members      | `CreateMapPattern.Problem`               | Members named like a parameter (any case) are skipped; the rest as before |
| MapInto      | `CreateMapPattern.Rewrite`               | A constructor map that needs `MapInto` is Manual |
| Write        | `CreateMapPattern.Extensions`            | `=> new(...)` with the object initializer when members remain |

## See also

- [automapper-constructor-plan.md](automapper-constructor-plan.md)
- [automapper-constructor-verify.md](automapper-constructor-verify.md)
