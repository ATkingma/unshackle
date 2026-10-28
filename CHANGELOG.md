# Changelog

All notable user-visible changes. Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow [SemVer](https://semver.org/).

Status per fix comes from the corpus rounds (open-source repos, their own tests run before and after `fix`). Lines marked
`Corpus:` are the only place the status of a fix is stated; they change when a new round is merged.

## [0.3.0] - unreleased

### Added

New fixes (run each with `--fix <id>`; `unshackle list` shows every pattern):

- **`moq`**: Moq → NSubstitute (setups, returns, throws, sequences, verify, verify-all).
  Corpus: pending (round 2 on 5 repos found build breaks B2-B7, all fixed with proofs; rerun open).
- **`polly`**: Polly v7 policies → Microsoft.Extensions.Resilience / Polly v8 pipelines. Polly charges a fee from 2026-11-16.
  Corpus: pending (round 3: 12 of 14 repos build and test with 0 regressions caused by the fix; the 2 restore failures are now Manual, rerun open).
- **`epplus`**: EPPlus → ClosedXML, per project (a project the tool cannot fully migrate stays on EPPlus).
  Corpus: 0 regressions (re-measure before/after; every build that fails after the fix fails the same way before it).
- **`efbulk`**: Z.EntityFramework bulk `DeleteFromQuery`/`UpdateFromQuery` → EF Core `ExecuteDelete`/`ExecuteUpdate`; EF Core < 7 is blocked.
  Corpus: 0 regressions (3 repos).
- **`masstransit`**: report-only detector for MassTransit v8 (consumers, sagas, transport, outbox, test harness, ...); it changes no code.
  Advice in the report: pin MassTransit < 9 (9.x is commercial); no free drop-in target ships on NuGet yet.
  Corpus: 0 tool bugs (scan only).
- **`specflow`**: SpecFlow 3.x → Reqnroll (packages, namespaces, renamed types, v3→v4 breaks, `<Using>` items, `Directory.Build.props`,
  `reqnroll.json`), per unit with its shared dependencies. Step texts are pinned to regular expressions.
  Corpus: pending (preview, no corpus proof yet).
- **`xunit3`**: xUnit v2 → v3 (packages, `OutputType Exe`, abstractions, async lifetime, `Record.ExceptionAsync`, runner and
  `Microsoft.NET.Test.Sdk` bumps; extensibility points are Manual).
  Corpus: pending (preview, no corpus proof yet).

CLI:

- `--sequential`: fix one file at a time (same output, less memory).
- `--max-degree <n>`: fix at most `n` files in parallel (default: processor count, capped).

### Changed

- **Faster**: files are fixed in parallel and files without a trace of the old library are skipped (a large solution went from 547 s to 201 s,
  reports identical).
- **Report**: rows that keep the old package now say why and name what keeps it (the dependent project, the helper call with `file:line`,
  the package that was added); Info rows explain kept config files and pinned settings instead of failing silently.
- **Project files**: a package is only removed when nothing in the project uses it anymore.

Existing fixes:

- **`fluentassertions`** (FluentAssertions → Shouldly): Corpus: 0 regressions (12 repos).
- **`mediatr`** (MediatR → Mediator): Corpus: 0 regressions (12 repos; all-or-nothing, so 5 got no change).
- **`automapper`** (AutoMapper → hand-written mappings): Corpus: 0 regressions (12 repos; mostly partial migrations).

### Known limitations

- `moq`: a test that depends on the generated proxy type name (Castle proxy vs NSubstitute proxy) fails after the fix; this cannot be
  detected up front (accepted limitation B1).
- `polly`: when the fix would downgrade a package across a project reference, the project is reported as Manual.
- `masstransit`: no rewrite; stay on 8.5.x until a free v8 fork ships on NuGet.
- `specflow` and `xunit3`: no corpus proof yet; review the diff and run your tests.
- A solution whose `global.json` asks for an SDK that is not installed cannot be loaded.
- Always build and run your own tests after `fix`.

## [0.2.0] - 2026-10-19

## [0.1.0]

[0.3.0]: https://github.com/ATkingma/unshackle/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/ATkingma/unshackle/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/ATkingma/unshackle/releases/tag/v0.1.0
