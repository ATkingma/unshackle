# Analysis: FluentAssertions → Shouldly to 100%?

Measured 2026-10-11 on `main` 194ea0f. Analysis only; no code changed.

## Short answer

**No, 100% is not possible.** The realistic ceiling is **~97–98% Fixed**. We are currently at **92.5%**.

- The last ~2–3% are the user's own assertion extensions (e.g. `HaveSingleMethodBody` in Mapperly and `BeFailure` in FluentResults). `BeEquivalentTo` also belongs here when it compares something Shouldly has nothing for: options, cyclic graphs, different types or runtime types. Here an automatic rewrite weakens the test or breaks it. Under our own rule ("a pattern never throws; when in doubt → Manual") these stay Manual.
- Of the rest, roughly two thirds is achievable without a semantic difference. Most of it falls into three cheap actions: `BeEquivalentTo` on collections with `ignoreOrder`, `BeSameAs` and `NotThrowAsync`.

## Method

- **A (recognition)**: every `.Should().X` counted across lines (`grep -Pzo`) in the 14 corpus tarballs (38,950 calls). These were compared with the FA method names that the patterns in `src/Unshackle.Core/Fixes/FluentAssertions/` recognize.
- **B (Fixed ratio)**: `dotnet restore` + `unshackle scan <sln> --fix fluentassertions --report …json` on 10 solutions. Reports are in `scratchpad/fa100/`.
- Shouldly targets checked by method name in `~/.nuget/packages/shouldly/4.3.0/lib/net8.0/Shouldly.dll` (metadata grep, no reflection on overloads).

## A. Recognition: FA APIs without a pattern

Recognized: **94.9%** (36,948 / 38,950). `corpus.md` still said 90.5%; that figure predates TAAK-038/039/050/051/052. **2,002 calls** remain:

| API                                                                                                      | count | #proj | cat | Shouldly equivalent / alternative                                                      |
| -------------------------------------------------------------------------------------------------------- | ----: | ----: | :-: | -------------------------------------------------------------------------------------- |
| `BeSameAs`                                                                                               |   482 |     9 |  1  | `ShouldBeSameAs` ✓                                                                     |
| `HaveSingleMethodBody` (Mapperly)                                                                        |   433 |     1 |  3  | custom                                                                                 |
| `NotThrowAsync`                                                                                          |   150 |     4 |  1  | `await Should.NotThrowAsync(act)` ✓                                                    |
| `HaveMapMethodBody` / `HaveDiagnostic` / `BeJson` (Mapperly)                                             |   409 |     1 |  3  | custom                                                                                 |
| `ContainInOrder`                                                                                         |    67 |     4 |  3  | no equivalent → Manual or shim                                                         |
| `NotBeSameAs`                                                                                            |    54 |     8 |  1  | `ShouldNotBeSameAs` ✓                                                                  |
| `HaveElementAt`                                                                                          |    36 |     1 |  1  | `x.ElementAt(i).ShouldBe(v)`                                                           |
| `BeFailure` / `BeSuccess` / `HaveValue` (FluentResults' own)                                             |    45 |     1 |  3  | custom                                                                                 |
| `BeOnOrAfter` / `BeOnOrBefore` / `BeAfter` / `BeBefore`                                                  |    54 |     2 |  1  | `ShouldBeGreaterThanOrEqualTo` / `…LessThanOrEqualTo` / `…GreaterThan` / `…LessThan` ✓ |
| `HaveSameCount`                                                                                          |    21 |     1 |  1  | `x.Count().ShouldBe(y.Count())`                                                        |
| `ContainEquivalentOf`                                                                                    |    22 |     5 |  2  | value-equality type → `ShouldContain(item)`; otherwise 3                               |
| `MatchRegex` / `NotMatchRegex`                                                                           |    19 |     3 |  1  | `ShouldMatch` / `ShouldNotMatch` ✓                                                     |
| `Match` (wildcard)                                                                                       |    14 |     4 |  1  | constant → regex + `ShouldMatch`; non-constant → 3                                     |
| `CompleteWithinAsync`                                                                                    |    10 |     1 |  2  | `Should.CompleteIn` ✓ (blocks synchronously, different timeout message)                |
| `HaveCountGreaterThan` / `…OrEqualTo` / `…LessOrEqualTo`                                                 |    10 |     3 |  1  | `x.Count().ShouldBeGreaterThan(n)`                                                     |
| `BeOneOf`                                                                                                |     9 |     3 |  1  | `ShouldBeOneOf` ✓                                                                      |
| `BeSubsetOf`                                                                                             |     8 |     2 |  1  | `ShouldBeSubsetOf` ✓                                                                   |
| `OnlyHaveUniqueItems`                                                                                    |     7 |     1 |  1  | `ShouldBeUnique` ✓                                                                     |
| `HaveValue` / `NotHaveValue` (Nullable)                                                                  |    11 |     2 |  1  | `ShouldNotBeNull` / `ShouldBeNull`                                                     |
| `BeInAscendingOrder` / `BeInDescendingOrder`                                                             |     6 |     1 |  1  | `ShouldBeInOrder(SortDirection…)` ✓; with comparer → 3                                 |
| `Satisfy` / `SatisfyRespectively`                                                                        |     5 |     2 |  1  | inline the lambda body on the subject                                                  |
| `AllBeEquivalentTo`                                                                                      |     4 |     2 |  2  | `ShouldAllBe(i => i == v)` only for value equality                                     |
| `HaveFlag` / `NotHaveFlag`                                                                               |     4 |     1 |  1  | `ShouldHaveFlag` / `ShouldNotHaveFlag` ✓                                               |
| `AllBeOfType` / `AllBeAssignableTo`                                                                      |     4 |     2 |  1  | `ShouldAllBe(i => i.GetType() == typeof(T))` / `i is T`                                |
| `NotIntersectWith` / `NotContainAny`                                                                     |     4 |     2 |  1  | `x.Intersect(y).ShouldBeEmpty()`                                                       |
| `Equals`                                                                                                 |     2 |     1 |  3  | `object.Equals` on `ObjectAssertions` is a bug in the test → Manual                    |
| other custom (git warnings, NetArchTest `NotHaveDependencyOn`, `HaveNameEndingWith`, `NotBePublic`, …)   |  ~150 |   1–2 |  3  | custom                                                                                 |

Recognition summary (corpus, 38,950 calls):

|                                                     |  calls |     % |
| --------------------------------------------------- | -----: | ----: |
| recognized now                                      | 36,948 | 94.9% |
| plus cat 1                                          |   +898 | 97.2% |
| plus cat 2                                          |    +36 | 97.3% |
| cat 3 (custom 999, `ContainInOrder` 67, `Equals` 2) |  1,068 |  2.7% |

Mapperly alone accounts for 842 of the 999 custom calls. Without Mapperly the recognition ceiling is around 99%.

## B. Fixed ratio

| Project                                           | loaded  |     Fixed | Manual* | not recognized |   Fixed % |
| ------------------------------------------------- | ------- | --------: | ------: | -------------: | --------: |
| CSharpFunctionalExtensions                        | yes     |     4,448 |      52 |             36 |     98.1% |
| FluentResults                                     | yes     |       567 |      61 |             50 |     83.6% |
| Ardalis.Result                                    | yes     |       275 |      44 |             17 |     81.8% |
| PowerApps-Tooling (case study)                    | yes     |        29 |     109 |             45 |     15.8% |
| SampleShop                                        | yes     |        62 |      22 |              5 |     69.7% |
| **total (5)**                                     |         | **5,381** | **285** |        **153** | **92.5%** |
| SmartEnum, Specification, Bogus, Polly, EventFlow | **no**  |           |         |                |           |

\* excluding `project-file` findings (14; they follow from other Manuals).
Denominator = Fixed + Manual + unrecognized calls. An `.And` chain counts as one finding, so the figures are ±1%.

**Load errors (finding separate from patterns)**: 5 of the 10 solutions do not load. For Polly, EventFlow and SmartEnum this is caused by NuGet audit (`NU1902`/`NU1903`, vulnerable package, "Warning As Error"). Bogus fails on `NU1701` (net4x package in a net9 project). Specification is missing a project in the tarball, and SmartEnum also has "no Compile target". `dotnet build` succeeds on Polly and EventFlow, but our loader aborts on these warnings. For real users that is worse than any Manual. See action 0.

### Manual reasons (285)

| Reason                                                                                                                                                                         | count | pattern                           |                   cat                    | Best alternative                                                                                                                                          |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----: | --------------------------------- | :--------------------------------------: | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `BeEquivalentTo(params)` / collection: order ignored                                                                                                                           |    93 | be-equivalent-to-object           | 1 (strings/ints/enums/records), 3 (rest) | `x.ShouldBe(expected, ignoreOrder: true)` ✓ (multiset, same as FA). Measured: almost all `string`/`int`.                                                  |
| Inside `AssertionScope` (be-true-false 24, should-be 6, have-count 1, be-greater-less 1) + the scope itself 17                                                                 |    49 | assertion-scope etc.              |                    2                     | entire `using` block → `ShouldSatisfyAllConditions(() => …, …)` ✓. Warning: non-assertion code in the block ends up inside the lambdas.                  |
| Result used (`.Which`/`.And`/value): ContainSingle 9, Throw/Invoking 11, Be 4, BeOfType 4, NotBeNull 2, NotBeEmpty 2, BeEquivalentTo 2, HaveCount/Contain/ContainKey 3       |    37 | various                           |                    1                     | queue #19 `fa.which-chain`: split the statement, value-returning Shouldly call (`ShouldBeOfType`, `ShouldHaveSingleItem`, `Should.Throw`) into a local    |
| Non-plain type (object/interface/BCL)                                                                                                                                          |    14 | be-equivalent-to-object           |                    3                     | Manual (runtime members determine the comparison)                                                                                                         |
| Cyclic graph                                                                                                                                                                   |    12 | be-equivalent-to-object           |                    3                     | Manual; Shouldly is more lenient here                                                                                                                     |
| Different types / anonymous expected                                                                                                                                           |    12 | be-equivalent-to-object           |                    3                     | Manual with advice: `ShouldBe` per property. A shim is possible, but then you are rebuilding FA.                                                          |
| becauseArgs / named args                                                                                                                                                       |    13 | should-be, have-count, be-null, … |                    1                     | queue #22: `string.Format(because, args)`                                                                                                                 |
| Not in the form `x.Should().X` (after `.And`), `.And` with unknown X, subject with side effects                                                                                |    12 | and-chain, all-satisfy, …         |                    1                     | subject first into `var tmp = …;` (evaluates once, same as FA); then follow the other patterns                                                            |
| `BeNullOrEmpty` / `NotBeNullOrEmpty` on collection                                                                                                                             |    11 | string-start-end                  |                    1                     | `x?.ShouldBeEmpty();` / `x.ShouldNotBeNull(); x.ShouldNotBeEmpty();`                                                                                      |
| `Contain(IEnumerable)`                                                                                                                                                         |     6 | contain                           |                    2                     | `expected.ShouldBeSubsetOf(x)` ✓ (same semantics, message reversed)                                                                                       |
| `WithMessage` non-constant                                                                                                                                                     |     4 | throw                             |                    3                     | Manual; possibly a runtime wildcard helper (shim)                                                                                                         |
| Invoking receiver with side effects                                                                                                                                            |     4 | invoking                          |                    1                     | receiver first into a local (FA also evaluates it once)                                                                                                   |
| Expected type ≠ subject type `double?`/`double`                                                                                                                                |     3 | should-be                         |                    1                     | cast the subject: `((double?)x).ShouldBe(y)`                                                                                                              |
| Dictionary `HaveCount`                                                                                                                                                         |     3 | have-count                        |                    1                     | `dict.Count.ShouldBe(n)` (FA counts pairs, so identical)                                                                                                  |
| Throw subject not an Action/Func\<ref\>                                                                                                                                        |     3 | throw, throw-async                |                    1                     | wrap in a lambda: `() => { _ = f(); }`                                                                                                                    |
| `BeEquivalentTo(…, options)`                                                                                                                                                   |     3 | be-equivalent-to-object           |                    3                     | Manual per option (queue #32)                                                                                                                             |
| `Task<Result>` compared with `Result`                                                                                                                                          |     2 | not-be                            |                    3                     | test bug: Manual                                                                                                                                          |
| `FluentAssertions.Types` `Be`                                                                                                                                                  |     1 | should-be                         |                    3                     | Manual                                                                                                                                                    |
| Other (unknown type, async chains)                                                                                                                                             |     4 |                                   |                   1/3                    |                                                                                                                                                           |

Tally: **cat 1 ≈ 178**, **cat 2 ≈ 55**, **cat 3 ≈ 52**.

The 153 unrecognized calls in these five projects:

- cat 1 ≈ 55: `BeSameAs` 37, `Match` 7, `HaveSameCount` 5, `Satisfy` 4, `NotThrowAsync` 2
- cat 2 ≈ 19: `ContainEquivalentOf` 16, `AllBeEquivalentTo` 3
- cat 3 ≈ 79: custom from FluentResults and PowerApps (`BeFailure`, `HaveEntry`, `HaveValueEqual`, `ContainName`, …)

### Ceiling

|                            |         Fixed |         % |
| -------------------------- | ------------: | --------: |
| current                    | 5,381 / 5,819 | **92.5%** |
| + cat 1                    |         5,614 |     96.5% |
| + cat 2 (with warning)     |         5,688 | **97.7%** |
| cat 3 stays Manual         |           131 |      2.3% |

This varies greatly per project. CFE goes to ~99.5%. PowerApps stays around 75%, because the case study has a lot of AssertionScope and its own extensions.

## Category 3: what then?

| Case                                                                                        | Advice                                                                                                                                                                                                                                                                                                                                                            |
| ------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Own extensions (`ReferenceTypeAssertions` subclasses, `HaveSingleMethodBody`, `BeFailure`)  | **Manual** with the reason "custom FA-assertion `X` in `<bestand>`: herschrijf de extensie naar Shouldly; de aanroepen kunnen blijven" (queue #34). No shim: the body uses `Execute.Assertion`, which Unshackle cannot translate generically. What is cheap: the report groups by extension. Then it is one manual fix per extension instead of hundreds.          |
| `ContainInOrder`                                                                            | Candidate for a **shim**: a generated `ShouldContainInOrder` extension (±15 lines, subsequence check) in the test project. That is semantically exact, but it is code Unshackle puts into the user's repo. Advice: Manual first with concrete code in the reason, and only a shim if users ask for it.                                                            |
| `BeEquivalentTo` with options / cyclic / different type / runtime type                      | **Manual**. Shouldly's `ShouldBeEquivalentTo` is more lenient here, or stricter in a different way, and a shim would rebuild FA's equivalency engine.                                                                                                                                                                                                              |
| `WithMessage` with non-constant pattern                                                     | Manual. A shim (wildcard → regex at runtime) is possible, but yields little (4×).                                                                                                                                                                                                                                                                                 |
| `Equals` on assertions, `Task` vs value                                                     | Manual: it is a bug in the test, report that.                                                                                                                                                                                                                                                                                                                     |

## Top 10 actions (gain per effort)

Gain = calls that move from Manual or unrecognized to Fixed (B = 5 projects, A = corpus).

| #   | Action                                                                                                                                    | gain                                    | effort                                       | cat |
| --- | ----------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------- | -------------------------------------------- | :-: |
| 0   | **Loader**: do not treat NuGet warnings (`NU1701`/`NU190x`) as a fatal load error                                                         | 5 of 10 corpus solutions currently fail to load | S                                    |  —  |
| 1   | `BeEquivalentTo` on collection with value-equality elements → `ShouldBe(exp, ignoreOrder: true)`                                          | B 93 (1.6 pp)                           | S (extension)                                |  1  |
| 2   | `fa.be-same-as` (+ `NotBeSameAs`)                                                                                                         | A 536, B 37                             | XS                                           |  1  |
| 3   | `fa.not-throw-async`                                                                                                                      | A 150                                   | S (copy of not-throw)                        |  1  |
| 4   | Comparisons: `BeOnOrAfter`/`Before`/`After`/`Before`, `HaveCountGreaterThan…`, `HaveSameCount`, `HaveElementAt`                           | A 136                                   | XS (mapping in be-greater-less / have-count) |  1  |
| 5   | `fa.which-chain` (#19): result via local                                                                                                  | B 37                                    | M                                            |  1  |
| 6   | `fa.because-args` (#22)                                                                                                                   | B 13 (+ in every pattern)               | S                                            |  1  |
| 7   | Hoist subject or receiver with side effects into `var tmp` (and-chain, invoking, all-satisfy, "not in the form")                          | B 16                                    | S–M                                          |  1  |
| 8   | `fa.match` (#31): `MatchRegex` + constant wildcard `Match`                                                                                | A 33                                    | S                                            |  1  |
| 9   | `AssertionScope` → `ShouldSatisfyAllConditions`                                                                                           | B 49 (mostly PowerApps)                 | M–L (block rewrite)                          |  2  |
| 10  | Small set: `BeOneOf`, `BeSubsetOf`, `OnlyHaveUniqueItems`, `HaveValue`, `HaveFlag`, `BeNullOrEmpty` on collection, `Contain(IEnumerable)` | A ~60, B ~17                            | XS each                                      | 1/2 |

Actions 1–4 together gain ~3 percentage points of the ~5 achievable, for little work.

## After actions 1–4 (TAAK-092 through 095)

Built: `BeEquivalentTo` on collections of leaves → `ShouldBe(…, ignoreOrder: true)` (extension of `fa.be-equivalent-to-object`),
`fa.be-same-as`, `fa.not-throw-async`, and date/count comparisons in `fa.be-greater-less` and `fa.have-count`.
Scan (`unshackle scan --fix fluentassertions`) with main bcf1156 (before) and this change (after):

| Project                                      | Fixed before | Manual before | Fixed after | Manual after | Difference                                  |
| -------------------------------------------- | -----------: | ------------: | ----------: | -----------: | ------------------------------------------- |
| SampleShop                                   |           62 |            24 |          62 |           24 | none: uses none of these APIs               |
| PowerApps-Tooling (case study, after restore) |          29 |           109 |          33 |          109 | +4 `fa.be-same-as` (were not recognized)    |

Manual here includes `project-file` findings (SampleShop 2, PowerApps 3). The corpus gain (A) is mostly in projects
that do not load here (action 0).

## After actions 5–10 (TAAK-096 through 101)

Built: `.Which`/`.Subject` after BeOfType/ContainSingle/Throw (096), becauseArgs centrally as `string.Format(InvariantCulture, …)` (097),
subject with side effects → `var subject = …;` in and-chain/only-contain/all-satisfy (098), `fa.match` (099),
`AssertionScope` with only assertions → `ShouldSatisfyAllConditions` (100), `fa.direct-mapping` + ContainInOrder Manual with code (101).
Scan with main 0079d44 (before, = "after" above) and this change (after), Manual including `project-file`/`usings`:

| Project                                       | Fixed before | Manual before | Fixed after | Manual after |
| --------------------------------------------- | -----------: | ------------: | ----------: | -----------: |
| SampleShop                                    |           62 |            24 |          69 |           16 |
| PowerApps-Tooling (case study, after restore) |           33 |           109 |          55 |           93 |

Not done (stays Manual): `ThrowAsync(..).Which`, `.WithMessage(..).Which`, `ContainKey(..).WhoseValue`, hoisting the Invoking receiver,
AssertionScope with a context argument or other statements, named arguments.

## See also

- `docs/projectinfo/architectuur/corpus.md` — source A; the percentage there (90.5%) is outdated
- `claude/tasks/patroon-queue.md` — #19, #22, #31–#34 and the unnumbered rows correspond to the actions above
- `docs/projectinfo/architectuur/proofs.md`
