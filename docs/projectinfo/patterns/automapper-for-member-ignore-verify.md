# Verification: automapper-for-member-ignore

|               |                                                                                                          |
| ------------- | -------------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-for-member-ignore-plan.md`](automapper-for-member-ignore-plan.md) |
| **Status**    | Verified                                                                                                 |
| **Generated** | 2026-10-12 by KingmaDoc skill (by hand, `kingmadoc` not available)                                  |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ForMemberIgnorePattern.cs`, `CreateMapPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ForMemberIgnore/`. Unittests: `src/Unshackle.Tests/Core/ForMemberIgnorePatternTests.cs`.

| REQ   | Verdict | Evidence                                                                                                                                  |
| ----- | ------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | Met | `simple` (Ignore with and without a source member, two ForMember links), `mixed`, unit test `Rewrite_IgnoredMemberHasSourceMember_LeavesItOut` |
| REQ-2 | Met | `simple`, `mixed`                                                                                                                       |
| REQ-3 | Met | `unmatched`                                                                                                                             |
| REQ-4 | Met | `string-name`, `reverse-map`                                                                                                |
| REQ-5 | Met | unittest `Rewrite_IgnoredMemberNotPublic_ReportsManual`                                                                                 |
| REQ-6 | Met | unittest `Find_ProfileWithOrWithoutIgnore_IsClaimedByOneOfThePatterns`                                                                  |

## Deviations

- No own `Rewrite`: `ForMemberIgnorePattern` inherits it from `CreateMapPattern` (a profile is replaced as a whole, see plan, Design).
- The PNG/SVG of the models are not rendered (no `kingmadoc`/`d2` available); the D2 sources are there.

## Validation results

| Check   | Command                                                       | Result                                                                                                                                                                                              |
| ------- | ------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Build   | `dotnet build -p:WarningsNotAsErrors=NU1507 -p:NoWarn=NU1507` | passed (0 warnings); without those options restore fails in this environment on a second NuGet source (`afas`)                                                                                    |
| Tests   | `dotnet test` (same options)                                  | 723/759 green; ProofTests and pattern tests 556/556 green; the 36 failures (`FixerTests`, `XmlProjectFileStoreTests`, `CommandTests`, SampleShop integration) are outside this pattern (line endings and NuGet source in this Windows environment) |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx`   | _not run_                                                                                                                                                                                           |
| Lint    | _none_                                                        | _not run_: no lint command configured                                                                                                                                                             |

## See also

- [automapper-for-member-ignore-plan.md](automapper-for-member-ignore-plan.md)
- [automapper-for-member-ignore-modellen.md](automapper-for-member-ignore-modellen.md)
