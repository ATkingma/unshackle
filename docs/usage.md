# Using Unshackle

A step-by-step guide to migrating a solution, plus every command and option. The supported rewrites are listed in
[patterns.md](patterns.md).

## 1. Requirements

- The **.NET 10 SDK** to run the tool (`dotnet --version`). The solution you migrate may target older frameworks
  (net6.0, net8.0, netstandard2.0, net48, …) as long as this SDK can build it.
- The solution must **restore and build** before you start: Unshackle reads the code with the compiler, so it needs every
  package and reference.
- A **git** repository with a clean working tree, so you can review and undo the change (`fix` checks this).

Supported library versions: FluentAssertions 6–7, MediatR 12, AutoMapper 12–14.

## 2. Install

```bash
dotnet tool install --global Unshackle     # install
dotnet tool update --global Unshackle      # later: update
dotnet tool uninstall --global Unshackle   # remove
unshackle --version
```

Prefer not to install globally? Use a local tool manifest in the repository:

```bash
dotnet new tool-manifest
dotnet tool install Unshackle
dotnet unshackle --help
```

## 3. Migrate, step by step

Run one library at a time and commit after each one.

```bash
cd MyApp
git switch -c migrate-fluentassertions
dotnet restore

# 1. What would change? Writes nothing.
unshackle scan --fix fluentassertions --report scan.md

# 2. Look at the diff. Writes nothing.
unshackle fix --fix fluentassertions --dry-run

# 3. Rewrite the code and the project files.
unshackle fix --fix fluentassertions --report fix.md

# 4. Check it.
dotnet build
dotnet test
git diff --stat
```

Without a path, Unshackle uses the only `.sln` or `.slnx` in the current directory. Pass a path when there are several:
`unshackle fix src/MyApp.sln --fix mediatr`. A single project file (`.csproj`) works too.

Then handle the **Manual** findings in the report (see section 5), build, test and commit. Running `fix` again after you
fixed some of them is safe: code that is already migrated is left alone.

## 4. Commands and options

| Command                                | What it does                                                                                      |
| -------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `unshackle list`                       | Every fix and its patterns. `--markdown` prints them as the tables in [patterns.md](patterns.md). |
| `unshackle scan [solution] --fix <id>` | Finds everything the fix would change and prints a summary. Writes no code.                       |
| `unshackle fix [solution] --fix <id>`  | Rewrites the code and the project files.                                                          |

| Option            | Commands  | Meaning                                                                                           |
| ----------------- | --------- | ------------------------------------------------------------------------------------------------- |
| `--fix <id>`      | scan, fix | Required: `fluentassertions`, `mediatr` or `automapper`.                                          |
| `--report <file>` | scan, fix | Writes every finding to `.md`, `.csv` or `.json` (format from the extension).                     |
| `--dry-run`       | fix       | Prints the diff per file, writes nothing.                                                         |
| `--force`         | scan, fix | `fix`: writes even when the git working tree is dirty or there is no git. Both: overwrites an existing report. |
| `--max-degree <n>` | scan, fix | Rewrites at most `n` files at once (default: the processor count, at most 8). Lower it when memory runs out on a large solution. |
| `--sequential`    | scan, fix | Rewrites one file at a time. Slower; use it when even `--max-degree 2` uses too much memory, or to rule out parallelism when comparing runs. |

The output (code, summary, report) is identical whatever `--max-degree` or `--sequential` you choose; they only trade speed for memory.

**Exit codes**: `0` done (also when there are Manual findings), `1` the solution could not be loaded or written, or `fix`
refused because of uncommitted changes, `2` invalid arguments (unknown fix or report extension, no or several solutions found).

## 5. Reading the result

The console shows one row per pattern with the number of **Fixed** and **Manual** findings, then a total.

- **Fixed**: rewritten. Some carry a _warning_ (e.g. a culture-sensitive string comparison): check those lines.
- **Manual**: not rewritten, with the reason. It is either impossible to do safely (a different semantics in the new library) or
  needs a decision only you can make.
- **"N findings wait on these M manual ones"**: for MediatR and AutoMapper the migration is all or nothing (a half-migrated app
  breaks at run time). When a few Manual findings block the rest, the summary lists exactly those: fix them, run `fix` again.
- **MediatR, partly migrated**: before giving up, MediatR tries a partial migration: the code tied to the Manual findings (handlers, senders,
  registrations) stays on MediatR, written as `global::MediatR.X`, and the rest moves to Mediator; both stay registered
  side by side. Every kept place gets a Manual row "Stays on MediatR next to Mediator". When that cannot be done safely
  (for example `AddMediatR(...)` itself has to stay), nothing changes, as in the previous point.

The report has one row per finding: `File, Line, Fix, Pattern, Status, Reason`. Open `report.md` in your editor or import the
`.csv` in a spreadsheet to work through the Manual ones.

## 6. What `fix` changes

|               | FluentAssertions → Shouldly                                                  | MediatR → Mediator                                                                                                                                      | AutoMapper → mappings                                                                                 |
| ------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| Code          | Assertions rewritten (`x.Should().Be(y)` → `x.ShouldBe(y)`), `using` swapped | Handlers return `ValueTask`, behaviors, processors, exception handlers, `AddMediatR` → `AddMediator`; `Send`/`Publish` used as a `Task` get `.AsTask()` | `CreateMap<A, B>()` becomes a `static B ToB(this A source)` extension; `mapper.Map<B>(a)` → `a.ToB()` |
| Packages      | `FluentAssertions` out, `Shouldly 4.3.0` in                                  | `MediatR` (+ `MediatR.Contracts`) out, `Mediator.Abstractions 3.0.2` in; `Mediator.SourceGenerator` only in the project that calls `AddMediatR`         | AutoMapper stays referenced (the emptied profiles remain); remove it yourself when nothing uses it    |
| Global usings | `<Using Include="FluentAssertions" />` and `global using` swapped            | idem for `MediatR`                                                                                                                                      | —                                                                                                     |

Central package management (`Directory.Packages.props`) is respected: versions go there, not in the project files.

## 7. In a CI pipeline

`scan` writes no code, so it can run on every build to track what is left, e.g. in GitHub Actions:

```yaml
- run: dotnet tool install --global Unshackle
- run: dotnet restore
- run: unshackle scan MyApp.sln --fix fluentassertions --report unshackle.md
- uses: actions/upload-artifact@v4
  with:
      name: unshackle-report
      path: unshackle.md
```

## 8. Troubleshooting

| Symptom                                            | Cause and fix                                                                                                                                                                         |
| -------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `The working tree has uncommitted changes`         | Commit or stash first, or preview with `--dry-run`. `--force` writes anyway.                                                                                                          |
| `No .sln or .slnx in …` / `Several solutions in …` | Pass the solution path as the first argument.                                                                                                                                         |
| Load errors, or almost every finding is Manual     | The solution does not restore or build. Run `dotnet restore` and `dotnet build` first and fix those errors.                                                                           |
| The build fails after `fix`                        | Please [open an issue](https://github.com/ATkingma/unshackle/issues/new/choose) with the error and the line; undo with `git checkout -- .`. A rewrite that breaks the build is a bug. |
| A test fails after `fix` that passed before        | Same: open an issue. Check first that it is not flaky (run the tests twice on the original code).                                                                                     |
| `Mediator` source generator errors (`MSG…`)        | The host project must call `AddMediator` once and reference `Mediator.SourceGenerator`; libraries only reference `Mediator.Abstractions`.                                             |

## 9. Limits worth knowing

- Unshackle never guesses: anything it cannot prove equivalent stays in the report as Manual.
- AutoMapper: cycles, `ReverseMap` with unflattening, private setters, resolvers, converters and `ConstructUsing` stay Manual.
- MediatR: `AddMediatR` in a shared or generic library method, direct use of `MediatR.Mediator`, and some decorators stay Manual.
- FluentAssertions: custom assertion classes (`ReferenceTypeAssertions` subclasses), `BeEquivalentTo` with options, and `.And`/`.Which`
  chains on another subject stay Manual.
- Always build and run your tests after `fix`.

## See also

- [patterns.md](patterns.md) — every supported pattern
- [issues.md](issues.md) — how to report a wrong rewrite or ask for a pattern
- [README](https://github.com/ATkingma/unshackle/blob/main/README.md)
