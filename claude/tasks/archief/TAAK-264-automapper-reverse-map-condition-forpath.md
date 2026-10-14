# TAAK-264 — automapper reverse map: Condition and ForPath

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: `Condition`/`PreCondition`/`ForPath` on a `ReverseMap()` (jhipster: 89 blocked Map calls, 23 through `ReverseMap`) are rewritten where that reproduces AutoMapper 14 exactly; the rest stays Manual with a precise reason.

## Context for the next agent

- Before: `CreateMapPattern.TryParseMap` rejected every `Condition`/`ForPath` after `.ReverseMap()` ("a Condition or ForPath on the reverse map is not migrated") and `Rewrite` rejected any `Condition`/`ForPath` of the forward map when a `ReverseMap` followed. TAAK-255 built the machinery (`MemberOptions`, `PathProblem`, `ConditionProblem`); this task points it at the reverse map.
- jhipster has exactly one such shape: `CreateMap<User, UserDto>().ForMember(Roles, MapFrom(...)).ReverseMap().ForPath(user => user.UserRoles, opt => opt.MapFrom(dto => dto.Roles.Select(...).ToHashSet()))`.
- Source `<corpus>/corpus-automapper/src/jhipster` (clone of `jhipster/jhipster-sample-app-dotnetcore` main = `7a799c7babb4`, as in the survey); reports `<corpus>/corpus-automapper/_reports/jhipster-264-before.json` and `jhipster-264.json`.

## Evidence (AutoMapper 14.0.0, scratch console project, fields without nullable context)

| Case                                                                                                                                  | Result                                                                                                                        |
| ------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| `ReverseMap().ForMember(d => d.Name, o => o.Condition(s => s.Flag))`, `Name` has initializer `"init"`                                 | flag true: value; flag false: `"init"` (like the forward map); a reversed `MapFrom` (`X = s.Y`) in the same map still applies |
| same on a member without initializer (`int`)                                                                                          | false: `0` = `default`                                                                                                        |
| `Condition` on a member that the reversed `MapFrom` fills (`ForMember(Count, MapFrom(s => s.N))` forward, `Condition` on `N` reverse) | true: `N = 5`, false: `0`: `N = cond ? source.Count : default`                                                                |
| `ReverseMap().ForMember(d => d.Name, o => o.PreCondition(s => s.Flag))`                                                               | same as `Condition`                                                                                                           |
| `ReverseMap().ForMember(d => d.Name, o => { o.Condition(s => s.Flag); o.MapFrom(s => s.Other); })`                                    | false: null, true: `Other`                                                                                                    |
| `ReverseMap().ForPath(d => d.Name, o => o.MapFrom(s => s.Other))` while the source also has `Name`                                    | `Other` wins: a single-member ForPath is a `MapFrom`                                                                          |
| `ReverseMap().ForPath(d => d.Inner.Name, o => o.MapFrom(s => s.Title))`                                                               | `Inner` created, `Name = Title`; the forward map unaffected                                                                   |
| same while the source has a member `Inner` as well                                                                                    | the ForPath wins over the same-named member (so `PathProblem`'s "AutoMapper also maps A itself" check stays: Manual)          |
| `ReverseMap().ForPath(d => d.Items, o => o.MapFrom(s => s.Names.Select(...).ToHashSet()))`, `Names` null                              | no exception: `Items` is an empty set (not null), AutoMapper maps the null result as an empty collection                      |
| forward `ForPath(d => d.Inner.Name, MapFrom(s => s.Title))` + plain `ReverseMap()`                                                    | **AutoMapper reverses the ForPath**: reverse gives `Title = source.Inner.Name` (null when `Inner` is null)                    |
| forward `Condition(s => s.Flag)` on `Name` + plain `ReverseMap()`                                                                     | forward conditional, reverse **unconditional** (a `Condition` is not carried over)                                            |
| forward `{ Condition(...); MapFrom(s => s.X); }` + `ReverseMap()`                                                                     | forward conditional; reverse `X = source.Y` unconditional                                                                     |
| `ReverseMap().ForPath(d => d.Inner.Name, ...).ForMember(d => d.Count, o => o.Condition(...))`                                         | both apply independently                                                                                                      |

## Choices

- **Condition/PreCondition on the reverse map → `X = cond ? value : default`**, with the same rules as TAAK-255 (member without initializer, no declared constructor, `new()`, condition passes `MapFromProblem`). Reason: the measured behaviour on the reverse map equals the forward one, also over a reversed `MapFrom`. Implementation: `TryParseMap` now returns the reverse options in a second `MemberOptions` (`reverseExtra`), `Reverse` hands it to `Problem(extra:)`.
- **ForPath on the reverse map → MapFrom (one member) or `A = new() { B = ... }`** via the existing `PathProblem` with the reverse source/destination. Deeper than two members stays Manual (TAAK-255).
- **A forward `Condition` next to `ReverseMap()` is now allowed** (reverse is unconditional, measured); the forward map is rewritten as before.
- **A forward `ForPath` next to `ReverseMap()` stays Manual**, with the new reason "ReverseMap reverses the ForPath of the forward map": AutoMapper writes `T = source.A.B` in reverse (unflattening, null-tolerant); reproducing it needs null handling that was not measured further. Also covers a single-member forward `ForPath` (it is a `MapFrom` that AutoMapper reverses the same way, but the shared check is by `HasForPath`; conservative).
- **ForPath into a member the reversed `MapFrom` of the forward map also fills** → Manual (`Problem` would let the path win silently; which one AutoMapper uses was not measured).
- **MapInto** (`Map(dto, order)` on the reverse map) with a Condition/ForPath of the reverse map → Manual (existing rule, now also checked in `Reverse`): the existing value or object is kept.
- **jhipster is not Fixed by this**: the profile is now blocked by the next, legitimate reason: the forward `MapFrom` `user.UserRoles.Select(iur => iur.Role.Name).ToHashSet()` dereferences a maybe-null collection (AutoMapper gives an empty set, a plain assignment throws; the elements' `Role` can be null as well). Writing it as `?.` ... `?? []` is not provably the same (null `Role` inside the lambda makes AutoMapper drop the whole value, a `?.` chain would give a null element), so it stays Manual with that reason.

## Steps

- [x] 1. Research: clone jhipster, find the shape, measure AutoMapper 14 (table above).
- [x] 2. `CreateMapPattern`: `TryParseMap` (out `reverseExtra`), `Rewrite` guards, `Reverse(extra)`, `OutsideProblem` of the reverse lambdas.
- [x] 3. Proofs and unit tests.
- [x] 4. jhipster scan before/after; docs (plan, verify, models of `automapper.member-options`, reverse-map verify/plan notes).
- [x] 5. `dotnet build`, `dotnet test`, SampleShop.

## Result

- Scan jhipster (`--fix automapper`): before 0 Fixed / 126 Manual (all "a Condition or ForPath on the reverse map is not migrated"), after 0 Fixed / 126 Manual (all now: the forward `MapFrom` of `Roles` may dereference null). Same count, the reason names the real blocker; the reverse `ForPath` itself passes.
- Proofs Fixed: `CreateMap/reverse-condition`, `CreateMap/reverse-for-path-member`, `CreateMap/forward-condition-reverse-map`, `ForMemberMapFrom/reverse-pre-condition-map-from`, `ForMemberIgnore/reverse-for-path`. Manual: `CreateMap/forward-for-path-reverse-map`, `CreateMap/reverse-condition-initializer`; `CreateMap/reverse-map-options` (jhipster shape with a `HashSet<Role>`) now stops at the collection copy.
- Unit tests: `CreateMapPatternTests.Rewrite_ConditionOrForPathOnReverseMapAutoMapperDoesMoreFor_ReportsManual` (4 cases), `MapIntoPatternTests.FixAsync_ReverseMapConditionWithMapInto_ReportsManual`; `Rewrite_MemberOptionAutoMapperDoesMoreFor_ReportsManual` case for ReverseMap changed to forward ForPath.
- `dotnet test`: 1442/1442; SampleShop: 38/38; build 0 warnings.
- Open: a null-safe form of the jhipster `MapFrom` (collection of a maybe-null source), forward `ForPath` with `ReverseMap` (reversal measured: `T = source.A.B`), `NullSubstitute`.

## Changed files

- `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs`
- `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`, `src/Unshackle.Tests/Core/MapIntoPatternTests.cs`
- `src/Unshackle.Tests/Proofs/AutoMapper/{CreateMap,ForMemberMapFrom,ForMemberIgnore}/` (new proofs as above, `reverse-map-options.manual.txt`)
- `docs/projectinfo/patterns/automapper-member-options-{plan,modellen,verify}.md`, `img/automapper-member-options-modellen-1.d2`, `automapper-reverse-map-{plan,verify}.md`, `docs/projectinfo/architectuur/proofs.md`
- `claude/docs/INDEX.md`

## See also

- `claude/tasks/archief/TAAK-255-automapper-member-options.md`
- `claude/tasks/archief/TAAK-249-automapper-reverse-map.md`
- `claude/tasks/archief/TAAK-260-automapper-profile-cascade.md`
- `docs/projectinfo/patterns/automapper-member-options-plan.md`
