# TAAK-148 — MediatR: InternalsVisibleTo only Manual on a real call

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/archief/TAAK-147-mediatr-restpunten-144.md` (Not done, `shortcut:` in `AddMediatRPattern.Friend`)

## Assignment

TAAK-147 made an `internal` registration method with a scanned assembly parameter Manual as soon as the project had any `InternalsVisibleTo`. Now only Manual if another project really calls the method, and the message names that project (with file:line).

## Choice

- New interface `Core/Abstractions/ISolutionPattern : IPattern` with `RewriteAsync(node, model, solution, cancellationToken)`. The Fixer calls it if a pattern implements it (otherwise `Rewrite`); `ApplyPattern`/`Rewrite` in `Fixer` became async. No default interface member on `IPattern`: NSubstitute mocks of `IPattern` would override it.
- `AddMediatRPattern` implements it: with a friend it looks with `SymbolFinder.FindReferencesAsync` for calls of the enclosing method in projects with a different assembly name (multi-target copies therefore do not count). The synchronous `Rewrite` (without a solution) stays cautious: every friend "may call".
- Core stays without project references (`SymbolFinder` is in Workspaces, already a package of Core).

## Steps

- [x] Code: `ISolutionPattern`, `Fixer`, `AddMediatRPattern`
- [x] Proof `AddMediatR/edge-assemblies-friend`: manual → after (no caller in a one-project proof)
- [x] Test `AddMediatRPatternTests.RewriteAsync_InternalMethodWithFriend_ManualOnlyWhenOtherProjectCallsIt` (two projects, with/without a call)
- [x] Pattern docs plan/verify, `proofs.md` regenerated
- [x] `dotnet build`, `dotnet test` (740/740), samples 38/38
- [x] Harness on `<corpus>/src/evo`, output `<corpus>/t148/out/`:
      evo1 `ok` 45/45 → 45/45, 0 regressions, **rightly stays Manual**: `Fitnet.IntegrationTests/Common/TestEngine/Configuration/ConfigurationExtensions.cs:43` calls `AddInMemoryEventBus(Assembly.GetExecutingAssembly())`, and `FakeEventConsumer` (handler) lives in that test project. The message now names that project and that line.
      evo2 `ok` 39/39 → 39/39, 0 regressions, Manual as in TAAK-147 (public method).

## Not done

- Only `AddMediatRPattern` uses `ISolutionPattern`; other patterns do not need it.
- Calls via reflection are not seen (no reference in the solution).

## See also

- `claude/tasks/archief/TAAK-147-mediatr-restpunten-144.md`
- `docs/projectinfo/patterns/mediatr-add-mediatr-verify.md`
- `claude/tasks/REGRESSIE-corpus.md`
