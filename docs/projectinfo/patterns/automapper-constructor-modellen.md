# Models: automapper.constructor

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`Rewrite`, `Problem`, `ConstructorChoice`, `ConstructorProblem`, `ConstructUsingProblem`, `Extensions`) and `MemberValues.cs`.

## Figure 1: decision tree

**Figure 1a.** The questions for one `CreateMap<A, B>` whose destination may need a constructor. Split into Figures 1a–1c (at most 12 arrows each).

<!-- kingmadoc:diagram img/automapper-constructor-modellen-1.d2 -->
![Figure 1: decision tree](img/automapper-constructor-modellen-1.png)

**Figure 1b.** Continuation of Figure 1a from its "Continued in Figure 1b" node.

<!-- kingmadoc:diagram img/automapper-constructor-modellen-2.d2 -->
![Figure 1: decision tree](img/automapper-constructor-modellen-2.png)

**Figure 1c.** Continuation of Figure 1b from its "Continued in Figure 1c" node.

<!-- kingmadoc:diagram img/automapper-constructor-modellen-3.d2 -->
![Figure 1: decision tree](img/automapper-constructor-modellen-3.png)

| Step         | Method in the code                       | What happens |
| ------------ | ---------------------------------------- | ------------ |
| ConstructUsing | `CreateMapProblems.ConstructUsingProblem` | A one-parameter expression lambda over the source and static members becomes the creation (TAAK-266); the constructors of the type are not consulted |
| Constructor  | `CreateMapProblems.ConstructorChoice`     | One public constructor: as before. Several: the most parameters AutoMapper can resolve (first declared on a tie, over a parameterless one); undecidable or private: Manual (TAAK-266) |
| Arguments    | `CreateMapProblems.ConstructorProblem`    | Per parameter the single source member matching in any case, through `MemberValues.Problem`; `creation` = `new(...)` |
| Members      | `CreateMapProblems.Problem`               | Members named like a parameter (any case) are skipped; the rest as before |
| MapInto      | `CreateMapPattern.Rewrite`               | A constructor map that needs `MapInto` is Manual |
| MaxDepth     | `CreateMapPattern.Rewrite`, `UnsupportedOptions.MaxDepthLimit` | `MaxDepth(n >= 2)` on an acyclic nested copy is dropped (TAAK-266) |
| Write        | `MapMethodWriter.Extensions`            | `=> new(...)` with the object initializer when members remain; a block body after a `ConstructUsing` that is no plain `new` |

## See also

- [automapper-constructor-plan.md](automapper-constructor-plan.md)
- [automapper-constructor-verify.md](automapper-constructor-verify.md)
