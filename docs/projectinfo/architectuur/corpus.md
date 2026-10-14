# Test corpus: FluentAssertions in real projects

Which FA APIs real open-source test code uses, how often, and whether a pattern covers them. Drives the priority of
`claude/tasks/patroon-queue.md`. Measured on 2026-10-11 with tarballs from GitHub (grep only; two projects also
built and fixed, see below). Count: every `.Should().X` (also across lines), per X.

## Projects

| Project                      | Kind                                | FA   | Files     |  Calls | Covered |
| ---------------------------- | ----------------------------------- | ---- | --------: | -----: | ------: |
| Akka.NET                     | actor framework, large              | 5.10 |       711 | 10 300 |     94% |
| LiteDB                       | embedded database                   | 6.12 |       540 |  7 915 |     86% |
| Steeltoe                     | cloud-native libs (Spring-like)     | 7.2  |       265 |  5 751 |     94% |
| Polly (tag 7.2.4)            | resilience lib                      | 6.11 |        81 |  5 326 |     94% |
| CSharpFunctionalExtensions\* | Result/Maybe                        | 5.10 |       248 |  4 548 |     99% |
| Bogus                        | fake-data generator                 | 6.12 |       104 |    945 |     87% |
| Ardalis.Specification\*      | EF specifications                   | 7.2  |        85 |    889 |     85% |
| Mapperly (tag v3.6.0)        | source generator, custom assertions | 6.12 |        85 |    837 |     14% |
| FluentResults                | Result lib + own FA extensions      | 6.7  |        24 |    675 |     93% |
| EventFlow\* (tag v0.83.4713) | CQRS/ES                             | 5.6  |        91 |    518 |     96% |
| Ardalis.SmartEnum\*          | enums                               | 6.12 |        52 |    419 |     78% |
| NUKE\*                       | build tool                          | 6.12 |        33 |    367 |     87% |
| Ardalis.Result               | Result lib + own FA extensions      | 7.0  |        17 |    335 |     95% |
| modular-monolith-with-ddd    | DDD sample app (NUnit)              | 6.12 |        29 |    125 |     50% |

\* already used in `edge-cases.md`. EventFlow main and Refit no longer use FA (Shouldly); Refit is left out.
Dropped (no FA ≤ 7 in HEAD): Polly main, Nethermind, Dapr, Jellyfin, IdentityServer4, Orleans (AwesomeAssertions),
ErrorOr (FA 8), ardalis/CleanArchitecture.

**Total**: 14 projects, 38,950 calls, **94.9%** (36,948) recognised by API by an existing pattern (Fixed or deliberate
Manual), measured after TAAK-038/039/050/051/052 (`claude/tasks/ANALYSE-fa-100.md`; previously 90.5%). The column "Covered" per
project above is still the old measurement. Ceiling ~97%: the rest is mainly own assertion extensions (Mapperly only 842 calls).
Low coverage for Mapperly and modular-monolith comes from own assertion extensions (`HaveSingleMethodBody`,
`HaveDiagnostic`, `BeJson`; row #34).

## Frequency (≥ 3 projects or ≥ 100 calls)

| API                     |  count | #projects | covered      |
| ----------------------- | -----: | --------: | ------------ |
| `Be`                    | 17 444 |        14 | yes          |
| `BeTrue`                |  3 691 |        14 | yes          |
| `BeFalse`               |  2 225 |        13 | yes          |
| `Throw`                 |  1 871 |        11 | yes          |
| `BeEquivalentTo`        |  1 865 |        13 | yes          |
| `Equal`                 |  1 520 |         7 | yes          |
| `Contain`               |    953 |        10 | yes          |
| `BeOfType`              |    862 |        12 | yes          |
| `BeNull`                |    846 |        14 | yes          |
| `BeEmpty`               |    802 |        11 | yes          |
| `NotBeNull`             |    784 |        11 | yes          |
| `HaveCount`             |    595 |        12 | yes          |
| `ThrowAsync`            |    561 |         5 | yes          |
| `BeSameAs`              |    482 |         9 | yes          |
| `ContainSingle`         |    453 |         8 | yes          |
| `HaveSingleMethodBody`  |    433 |         1 | no (custom)  |
| `NotThrow`              |    379 |        10 | yes          |
| `ContainKey`            |    342 |         6 | yes          |
| `BeGreaterThan`         |    269 |         7 | yes          |
| `NotBe`                 |    216 |         8 | yes          |
| `NotContain`            |    179 |         6 | yes          |
| `NotThrowAsync`         |    150 |         4 | yes          |
| `HaveMapMethodBody`     |    139 |         1 | no (custom)  |
| `HaveDiagnostic`        |    138 |         1 | no (custom)  |
| `BeJson`                |    132 |         1 | no (custom)  |
| `OnlyContain`           |    131 |         6 | yes          |
| `StartWith`             |    105 |         7 | yes          |
| `NotBeEmpty`            |     98 |         7 | yes          |
| `ThrowExactly`          |     75 |         5 | yes          |
| `BeLessThan`            |     72 |         7 | yes          |
| `BeGreaterOrEqualTo`    |     68 |         5 | yes          |
| `NotBeNullOrEmpty`      |     67 |         8 | yes          |
| `ContainInOrder`        |     67 |         4 | Manual+code  |
| `BeInRange`             |     60 |         4 | yes          |
| `NotBeSameAs`           |     54 |         8 | yes          |
| `BeAssignableTo`        |     52 |         5 | yes          |
| `BePositive`            |     23 |         5 | yes          |
| `BeLessOrEqualTo`       |     23 |         4 | yes          |
| `ContainEquivalentOf`   |     22 |         5 | open #33     |
| `EndWith`               |     21 |         5 | yes          |
| `NotBeNullOrWhiteSpace` |     21 |         3 | yes          |
| `MatchRegex`            |     18 |         3 | yes          |
| `BeNullOrEmpty`         |     18 |         4 | yes          |
| `BeCloseTo`             |     16 |         5 | yes          |
| `Match`                 |     14 |         4 | yes          |
| `HaveLength`            |     13 |         3 | yes          |
| `NotEqual`              |     11 |         3 | yes          |
| `NotBeOfType`           |      9 |         3 | yes          |
| `BeOneOf`               |      9 |         3 | yes          |
| `HaveCountGreaterThan`  |      4 |         3 | yes          |

Chains after an assertion: `.And` 726, `.Where` 558 (mostly `Throw<T>().Where(...)`), `.WithMessage` 460, `.Which` 301,
`.WhoseValue` 257, `.Subject` 184 (63× after `ContainSingle`, 47× after `BeOfType`), `.WithInnerException` 46.
"Covered = yes" means: the pattern recognises the API; depending on type and chain it can become Manual.

## Fix run on two projects

Method: `dotnet restore` + baseline build/test (net8.0) → `unshackle fix <sln> --fix fluentassertions --force` →
build/test. Tests in the folder with their own `global.json` (VSTest instead of MTP).

| Project        | Baseline         | Fixed | Manual | Build errors after fix           | Regressions after fix    |
| -------------- | ---------------- | ----: | -----: | -------------------------------- | ------------------------ |
| FluentResults  | 288 green        |   567 |     64 | none                             | 0 (TAAK-070 solved)      |
| Ardalis.Result | 373 green (\*\*) |   281 |     47 | CS0246 in 2 files (TAAK-073)     | none (after manual fix)  |

(\*\*) `SampleWeb` net6.0 already does not build in the baseline (no .NET 6 runtime for the OpenAPI tool); not counted.

Manual reasons (111 together): `BeEquivalentTo` with "becauseArgs" 50 (wrongly, TAAK-071), runtime/BCL type 14,
cyclic graph 12, different types 10, `project-file` 6, `WithMessage` non-constant 4, `AssertionScope` 4,
collection order 3, rest ≤ 2.

Findings:

- **Regression** (TAAK-070): `Action act = () => x.Should().Be(y); act.Should().Throw<XunitException>();` — the inner
  assertion becomes Shouldly and throws another exception type. Solved: assertion and Throw Manual if T is not `Exception`/`ShouldAssertException` (FluentResults: 0 regressions).
- **Wrong Manual reason** (TAAK-071): `coll.Should().BeEquivalentTo("a", "b")` (params overload) was called "becauseArgs". Solved: collection reason.
- **Build error** (TAAK-073): a file with only an unused `using FluentAssertions;` keeps that using while the
  package disappears from the project. Solved: the Fixer removes that using also without findings.
- **Loading**: a solution with a non-restored `net48` project fails as a whole ("reference assemblies for
  .NETFramework 4.8 not found"). After `dotnet restore` it works. Candidate for a clearer message in the CLI.

## Fixed ratio (scan, `claude/tasks/ANALYSE-fa-100.md`)

Recognised is not the same as Fixed. Scan with `--fix fluentassertions` on the solutions that load; Fixed % = Fixed / (Fixed + Manual + not recognised):

| Project                    |     Fixed |  Manual | not recognised |   Fixed % |
| -------------------------- | --------: | ------: | -------------: | --------: |
| CSharpFunctionalExtensions |     4 448 |      52 |             36 |     98.1% |
| FluentResults              |       567 |      61 |             50 |     83.6% |
| Ardalis.Result             |       275 |      44 |             17 |     81.8% |
| PowerApps-Tooling (case)   |        29 |     109 |             45 |     15.8% |
| SampleShop                 |        62 |      22 |              5 |     69.7% |
| **total**                  | **5 381** | **285** |        **153** | **92.5%** |

After TAAK-092..101 (which chains, becauseArgs, hoisting, `fa.match`, AssertionScope → `ShouldSatisfyAllConditions`,
direct mappings): SampleShop 62 → **69** Fixed (Manual 24 → 16), PowerApps 29 → **55** Fixed (Manual 109 → 93).
The other projects were not scanned again afterwards. Numbers taken over from the source; the Manual column adds up to 288, not 285 (discrepancy in the source).

## Corpus run (fix + build + test)

FA (`claude/tasks/CORPUSRUN-fa.md`, main 40cf91e): of 15 solutions 9 could run; of those, 2 built after `fix`
(SmartEnum, FluentResults). Four broke because the code was rewritten while the project file could not be changed
(TAAK-111/134, solved in 2962f95); further TAAK-110 (CSFE), TAAK-112 (Bogus does not load), TAAK-113 (PowerApps).
Later rounds (3–5, per-test regression harness, 16 solutions) are in `claude/tasks/CORPUSRUN-fa.md`; round 4: 16/16 build after the fix.

MediatR (`claude/tasks/CORPUSRUN-mediatr.md`): first run 0 of 9 fully migrated, mainly through project-file blockers
(TAAK-134, now textual splicing) and TAAK-130/131/132/133/136; after rounds 2–6: 7 of 9 fully migrated and green.

## See also

- `docs/projectinfo/architectuur/edge-cases.md` — deviating forms → `edge-*` proofs
- `claude/tasks/ANALYSE-fa-100.md`, `claude/tasks/CORPUSRUN-fa.md`, `claude/tasks/CORPUSRUN-mediatr.md` — sources of the numbers
- `claude/tasks/patroon-queue.md` — queue, priority follows this table
- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md` through `TAAK-073-unused-fa-using-after-package-removal.md`
