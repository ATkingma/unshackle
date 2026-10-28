# Corpus run Polly (L6 prep)

## Linux baseline (2026-10-22) - WIP

Clones, results and logs live on the corpus disk (`corpus-polly/src`, `corpus-polly/_results/{base,scan}/<name>`), driven by `claude/tools/corpus-am.sh` (FIX=polly).
Status: clone done for all 20 repos. Baseline and scan in progress, updated per repo.

| Repo            | Baseline             | Scan    |
| --------------- | -------------------- | ------- |
| aerosharp       | ok                   | pending |
| babylon-gateway | ok                   | pending |
| deepl-dotnet    | ok                   | pending |
| eventualshop    | ok                   | pending |
| fergun          | ok                   | pending |
| gpr             | baseline-build-fails | pending |

## Windows baseline + after round (2026-10-26)

Driver: `claude/tools/corpus-polly-run.sh` -> `corpus-polly-am.sh` (copy of `corpus-am.sh` with `--max-degree 4` and a `PYTHON` override), sequential, TEST_TIMEOUT=1200.
`after` = build, `fix --fix polly --force`, clean, restore, build, test, compare with base.

| Repo | Baseline | After | Fixed | Manual | Tests base -> after |
| ---- | -------- | ----- | ----- | ------ | ------------------- |
| aerosharp | ok | ok | 0 | 19 | 270/516 -> 270/516, 0 regressions |
| babylon-gateway | ok | restore-after-fix-fails (B1) | 14 | 14 | |
| deepl-dotnet | ok | ok | 0 | 9 | 627/663 -> 627/663, 0 regressions |
| eventualshop | ok | restore-after-fix-fails (B1) | 5 | 16 | |
| fergun | ok | BUILD-AFTER-FIX-FAILS (B2, B3) | 7 | 20 | |
| gpr | baseline-build-fails | (excluded) | 0 | 11 | |
| heappe | baseline-build-fails | restore-after-fix-fails (B1) | 22 | 20 | |
| igdb-dotnet | ok | ok | 2 | 7 | 20/49 -> 20/49, 0 regressions |
| listenarr | ok | restore-after-fix-fails (B1, NU1109) | 4 | 14 | |
| logshark | restore-fails | (excluded) | 0 | 0 | |
| okta-sdk-dotnet | ok | BUILD-AFTER-FIX-FAILS (B4) | 3 | 27 | |
| recfluence | ok | ok | 0 | 26 | 4/19 -> 4/19, 0 regressions |
| seedwork.cqrs.bus | ok | restore-after-fix-fails (B1) | 1 | 8 | |
| sme-novosgp | ok | ok | 0 | 115 | test runs time out (1200 s) in base and after; no code changed |
| sme-servidorrelatorios | ok | ok | 0 | 31 | 2/7 -> 2/7, 0 regressions |
| squidlr | restore-fails | (excluded) | 0 | 0 | |
| starnight | baseline-build-fails | (excluded) | 0 | 201 | |
| suucat | ok | ok | 0 | 76 | 22/50 -> 22/50, 0 regressions |
| trader | baseline-build-fails | (excluded) | 0 | 49 | |
| uno.check | ok | ok | 1 | 5 | 376/376 -> 376/376, 0 regressions |

Of the 14 repos with a green baseline, 8 pass with 0 test regressions and 6 break: 4 at restore (B1) and 2 at build (fergun B2+B3, okta B4).

### Bugs (repros; B1 and B4 fixed afterwards, see each entry)

- **B1 project-file: package downgrade after adding Microsoft.Extensions.Resilience / Http.Resilience 10.11.0** (babylon-gateway, eventualshop, heappe, seedwork.cqrs.bus, listenarr).
  The added package pulls `Microsoft.Extensions.*` >= 8.0.x / 9.0.21 / 10.0.x transitively. The project references an older version directly, so restore fails with NU1605 (warning as error) or NU1109 (central pinning).
  Repro: `RadixDlt.NetworkGateway.Abstractions.csproj` references `Microsoft.Extensions.Logging.Abstractions 8.0.0`. After the fix: `NU1605 Detected package downgrade: Microsoft.Extensions.Logging.Abstractions from 8.0.3 to 8.0.0` (via Resilience 10.11.0 -> Telemetry.Abstractions 10.11.0). In eventualshop, the direct `Microsoft.Extensions.Http 8.0.0-preview.1` conflicts with Http.Resilience, which needs >= 8.0.1.
  Expected: pick a Resilience version whose dependencies match the project's direct `Microsoft.Extensions.*` references, bump those references, or report Manual (`PackageDowngrade`) instead of writing a project that does not restore.
  **Fixed**: `PollyPackageDowngrade` blocks the project with `ProjectBlockingReason.PackageDowngrade` (resolved versions from the reference paths vs. the nuspec minimums); no bump, the project stays on v7.
- **B2 polly.add-policy-handler: expression-bodied method returning IHttpClientBuilder** (fergun `src/Extensions/HttpClientBuilderExtensions.cs:21`).
  Before: `public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder builder) => builder.AddTransientHttpErrorPolicy(p => p.OrTransientHttpStatusCode().WaitAndRetryAsync(3, a => TimeSpan.FromSeconds(Math.Pow(2, a))));`
  After: `=> builder.AddResilienceHandler("retry", builder1 => builder1.AddRetry(...));` -> CS0266 (`IHttpResiliencePipelineBuilder` to `IHttpClientBuilder`).
  Expected: when the value of the `AddPolicyHandler`/`AddTransientHttpErrorPolicy` call is used (returned, assigned or chained), keep returning the `IHttpClientBuilder` (block body `{ builder.AddResilienceHandler(...); return builder; }`) or report Manual.
  **Fixed 2026-10-27:** Manual `HandlerResultUsed` when the builder result is used (REQ-5b, proof `result-returned`).
  Side issue in the same output: `.OrTransientHttpStatusCode()` adds a second, identical `HandleResult(5xx || 408)` clause (harmless but redundant). **Fixed 2026-10-27 (TAAK-335):** each transient clause is written once (REQ-3b, proof `transient-or-status`).
- **B3 polly.retry-variants: v8 `Outcome<T>.Exception` is nullable** (fergun `src/Apis/Musixmatch/MusixmatchClient.cs:42`).
  Before: `Policy<JsonDocument>.Handle<MusixmatchException>(...).RetryAsync(async (result, _) => { logger.LogWarning(result.Exception, "...", ((MusixmatchException)result.Exception).Hint); ... })`
  After: the body moves unchanged into `async Task OnRetryCallback(Outcome<JsonDocument> result, int unused1)` -> CS8600 + CS8602 at the cast (nullable warnings are errors).
  Expected: in a nullable-enabled context, write `result.Exception!` wherever the v7 `DelegateResult<T>.Exception` was dereferenced or cast, or report Manual.
  **Fixed 2026-10-27:** `PollyOnRetry` appends `!` to every `.Exception` read that is not a null check, only when nullable warnings are enabled (REQ-4a, proof `on-retry-exception-nullable`).
- **B4 project-file: CS0433 duplicate ServiceCollection** (okta-sdk-dotnet `src/Okta.Sdk/Client/OktaApiServiceBuilder.cs:62`).
  The project references `Microsoft.Extensions.DependencyInjection 5.0.0`. The added Resilience package brings in `DependencyInjection.Abstractions 8.0`, which also defines `ServiceCollection` -> CS0433. Expected: bump the direct `Microsoft.Extensions.DependencyInjection` reference to match, or report Manual. Same family as B1.
  **Fixed** with B1: DependencyInjection 5.0.0 is below the 8.0 group's major, so the project is blocked with `PackageDowngrade`.

## Rerun after B1-B4 and TAAK-335 (2026-10-27, round 3)

Tool built from main `07552c5` (published to `corpus-polly/tool-r3`), driver `corpus-polly-run.sh after` on the 14 repos with a green baseline,
TEST_TIMEOUT=1200, NuGet caches under `corpus-polly/nuget`. Results in `_results/after`; the earlier same-day partial round moved to `_results/after-r2`.
Free disk stayed above the H1 threshold (98 GB at the end).

| Repo | After | Fixed | Manual | Tests base -> after |
| ---- | ----- | ----- | ------ | ------------------- |
| aerosharp | ok | 0 | 19 | 270/516 -> 270/516, 0 regressions |
| babylon-gateway | **restore-after-fix-fails (B5)** | 7 | 19 | |
| deepl-dotnet | ok | 0 | 11 | 627/663 -> 627/663, 0 regressions |
| eventualshop | ok (was B1) | 3 | 18 | 10/11 -> 10/11, 0 regressions |
| fergun | ok (was B2, B3) | 0 | 29 | 788/801 -> 788/801, 0 regressions |
| igdb-dotnet | ok | 0 | 10 | 20/49 -> 20/49, 0 regressions |
| listenarr | **restore-after-fix-fails (B6, NU1109)** | 3 | 14 | |
| okta-sdk-dotnet | ok (was B4) | 0 | 28 | 2833/3213 -> 2834/3213, 1 regression: file-lock flake (`okta.yaml` in use), not caused by the fix |
| recfluence | ok | 0 | 33 | 4/19 -> 4/19, 0 regressions |
| seedwork.cqrs.bus | ok (was B1) | 0 | 10 | no tests |
| sme-novosgp | ok | 0 | 141 | test run times out (1200 s) as in base; no code changed |
| sme-servidorrelatorios | ok | 0 | 41 | 2/7 -> 2/7, 0 regressions |
| suucat | ok | 0 | 86 | 22/50 -> 22/50, 0 regressions |
| uno.check | ok | 1 | 5 | 376/376 -> 376/376, 0 regressions |

12 of 14 pass with no test regression caused by the fix (was 8). B2, B3 and B4 are confirmed fixed (fergun, okta build and test green);
B1 is fixed for eventualshop and seedwork.cqrs.bus, but two variants remain (B5, B6).
fergun and okta now get 0 Fixed: the former Fixed sites became Manual (`HandlerResultUsed`, `PackageDowngrade`), as designed.
TAAK-335 (single transient clause) could not be observed directly: the fergun site is now Manual (B2 fix), so no corpus rewrite exercises it.

### New bugs (round 3)

- **B5 project-file: package downgrade across a project reference** (babylon-gateway).
  `restore1` fails with `NU1605 Detected package downgrade: Microsoft.Extensions.Logging.Abstractions from 8.0.3 to 8.0.0`:
  `RadixDlt.NetworkGateway.Abstractions` references `Logging.Abstractions 8.0.0` directly and gets `Microsoft.Extensions.Resilience 10.11.0`
  through `ProjectReference` `RadixDlt.CoreApiSdk` (-> Telemetry.Abstractions 10.11.0 -> Logging.Abstractions >= 8.0.3).
  `PollyPackageDowngrade` only compares a project's own direct references, so the project that receives Resilience transitively is not blocked.
  Oddly, the report lists `RadixDlt.CoreApiSdk.csproj` as Manual ("project-file edits not applied"), while the restore graph shows it with
  Resilience 10.11.0; check where that reference comes from (shared props or a sibling project edit).
  Expected: run the downgrade check over every project that gets the new package directly or through `ProjectReference`, and block (Manual) the
  originating edit when a consumer would downgrade.
- **B6 project-file: central package management pins below the Resilience minimums** (listenarr, NU1109; was listed under B1).
  The fix edits `Directory.Packages.props`, `Listenarr.Api.csproj` and `tests/Listenarr.Tests.csproj`; central pins of `Microsoft.Extensions.*`
  10.0.8 (`Http`, `Logging.Abstractions`, `Options`, `DependencyInjection.Abstractions`) are below the 10.0.13 that Resilience/Http.Resilience
  10.11.0 require, so restore fails with NU1109. The downgrade check does not read the versions from `Directory.Packages.props`.
  Expected: compare against the central `PackageVersion` pins (and bump them or report `PackageDowngrade`).

B5 and B6 fixed in TAAK-354 (Manual `PackageDowngrade`, no bump); the CoreApiSdk row is explained there.

## H1 free-disk rule (council round 15)

`claude/tools/corpus-polly-am.sh` (and `corpus-polly-run.sh`) stop the run when the corpus disk has less than `MIN_FREE_GB` (default 20)
free before a repo: "run incomplete: disk below threshold" in `_results/INCOMPLETE.txt`; remaining repos are not counted. Details in
`CORPUSRUN-automapper.md` ("H1 free-disk rule"). Harness copies outside the repo need the same change.

## See also

- `claude/tasks/polly-queue.md` (row 13)
