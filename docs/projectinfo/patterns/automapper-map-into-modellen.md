# Models: automapper.map-into

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapIntoPattern.cs`, `CreateMapPattern.cs` and `MapCallPattern.cs`.

> The figures were created as D2 sources in `img/`; `kingmadoc`/`d2` were not available in this environment, so the PNG/SVG follow with the next `kingmadoc render`.

## Figure 1: classes

**Figure 1.** `MapIntoPattern` and `CreateMapPattern` as `ISolutionPattern`; `MapCallPattern` provides the profile search.

<!-- kingmadoc:diagram img/automapper-map-into-modellen-1.d2 -->
![Figure 1: classes](img/automapper-map-into-modellen-1.png)

| Element            | Role                                                                                           |
| ------------------ | ---------------------------------------------------------------------------------------------- |
| `MapIntoPattern`   | Id `automapper.map-into`; `Find` via `IsMapIntoCall`; `Rewrite` only with an existing `MapInto` |
| `IsMapIntoCall`    | AutoMapper's `Map<A, B>` with two arguments, the second parameter of type `B`                  |
| `TargetsAsync`     | All pairs `A|B` (`Key`) that a `Map(a, b)` in the solution uses                                |
| `CreateMapPattern` | `RewriteAsync` writes `MapInto` for those pairs, after the `ToB` methods                       |
| `GeneratedAsync`   | From `MapCallPattern`: profile with `CreateMap<A, B>`, namespace, internal, project            |

## Figure 2: sequence

**Figure 2.** Profile rewrite and call rewrite in the Fixer.

<!-- kingmadoc:diagram img/automapper-map-into-modellen-2.d2 -->
![Figure 2: sequence](img/automapper-map-into-modellen-2.png)

| Step      | Method in the code                              | What happens                                              |
| --------- | ----------------------------------------------- | --------------------------------------------------------- |
| Profile   | `CreateMapPattern.RewriteAsync`, `TargetsAsync` | `MapInto` added for maps that `Map(a, b)` uses            |
| Find      | `Find`, `IsMapIntoCall`                         | AutoMapper's `Map(a, b)`                                  |
| Check     | `Check`                                         | Receiver, types, collection, null of source and destination |
| Existing  | `Existing`                                      | A `MapInto` that already exists (also just generated here) |
| Profiles  | `MapCallPattern.GeneratedAsync`                 | Which profile has the map, namespace, accessibility       |
| Write     | `Fixed`                                         | `a.MapInto(b)`, parentheses where needed, `using` if needed |

## Figure 3: decision tree

**Figure 3.** The questions of `RewriteAsync`.

<!-- kingmadoc:diagram img/automapper-map-into-modellen-3.d2 -->
![Figure 3: decision tree](img/automapper-map-into-modellen-3.png)

| Leaf                              | Condition                                 | Evidence                                        |
| --------------------------------- | ----------------------------------------- | ----------------------------------------------- |
| Manual "not called as mapper.Map" | the receiver is a call                    | `receiver-call`                                 |
| Manual "no type of its own"       | source or destination `object`/`dynamic` | `object-source`                                 |
| Manual "maps a collection"        | source or destination is a collection     | `collection`                                    |
| Manual "source may be null"       | flow state of the source is MaybeNull     | `maybe-null-source`                             |
| Manual "destination may be null"  | flow state of the destination is MaybeNull | `maybe-null-destination`                        |
| Manual "No CreateMap"             | no `MapInto`, no rewritten profile        | `unknown-map`                                   |
| Manual "Several profiles" etc.    | several profiles, internal, own member    | —                                               |
| Fixed (`a.MapInto(b)`)            | everything above passed                   | `existing-method`, `map-into-profile`, unit test |

## See also

- [automapper-map-into-plan.md](automapper-map-into-plan.md)
- [automapper-map-into-verify.md](automapper-map-into-verify.md)
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapIntoPattern.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/MapInto/`
