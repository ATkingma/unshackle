# Models: automapper.value-resolver

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ValueResolverPattern.cs`, `ValueResolvers.cs` and `CreateMapPattern.cs`. Written by hand as tables (no figures: `kingmadoc`/`d2` were not available).

## Classes

| Element                   | Role                                                                                                                     |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `ValueResolverPattern`    | `CreateMapPattern` subclass; `Find` claims profiles where `ValueResolvers.UsesResolver` is true                            |
| `ForMemberMapFromPattern` | `Find` now skips those profiles, so each profile still has exactly one profile pattern                                     |
| `ValueResolvers`          | `Option` (syntax of the resolver forms), `UsesResolver`, `Problem` (value or Manual reason per member), `Resolver`, `Lambda` |
| `CreateMapPattern`        | `TryParseOptions` collects resolver forms; `Rewrite` builds one `ValueResolvers` per map; `Problem` asks it first          |
| `MemberValues`            | Unchanged; turns the resolver result into the member value (conversions, nested maps)                                      |

## Sequence

| Step      | Method in the code                                  | What happens                                                                         |
| --------- | --------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Claim     | `ValueResolverPattern.Find`                         | Profile with a `ForMember(d => d.X, o => o.MapFrom...)` in a resolver form            |
| Parse     | `ProfileSyntax.TryParseMap` → `TryParseOptions`  | Resolver forms into `resolverCalls`, `Ignore`/`MapFrom(s => …)` into `options`; a member configured twice → no match |
| ReverseMap| `CreateMapPattern.Rewrite`                          | Resolver forms next to `ReverseMap()` → Manual                                         |
| Member    | `CreateMapProblems.Problem` → `ValueResolvers.Problem` | Per form: `Resolver` (class checks, `Resolve` body) or `Lambda` (parameter use, availability outside the profile) |
| Value     | `MemberValues.Problem`                              | Conversion of the result to the member type; a lambda also allows implicit numeric/nullable conversions |
| Write     | `MapMethodWriter.Extensions`                       | `X = new R().Resolve(source, default!, …)` or `X = expr` in `ToB` (and `MapInto`)      |

## Decision tree (`ValueResolvers.Problem`)

| Question                                                                                  | No → | Yes → |
| ----------------------------------------------------------------------------------------- | ---- | ----- |
| Is the call AutoMapper's `MapFrom`?                                                       | Manual | next |
| Lambda form: expression body, uses only the source parameter, names available outside the profile? | Manual (names the parameter) | `expr` |
| Resolver: plain non-generic class, no constructor parameters?                             | Manual (DI) | next |
| Base `object`, one empty/implicit accessible constructor, no instance fields, accessible type? | Manual | next |
| One public `Resolve` that implicitly implements the AutoMapper interface, source in the project? | Manual | next |
| `Resolve` body does not use destination, member value or context?                          | Manual (names the parameter) | `new R().Resolve(...)` |
| `MemberValues.Problem` gives a value (same type, conversion, nested map)?                 | Manual | Fixed |

## See also

- [`automapper-value-resolver-plan.md`](automapper-value-resolver-plan.md), [`automapper-value-resolver-verify.md`](automapper-value-resolver-verify.md)
