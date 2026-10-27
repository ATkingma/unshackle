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
  Side issue in the same output: `.OrTransientHttpStatusCode()` adds a second, identical `HandleResult(5xx || 408)` clause (harmless but redundant).
- **B3 polly.retry-variants: v8 `Outcome<T>.Exception` is nullable** (fergun `src/Apis/Musixmatch/MusixmatchClient.cs:42`).
  Before: `Policy<JsonDocument>.Handle<MusixmatchException>(...).RetryAsync(async (result, _) => { logger.LogWarning(result.Exception, "...", ((MusixmatchException)result.Exception).Hint); ... })`
  After: the body moves unchanged into `async Task OnRetryCallback(Outcome<JsonDocument> result, int unused1)` -> CS8600 + CS8602 at the cast (nullable warnings are errors).
  Expected: in a nullable-enabled context, write `result.Exception!` wherever the v7 `DelegateResult<T>.Exception` was dereferenced or cast, or report Manual.
- **B4 project-file: CS0433 duplicate ServiceCollection** (okta-sdk-dotnet `src/Okta.Sdk/Client/OktaApiServiceBuilder.cs:62`).
  The project references `Microsoft.Extensions.DependencyInjection 5.0.0`. The added Resilience package brings in `DependencyInjection.Abstractions 8.0`, which also defines `ServiceCollection` -> CS0433. Expected: bump the direct `Microsoft.Extensions.DependencyInjection` reference to match, or report Manual. Same family as B1.
  **Fixed** with B1: DependencyInjection 5.0.0 is below the 8.0 group's major, so the project is blocked with `PackageDowngrade`.

## See also

- `claude/tasks/polly-queue.md` (row 13)
