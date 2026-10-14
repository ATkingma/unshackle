# TAAK-248 — automapper.profile-shapes

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 21 of `claude/tasks/automapper-queue.md`: the profile rewrite also handles a profile nested in a DTO and an expression-bodied profile constructor; generic, partial and multi-constructor profiles get a precise Manual reason instead of the shared "nested, generic, partial or has several constructors".

## Context for the next agent

- Before this task `CreateMapPattern.Rewrite` returned Manual for every profile whose parent was not a namespace/compilation unit, that was generic or partial, or whose single constructor had no block body.
- Like nested maps (TAAK-245) and member lists (TAAK-246) this is a capability of the shared profile rewrite in `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`, not a pattern class: the three profile patterns (`CreateMap`, `ForMemberIgnore`, `ForMemberMapFrom`) all claim nested profiles.
- The Fixer replaces exactly the node a pattern found (`FixedResult.NewNodes` replace it); contracts may not change. An extension method needs a top-level static class, so for a nested profile `Find` reports the outermost type and the rewrite replaces that type by [type with emptied constructor, extension class].

## Corpus (TAAK-244 survey clones, `<corpus>/src`, brace-depth scan of every `class X : Profile`)

205 profile classes. Shapes:

| Shape | Profiles | Projects | Example |
| --- | --- | --- | --- |
| top-level, one block constructor | 109 | 14 | — |
| nested in a class, block constructor | 39 | 4 | jason `LookupDto.Mapping`, neozhu `AuditTrailDto.Mapping`, abp test classes |
| nested, expression-bodied constructor (`public Mapping() =>\n    CreateMap<…>()…;`) | 49 | 1 | cats `NoteDto.Mapping`; 4 in records (`TenantDto`), 8 two levels deep (`AddNote.Command.Mapping`) |
| nested in a record, block constructor | 1 | 1 | cats `AddActivity` |
| no `CreateMap` in the profile | 3 | 1 | personalasst (maps elsewhere) |
| several constructors | 2 | 1 | dasblog `ProfilePost`, `ProfileDasBlogUser`: an empty parameterless one plus one taking a service that holds the maps |
| generic | 2 | 1 | skoruba `IdentityMapperProfile<TUserDto, …>`: `CreateMap<TUser, TUserDto>` on type parameters |
| partial | 1 | 1 | nop `AdminMapperConfiguration`: constructor calls helper methods (`CreateConfigMaps()`) |

One outermost type holds two nested profiles: cats `AddPRI` (`PriReleaseDto.Mapper`, `PriMeetingDto.Mapper`). The survey's "73 nested in 6 projects" was a cruder text count; the scan above counts 89 nested profiles in 4 projects (abp 9, cats 61, jason 4, neozhu 15). Nested profiles are `private` in most cases; the DTOs around them are public.

## Choices

- **Where the extension class goes**: right after the outermost type, in its namespace, with that type's indentation. Reason: extension methods need a non-nested static class; next to the DTO keeps the code together, like the top-level form puts it next to the profile.
- **Name**: `<Outer…><Profile>Extensions` (`TodoItemBriefDtoMappingExtensions`, `AddNoteCommandMappingExtensions`); top-level profiles keep `<Profile>Extensions`. Reason: in the jason/cats style every nested profile is called `Mapping` or `Mapper`, so the profile name alone clashes in every namespace; the full nesting path is unique per file. A type of that name that already exists → Manual (existing rule).
- **The emptied profile**: stays, with an empty constructor, as for a top-level profile; an expression body becomes `{ }` on its own lines (initializer and trailing comment kept).
- **Find reports the outermost type**: the Fixer can only replace the found node. Consequence: the finding's line is the outer type's line.
- **Several profiles in one outermost type**: Manual ("holds several profiles"). Reason: one finding replaces the type once; doing several profiles (possibly claimed by different patterns) in one rewrite would complicate the shared rewrite for 1 type in the corpus.
- **Names in a nested profile**: a type argument that only binds inside the containing types is written as `ToMinimalDisplayString` at the outer type (`AddNote.Command`); if that would change the method name (a generic type qualified by its container) → Manual. `MapFrom` names must be accessible from the whole assembly and a simple name must bind to the same symbol next to the outer type (a static helper of the DTO called as `Serialize(...)` → Manual). The accessibility check runs for top-level profiles too (a protected base member was moved before and did not compile).
- **Private DTO nested in a query class**: the existing accessibility rule already gives Manual ("is private or protected; an extension method cannot use it"); proof added.
- **Generic profile**: Manual. Its maps are on type parameters (`CreateMap<TUser, TUserDto>`); a generic extension method would need the constraints and the type arguments of every `AddProfile` call.
- **Partial profile**: Manual. Another part can hold more maps, constructor helpers or members the maps use; the nop case also calls helper methods, which is Manual on its own.
- **Several constructors**: Manual. Which maps exist depends on the constructor AutoMapper or the code runs (dasblog: the parameterless one has no maps, the other uses an injected service); rewriting would make maps exist that did not.

## Steps

- [x] 1. Research: current behaviour, corpus shapes (scan above), Fixer contract.
- [x] 2. Implementation in `CreateMapPattern.cs` (`Find`, `RewriteAsync`, `Rewrite`, `TryParseMap`, `Extensions` signature; new `Anchor`, `MapStatements`, `ExtensionsName`, `Emptied`, `AtAnchor`, `OutsideProblem`), `ForMemberIgnorePattern.Find`, `ForMemberMapFromPattern.Find`, `MapCallPattern.GeneratedAsync`, `NestedMaps.AddRewritten`.
- [x] 3. Proofs + unit tests.
- [x] 4. Plan, models, verify.
- [x] 5. `dotnet build`, `dotnet test`, SampleShop tests.
- [x] 6. Task to archive. Queue row, README row, INDEX row and `proofs-doc.py` left to the main session.

## Result

- Proofs (Fixed): `CreateMap/nested-profile`, `nested-record`, `nested-in-nested`, `expression-bodied`, `expression-bodied-base`, `member-list-nested-profile` (was Manual), `ForMemberIgnore/nested-profile`, `ForMemberMapFrom/nested-profile`, `_Combined/nested-profile-map-call`, `_Combined/nested-profiles-nested-map`. Proofs (Manual): `CreateMap/nested-private-dto`, `nested-several-profiles`, `nested-name-taken`, `several-constructors`, `generic`, `partial`, `ForMemberMapFrom/nested-outer-member`, `ForMemberMapFrom/nested-private-member`.
- Unit tests: `src/Unshackle.Tests/Core/ProfileShapesTests.cs` (Find reports the outer type; Map and MapInto calls in another file; nested map to a nested profile rewritten earlier; CRLF for an expression body).
- Test results: see the verify document.
- Open: cats `AddPRI` (two profiles in one type); most cats profiles still stop on `ReverseMap`, `ConstructUsing` or conversions (rows 10–12), now with that reason instead of the shape reason.

## See also

- `claude/tasks/automapper-queue.md` — row 21
- `docs/projectinfo/patterns/automapper-profile-shapes-plan.md`
- `docs/projectinfo/patterns/automapper-profile-shapes-verify.md`
- `claude/tasks/archief/TAAK-246-automapper-member-list.md`
- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`
