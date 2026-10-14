# TAAK-254 — automapper.value-resolver

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 20 of `claude/tasks/automapper-queue.md`: `MapFrom<TResolver>()`, `MapFrom(new TResolver())`, `IValueResolver`, `IMemberValueResolver` (`MapFrom<TResolver, TSourceMember>(s => s.Y)`) and the `MapFrom((s, d) => …)` / `MapFrom((s, d, m, ctx) => …)` lambdas → hand-written values in `ToB`, or Manual.

## Context for the next agent

- New `ValueResolverPattern` (claims profiles with a resolver form; `ForMemberMapFromPattern` now skips them) and helper `ValueResolvers`. `CreateMapPattern.cs` changes are small: `TryParseOptions`/`TryParseMap` collect resolver forms in an extra dictionary, `Problem` takes an optional `ValueResolvers`, `IsReference`/`IsAvailableOutsideProfile` became `internal`, `ReverseMap` with resolver forms is Manual.
- The resolver class is not rewritten: it keeps AutoMapper's interface (and `ResolutionContext` parameter) until the package goes, like the emptied profiles.

## Evidence (AutoMapper 14.0.0, file-based app with `#:package AutoMapper@14.0.0`, `#:property PublishAot=false`)

- `IValueResolver.Resolve` gets the destination being built (`R1=0 R4=init`: initializers ran; a later lambda saw `d.R1=2` set by an earlier resolver), `destMember` = current value (0; 99 with `Map(a, existing)`), `context` non-null.
- `MapFrom<R>()`: constructor count 1 after the first `Map`, 2 after the second (new per call). `MapFrom(new RI())`: same instance on the second call.
- `(s, d) => s.Customer!.Name` with `Customer` null → `AutoMapperMappingException` / inner `NullReferenceException`; a throwing resolver → `AutoMapperMappingException` / inner `InvalidOperationException`; `MapFrom<MR5, string>(s => s.Customer!.Name)` with null `Customer` → wrapped `NullReferenceException` (not null-safe).
- `MapFrom<RS>()` with `IValueResolver<A, B, int>` on a `long` member does not compile (CS0311): `TDestMember` is the member type. `(s, d) => s.Id` into `string` gave `"1"`, into `long` gave `1`.
- Struct destination: the resolver got `default` (`d.R1=0`).

## Corpus (TAAK-244 clones, read-only)

| Repo | Shape | Outcome |
| --- | --- | --- |
| personalasst | `IValueResolver<ToDoList, object, bool>` resolvers reading `context.Items["UserId"]`; one with an `ICdnService` constructor | Manual (context / DI) — proofs `context-items`, `di-resolver` |
| letterbook | `MapFrom<MediaTypeResolver, Content?>(s => s.Contents.Order().FirstOrDefault())` etc. with `IMemberValueResolver`s; several use `context.Mapper` or `destMember`, some are clean | clean ones rewritten (proof `member-resolver`), others Manual |
| letterbook | `MapFrom((_, post) => post.FediId.Host)` (uses the destination) | Manual — proof `lambda-destination` |

## Steps

- [x] Measure AutoMapper 14 behaviour
- [x] `ValueResolvers` + `ValueResolverPattern`, hooks in `CreateMapPattern`, `ForMemberMapFromPattern.Find`, `AutoMapperFix.Patterns`
- [x] Proofs `ValueResolver/` (3 after, 3 manual) + `_Combined/value-resolver-map-call`; `ForMemberMapFrom/resolver-form` removed (now rewritten)
- [x] Unit tests `ValueResolverPatternTests` (10)
- [x] Plan/models/verify `docs/projectinfo/patterns/automapper-value-resolver-*`
- [x] `dotnet build`, `dotnet test` (1303 green), SampleShop (38 green)

## See also

- `docs/projectinfo/patterns/automapper-value-resolver-plan.md`
- `claude/tasks/automapper-queue.md` (row 20)
