# Models: automapper.generic-profile

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/GenericProfilePattern.cs` and `src/Unshackle.Core/Generics/TypeArgumentClosure.cs`. Diagrams are missing (d2/kingmadoc not available); tables replace them.

## Classes

| Element | Role |
| ------- | ---- |
| `GenericProfilePattern` | `ISolutionPattern`: finds top-level generic profiles (`IsGenericProfile`), closes them, writes the extension class |
| `GenericProfileCode` | Reason code `GenericProfileOpenSet` |
| `CreateMapPattern` (+ ForMember/ValueResolver subclasses) | `Find` skips top-level generic profiles |
| `MapOwners` | The partial plan skips top-level generic profiles (their `CreateMap<T, T>` has no pair to plan) |
| `TypeArgumentClosure` (Core) | `CloseByUses` over the class type parameters; R1 reason carries the A13 advice (`ProjectName`) |
| `CreateMapProblems`, `MemberValues`, `MapMethodWriter` | Member check and writing per closed pair, as for a plain `CreateMap<A, B>()` |

## Sequence

| Step | Method | What happens |
| ---- | ------ | ------------ |
| Find | `Find` / `IsGenericProfile` | Top-level class with type parameters, derives from `Profile`, has maps |
| Shape | `RewriteAsync` | Not partial, one constructor, every statement `CreateMap<A, B>();` |
| Close | `TypeArgumentClosure.Close` | Closed subclasses, `new P<X>()`, `AddProfile<P<X>>`; `typeof(P<>)`, R1/R2, reflection → open |
| Per pair | `Closed`, `CreateMapProblems.Problem` | Substitute, map into the profile's compilation, check the members; MapInto/ProjectTo targets → open |
| Rewrite | `MapMethodWriter.Emptied` / `Extensions` | Empty constructor, `<Profile>Extensions` with one `ToB` per pair |

## Decision tree

| Leaf | Condition | Evidence |
| ---- | --------- | -------- |
| Fixed: closed subclasses | every pair passes | `closed-subclasses`, `FixAsync_TwoClosedSubclasses_WritesBothPairs` |
| Fixed: construction or `AddProfile` | every pair passes | `FixAsync_ClosedConstruction_WritesMethodPerPair` |
| Fixed: public, `IsPackable=false` | project properties | `FixAsync_PublicProfile_FollowsIsPackable(false)` |
| Manual: packable public (A13 advice) | R1 | `packable-library`, `skoruba-shape`, `FixAsync_PublicProfile_FollowsIsPackable(true)` |
| Manual: `typeof(P<>)` | open registration | `open-registration` |
| Manual: no construction | empty set | `FixAsync_NoClosedConstruction_IsOpenSet` |
| Manual: options / nested type parameter | shape | `FixAsync_UnsupportedMap_IsOpenSet` |

## See also

- [automapper-generic-profile-plan.md](automapper-generic-profile-plan.md)
- [automapper-generic-profile-verify.md](automapper-generic-profile-verify.md)
- `src/Unshackle.Tests/Proofs/AutoMapper/GenericProfile/`
