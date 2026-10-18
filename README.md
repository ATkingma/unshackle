# Unshackle

Migrate .NET code away from libraries that went commercial (FluentAssertions, MediatR, AutoMapper).
Roslyn-powered, with a report and dry-run before anything changes.

> Version 0.1. Design and architecture: [docs/projectinfo](https://github.com/ATkingma/unshackle/blob/main/docs/projectinfo/README.md).

## What it migrates

| Fix (`--fix`) | From → to | Status |
|---|---|---|
| `fluentassertions` | FluentAssertions 6–7 → [Shouldly](https://github.com/shouldly/shouldly) | Most assertions rewritten (`Should().Be`, collections, exceptions, `AssertionScope`, `Equal`, …); the rest is reported as manual work |
| `mediatr` | MediatR 12 → [Mediator](https://github.com/martinothamar/Mediator) (source generator) | Handlers, notification handlers, pipeline/stream behaviors, pre/post-processors, exception handlers, `AddMediatR` |
| `automapper` | AutoMapper 12–14 → hand-written `ToB()` extension methods | `CreateMap` with `ForMember`/`MapFrom`/`Ignore`, constructors, flattening, nested maps, `Map` calls, `ProjectTo`, `AddAutoMapper`; only when provably equivalent |

`unshackle list` shows every pattern with a one-line description. Each pattern has proofs (`before` → `after`) in
`src/Unshackle.Tests/Proofs/`. A pattern never guesses: when a rewrite is not provably safe it is reported as
*Manual* with the reason, for you to finish by hand.

`fix` also migrates the project files: the old package reference and `<Using Include="..." />` go, the new packages
are added (central package management is respected).

## Quick start

Requires the .NET 10 SDK (the solution you migrate may target any framework the SDK can build, e.g. net6.0 or net48).

```bash
dotnet tool install --global Unshackle

cd MyApp                                          # the folder with your .sln/.slnx; commit your work first
dotnet restore

unshackle scan --fix fluentassertions             # what would change, writes nothing
unshackle fix  --fix fluentassertions --dry-run   # show the diff, writes nothing
unshackle fix  --fix fluentassertions --report report.md

dotnet build && dotnet test                       # always check the result with your own tests
```

**Full guide**: [docs/usage.md](https://github.com/ATkingma/unshackle/blob/main/docs/usage.md) (install, workflow, options, reports,
CI, troubleshooting). **All supported patterns**: [docs/patterns.md](https://github.com/ATkingma/unshackle/blob/main/docs/patterns.md).

`--fix` is `fluentassertions`, `mediatr` or `automapper` (`unshackle list` shows every pattern). Run one library at a time and
commit in between, so each step is easy to review and revert.

What `fix --dry-run` shows (from `samples/SampleShop`):

```diff
-   order.CustomerName.Should().StartWith("Ti").And.EndWith("me").And.HaveLength(5);
+   order.CustomerName.ShouldStartWith("Ti", Shouldly.Case.Sensitive);
+   order.CustomerName.ShouldEndWith("me", Shouldly.Case.Sensitive);
+   order.CustomerName.Length.ShouldBe(5);
-   order.Lines.Should().NotBeEmpty().And.HaveCount(2).And.NotContainNulls();
+   order.Lines.ShouldNotBeEmpty();
+   order.Lines.Count.ShouldBe(2);
+   order.Lines.ShouldNotContain(i => i == null);
```

Then a summary per pattern (`Fixed` / `Manual`). When a few manual findings hold back the rest (MediatR migrates all or nothing,
because a half-migrated app breaks at run time), the summary lists exactly those, so you know where to start. AutoMapper migrates
per map: a map it cannot rewrite stays on AutoMapper, together with the maps it depends on and the calls that use it, and the
rest is migrated (reported as `Stays on AutoMapper: …`).

## How well does it work?

Measured on open-source solutions the tool had never seen (none was used to develop it), with their own test suites run before and
after the fix ([details](https://github.com/ATkingma/unshackle/blob/main/claude/tasks/CORPUSRUN-blind.md)). Latest round: 18 repos
measured again plus 18 new ones (tool at commit `db05908`):

| Fix | Repos | Fully migrated | Partly migrated (old library stays for the rest) | Stopped, nothing rewritten | Build broken after fix | Test regressions |
|---|---|---|---|---|---|---|
| FluentAssertions → Shouldly | 12 | 4 | 6 | 0 | 2 | 0 |
| MediatR → Mediator | 12 | 6 | 1 | 5 | 0 | 0 |
| AutoMapper → mappings | 12 | 1 | 9 | 2 | 0 | 0 |

"Build broken" are four known bugs (open tasks TAAK-280..283, mostly preprocessor directives and `required` members): the build after `fix` caught
all of them, no test changed silently. On the 31 repos that build after the fix, no test regressed.
Where only part of a solution was migrated the report lists the rest as Manual, with the reason.

## Known limitations

- **AutoMapper**: only mappings that are provably the same as hand-written code are rewritten. Cycles, `ReverseMap` with unflattening,
  private setters, resolvers and converters stay on AutoMapper (with their dependent maps and calls), so the package, the profiles and
  `AddAutoMapper` stay too; injected `IMapper` fields are not removed yet. A class whose test stubs `IMapper` (Moq, NSubstitute,
  FakeItEasy) keeps calling `IMapper` for the stubbed maps, which then stay in the profile next to their generated method; other classes
  use the method. A test that configures its own `MapperConfiguration` with maps still blocks the whole fix.
- **MediatR**: `AddMediatR` in a shared or generic library method, direct use of `MediatR.Mediator` and some decorators are manual:
  Mediator's source generator must see the handlers at compile time, in the host project.
- **FluentAssertions**: custom assertion classes, `BeEquivalentTo` with options and `.Which`/`.And` chains on another subject are manual.
- The tool never guesses: when it is unsure it says so in the report. Still, always build and run your tests after `fix`.

## Usage

Exit codes: `0` ok (also when some findings need manual work), `1` load or write error (or `fix` refused on a dirty
git working tree), `2` invalid arguments.

`fix` refuses to write when the solution's git working tree has uncommitted changes; commit or stash first, or pass
`--force`. Reports: `.csv`, `.json` or `.md`, with the columns `File,Line,Fix,Pattern,Status,Reason`.

To build and install from source instead of NuGet:

```bash
dotnet pack src/Unshackle.Interface -o ./nupkg
dotnet tool install --global --add-source ./nupkg Unshackle
```

## Issues and requests

Missing a pattern, want another library supported, or got a wrong rewrite?
[Open an issue](https://github.com/ATkingma/unshackle/issues/new/choose) with one of the forms.
[docs/issues.md](https://github.com/ATkingma/unshackle/blob/main/docs/issues.md) explains what to include.

## Projects

| Project | Role |
|---|---|
| `src/Unshackle.Interface` | CLI (`unshackle fix`, `scan`, `list`), console output |
| `src/Unshackle.Core` | The `Fixer`, fixes per library, patterns per fix, contracts |
| `src/Unshackle.DataAccess` | Loading/saving solutions (MSBuild), report writers (CSV, JSON, Markdown) |
| `src/Unshackle.Tests` | Unit tests, proofs (`*.before.cs` / `*.after.cs`) per pattern, and `Integration/` tests on real projects in `samples/` |

Dependencies: `Interface → Core ← DataAccess`, `Interface → DataAccess`. Core depends on none of the others.
