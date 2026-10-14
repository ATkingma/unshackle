# TAAK-156 — automapper.for-member-map-from

**Status**: ✅ completed
**Created**: 2026-10-12
**Goal**: row 3 of `claude/tasks/automapper-queue.md`: `.ForMember(d => d.X, o => o.MapFrom(s => expr))` → `X = expr` in the `ToB` method, together with Ignore in the same profile.

## Research

- AutoMapper 14: `MapFrom(Expression<Func<TSource, TMember>>)` is the only overload with a lambda of one parameter; overloads with `(s, d)`, resolvers and a string name exist as well.
- AutoMapper executes MapFrom expressions null-safely (a NullReferenceException gives the default value) and converts types. A plain assignment does neither: that decides what becomes Manual.
- Expression trees do not allow `?.`; an expression with `x != null ? x.Y : ...` is possible.

## Choices

- **Subclass `ForMemberMapFromPattern : CreateMapPattern`** with a shared `Rewrite`, as with TAAK-155 (a profile is replaced as a whole). `Find` splits exclusively: a MapFrom call (of any form) → MapFrom, otherwise Ignore → Ignore, otherwise CreateMap. That way exactly one pattern finds a profile, also with Ignore + MapFrom.
- **Member list `(Name, Value)`**: `source.X` or the rewritten expression, in the order of B.
- **Only `s => expr`** (expression body, one parameter, AutoMapper symbol); `s` is replaced by `source` via the symbol. Other forms: Manual with the existing "not a CreateMap<A, B>() without options".
- **Null safety via the nullable flow**: a receiver of `.member`/`[ ]` may be `s`, a type or an expression with `NullableFlowState.NotNull`; otherwise Manual ("may be null"). Without a nullable context therefore almost always Manual; deliberately, a silent behaviour change is worse.
- **Type**: equal to the type of X, or implicitly numeric/nullable; otherwise Manual (AutoMapper converts). Collection X: Manual.
- **Outside the profile**: `this`, instance fields, statics of the profile, and a name `source` that is already something else: Manual. Local parameters of nested lambdas are allowed.
- X must be publicly writable and configured only once.
- The Ignore proof `map-from` (Manual) was dropped.

## Steps

- [x] 1. Research and choices.
- [x] 2. Plan `docs/projectinfo/patterns/automapper-for-member-map-from-plan.md` (by hand).
- [x] 3. `ForMemberMapFromPattern`, `CreateMapPattern` extended, registration in `AutoMapperFix`.
- [x] 4. Proofs (`simple`, `ignore-and-nullable`, 5x Manual), `ForMemberMapFromPatternTests`, `proofs-doc.py`.
- [x] 5. Models (D2 sources), verify, README row, queue.

## Result

- `dotnet build` 0 warnings (with `-p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507`); 736/771 tests green, ProofTests and pattern tests 568/568. The 35 failures (XmlProjectFileStoreTests, FixerTests, CommandTests, 2 SampleShop integration tests) are the known CRLF failures.

## See also

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-155-automapper-for-member-ignore.md`
- `docs/projectinfo/patterns/automapper-for-member-map-from-plan.md`
- `docs/projectinfo/patterns/automapper-for-member-map-from-verify.md`
