# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

**Don't know where something lives?** → see `claude/docs/INDEX.md`

**Took over a session?** → first read `claude/docs/overdracht.md` (ongoing work, open assignments, working agreements).

## Documentation architecture

| Location                         | Contents                                                                            |
| -------------------------------- | ----------------------------------------------------------------------------------- |
| `CLAUDE.md` (this file)          | Commands, architecture in brief, package rules                                      |
| `docs/projectinfo/README.md`     | Main document: C4 diagrams, links to architecture, project and every pattern (KingmaDoc) |
| `docs/projectinfo/project.md`    | What Unshackle is, why, what already exists, roadmap |
| `docs/projectinfo/architectuur/` | Projects, Core (Fixer → Fix → Pattern), testing, reporting |
| `claude/docs/INDEX.md`           | Full index: look here when you don't know where something lives                    |
| `claude/docs/code-standaarden.md` | Code style and design patterns |
| `claude/docs/agent-werkwijze.md` | Rules for task management, cross-references, document splitting                    |
| `claude/tasks/`                  | Active tasks `TAAK-NNN-<slug>.md`; completed ones in `archief/`                     |
| `.claude/skills/`                | `/taak-start`, `/taak-specifiek`, `/taak-oppakken`                                  |

File names are historical and partly Dutch (`archief/`, `overdracht.md`, `-modellen.md`, ...); they are kept so links keep working.

## Working agreements

- Communicate with the user in Dutch. Code, names, commit messages **and all documentation, models (KingmaDoc plan/models/verify, C4/UML diagram text), tasks, queues and corpus reports** are in English.
- **Never push or merge to `main` when a test fails (old or new).** Only push what demonstrably works (partial work included), and after every push check the CI run until it is green. See `claude/docs/agent-werkwijze.md` §6.
- Every pattern gets a KingmaDoc plan + verify in `docs/projectinfo/patterns/` and a row in `docs/projectinfo/README.md` (hook `.claude/hooks/check-pattern-docs.sh` guards this).
- Documentation and tasks follow `claude/docs/agent-werkwijze.md` (new document → `INDEX.md`, `## See also` at the bottom).
- Code follows `claude/docs/code-standaarden.md`: among others a class with `required init` for models, classic constructors, **never `sealed`**, `CancellationToken` always required, design patterns per GoF/refactoring.guru.

## Commands

```bash
dotnet build                                                        # warnings = errors, code style enforced
dotnet test                                                         # Microsoft.Testing.Platform (global.json), xUnit v3
dotnet test --project src/Unshackle.Tests -- --filter-method "*ShouldBe*"   # a single test
dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"
dotnet run --project src/Unshackle.Interface -- scan MijnApp.sln --fix fluentassertions
dotnet test --solution samples/SampleShop/SampleShop.slnx         # sample target code (old FA/MediatR/AutoMapper)
```

Exit code 8 from `dotnet test` means "no tests found", not an error.

CI: `.github/workflows/ci.yml` runs build, tests and the sample tests on every PR to `main` and every push to `main`.

## Architecture in brief

- `Interface → Core ← DataAccess`, `Interface → DataAccess`. **Core references no other project.** New I/O belongs in DataAccess, behind an interface from Core; `Interface/Program.cs` is the composition root.
- Core: `Fixer` → `IFix` (one per library) → `IPattern` (`Find` + `Rewrite`). A pattern never throws; if a rewrite fails, it returns `Manual(reason)` for the report.
- One test project `src/Unshackle.Tests`: unit (`Core/`, `DataAccess/`, `Interface/`), `Proofs/`, and `Integration/` (runs on `samples/` via MSBuild, slow). `samples/` is "user code": its own props and `.editorconfig`, not in `Unshackle.slnx`, not subject to our code standards.
- Every new pattern gets proofs in `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/` (`*.before.cs` + `*.after.cs` or `*.manual.txt`). `Proofs/` is not compiled; `before` must compile against the old library, `after` against the new one.
- Types in unit tests come from real assemblies (FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0, Shouldly, Mediator.Abstractions, `Basic.Reference.Assemblies.Net100`), not from a build.
- Only mock our own interfaces with NSubstitute, never Roslyn types. No Moq.

## Packages

- Versions only in `Directory.Packages.props` (central package management).
- No packages with license or sponsor checks (that is why Verify was removed). AutoMapper 14.0.0 is in the tests with `NoWarn="NU1903"`: deliberately the last free version, only as types for proofs.
- No `Microsoft.Build.Locator`: Roslyn 5.x `MSBuildWorkspace` uses its own build host.
