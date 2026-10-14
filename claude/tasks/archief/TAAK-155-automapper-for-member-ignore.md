# TAAK-155 — automapper.for-member-ignore

**Status**: ✅ completed
**Created**: 2026-10-12
**Goal**: row 2 of `claude/tasks/automapper-queue.md`: `.ForMember(d => d.X, o => o.Ignore())` → leave the member out of the `ToB` method.

## Research

- AutoMapper 14: `ForMember(Expression<Func<TDest, TMember>>, Action<IMemberConfigurationExpression<TSrc, TDest, TMember>>)`; also an overload with a string name. `o.Ignore()` leaves the destination member untouched (also if the source has a same-named member).
- Corpus: 1015 hits in 1 project; the SampleShop profiles use `ForMember` with `MapFrom` and `ReverseMap` (stay Manual).

## Choices

- **A subclass instead of a separate pattern with its own `Rewrite`**: a profile is replaced as a whole (`[profile, <Profile>Extensions]`). Two patterns each with a `Rewrite` on the same class overwrite each other. `ForMemberIgnorePattern : CreateMapPattern` therefore shares `Rewrite`; `Find` splits the profiles (with Ignore → this pattern, the rest → `CreateMapPattern`), so exactly one pattern finds a profile. Contracts untouched.
- **Only the lambda form `d => d.X` + `o => o.Ignore()`** with a symbol check on the AutoMapper assembly. String name, nested members, block lambdas, `MapFrom` and `ReverseMap` → Manual with the existing "not a CreateMap<A, B>() without options".
- **An ignored member is skipped before the name/type check**: a member without a source member or with another type is then no problem either.
- **An ignored member that B does not have as public → Manual**: AutoMapper would reject the configuration; nothing sensible to leave out.
- Only `public` members count; `AllOrNothing` and the `Map` Manual stay as in row 1.

## Steps

- [x] 1. Research and choices.
- [x] 2. Plan `docs/projectinfo/patterns/automapper-for-member-ignore-plan.md` (approved, by hand: `kingmadoc` not available).
- [x] 3. `ForMemberIgnorePattern`, `CreateMapPattern` extended, registration in `AutoMapperFix`.
- [x] 4. Proofs (`simple`, `mixed`, 4× Manual), `ForMemberIgnorePatternTests`, `proofs-doc.py`.
- [x] 5. Models (D2 sources; not rendered), verify, README row, queue.

## Result

- `dotnet build` 0 warnings; ProofTests and pattern tests 556/556. Full run 723/759: 36 failures outside this pattern (FixerTests, XmlProjectFileStoreTests, CommandTests, SampleShop integration; line endings and a second NuGet source `afas` in this environment).
- Build and test in this environment need `-p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507` (NU1507 because of two NuGet sources).

## See also

- `claude/tasks/automapper-queue.md`
- `claude/tasks/archief/TAAK-146-automapper-create-map.md`
- `docs/projectinfo/patterns/automapper-for-member-ignore-plan.md`
- `docs/projectinfo/patterns/automapper-for-member-ignore-verify.md`
