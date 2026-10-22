# Corpus: Polly v7 API usage in 20 open-source projects

Measured on 2026-10-22 for TAAK-308 (steps 1–2). Model: `corpus-automapper.md`.

## Why (facts checked 2026-10-22)

- Polly joins the Open Source Maintenance Fee (OSMF): US $20/month per organisation earning at least US $20,000 from a
  product that uses Polly; payment is requested from 2026-11-16, and only for **the next major version released after
  2026-11-16**. Existing versions (7.x, 8.x) are not subject to the fee.
  Sources: [announcement 2026-07-14, update 2026-10-12](https://www.thepollyproject.org/2026/07/14/polly-osmf-announcement.html),
  [Polly README](https://github.com/App-vNext/Polly/blob/main/README.md).
- "Only direct references to Polly are subject to the fee"; Polly as a dependency of `Microsoft.Extensions.Resilience` /
  `Microsoft.Extensions.Http.Resilience` is exempt (same sources; maintainer martincostello confirms in
  [discussion #3183](https://github.com/App-vNext/Polly/discussions/3183)).
- `Microsoft.Extensions.Resilience` 10.11.0 (MIT) depends on `Polly.Extensions` and `Polly.RateLimiting` 8.4.2;
  `Microsoft.Extensions.Http.Resilience` 10.11.0 (MIT) on `Microsoft.Extensions.Resilience`
  ([nuget](https://www.nuget.org/packages/Microsoft.Extensions.Resilience),
  [nuget](https://www.nuget.org/packages/Microsoft.Extensions.Http.Resilience)). `Polly` 8.8.0 (BSD-3-Clause) depends on
  `Polly.Core` and still carries the v7 API ([nuget](https://www.nuget.org/packages/Polly)).
- Open points: in #3183 a maintainer said the licence will change with the new version, while the 2026-10-12 update says
  "Polly's license is not changing"; unanswered there: does calling Polly APIs without a direct `PackageReference`
  (transitive only, or CPM transitive pinning) count as direct use? Conservative reading: a fix must remove every direct
  `Polly`/`Polly.Core`/`Polly.Extensions.Http`/`Microsoft.Extensions.Http.Polly` reference **and** every v7 API call.
- `Microsoft.Extensions.Http.Polly` (the `AddPolicyHandler` package) pulls in `Polly` 7.x itself, so repos without a
  direct `Polly` reference still use the v7 API through it.

## Method

- Candidates from `gh search code` on `Include="Polly" Version="7.2` and `Include="Microsoft.Extensions.Http.Polly"`;
  38 cloned shallowly (HEAD, 2026-10-22) into `D:\playground\corpus-polly\src\` (20 kept) and `extra\` (18 dropped:
  fewer than 4 v7 hits, already on v8, or no Polly at HEAD, e.g. eShopOnContainers, AspNetCore.Diagnostics.HealthChecks,
  minio-dotnet). List: `claude/tools/corpus-polly-repos.txt`.
- Measurement: regex over `*.cs` (comments stripped; `bin`, `obj`, `node_modules`, `wwwroot`, `*.g.cs`, `*.Designer.cs`
  skipped), only files mentioning `Polly`/`AddPolicyHandler`; `Execute*` only in files with `using Polly`. No Roslyn:
  numbers are indicative. Scripts outside the repo: `python -I D:\playground\corpus-polly\tools\measure.py <src> <out.json>`,
  then `agg.py <out.json>` (`tools\polly.json` is this run).
- Builds: `tools\build.sh`, one repo at a time, `dotnet build <sln> -m:2` with SDK 10, from outside the repo,
  `TEMP`/`TMP` on `D:`, 15 minute limit. Logs: `D:\playground\corpus-polly\logs\<name>.log`.

## Repos

"v7 hits" = sum of all v7 rows below for that repo. Packages as found in `*.csproj`/`*.props` (`MEHP` =
`Microsoft.Extensions.Http.Polly`).

| Name | Repo (ref) | Packages | TFM | Tests | Builds | v7 hits |
| ---- | ---------- | -------- | --- | ----- | ------ | ------: |
| aerosharp | `wayfair-incubator/AeroSharp` (`a7f15b43ce3c`) | Polly 7.2.4 | net7.0 | ? | yes | 8 |
| babylon-gateway | `radixdlt/babylon-gateway` (`c4ac0e5f84d3`) | Polly 8.3.0, MEHP 8.0.2 | net8.0 | xUnit | yes | 16 |
| deepl-dotnet | `DeepL/deepl-dotnet` (`5541f8c3fcfd`) | MEHP 5.0.1 | net5.0 | xUnit | yes | 10 |
| eventualshop | `AntonioFalcaoJr/EventualShop` (`d63e424fb770`) | Polly 7.2.3, MEHP | net8.0 | xUnit, 3 | yes | 35 |
| fergun | `d4n3436/Fergun` (`e2824365241d`) | Polly 8.7.0, MEHP 10.0.9 | net10.0 | xUnit v3 | yes | 60 |
| gpr | `jcansdale/gpr` (`b2773c3cbf84`) | Polly 7.2.1 | netcoreapp3.0 | — | no: Nerdbank.GitVersioning 3.1 fails (shallow clone / old LibGit2Sharp) | 9 |
| heappe | `It4innovations/HEAppE` (`97afa4d61586`) | Polly 8.6.5, MEHP 9.0.0 | net9.0 | xUnit, 6 | partial: post-build `dotnet tool run swagger` | 17 |
| igdb-dotnet | `kamranayub/igdb-dotnet` (`fdbc037017d6`) | MEHP 9.0.5 | netstandard2.0 | xUnit | yes | 8 |
| listenarr | `Listenarrs/Listenarr` (`4cd3455c636a`) | Polly 8.6.6, MEHP 10.0.8 | net10.0 | xUnit | yes | 22 |
| logshark | `tableau/Logshark` (`d39594a8b5f3`) | Polly 7.2.1 | net8.0 | xUnit | partial: test project needs a private package | 9 |
| okta-sdk-dotnet | `okta/okta-sdk-dotnet` (`5755dfdc348f`) | Polly 7.2.4 / 8.6.5 | net10.0 | xUnit, 3 | yes | 21 |
| recfluence | `markledwich2/Recfluence` (`d64c2790ebba`) | Polly 7.2.2 | net6.0 | NUnit | yes | 27 |
| seedwork.cqrs.bus | `tiagor87/Seedwork.CQRS.Bus` (`91f74ad95514`) | Polly 7.2.1 | netstandard2.0 | xUnit | yes | 9 |
| sme-novosgp | `prefeiturasp/SME-NovoSGP` (`c7c06015d2be`) | Polly 7.2.2, MEHP 5.0.1, Polly.Extensions.Http 3.0.0 | net5.0 | xUnit + MSTest, 11 | yes | 44 |
| sme-servidorrelatorios | `prefeiturasp/SME-ServidorRelatorios` (`73a137880602`) | Polly 7.2.2, MEHP 3.0.3 | netcoreapp3.0 | xUnit, 2 | yes | 12 |
| squidlr | `rgueldenpfennig/Squidlr` (`0349007daa43`) | MEHP 9.0.10 | net9.0 | xUnit, 2 | partial: MAUI app needs the `maui-android` workload | 10 |
| starnight | `StarnightLibrary/starnight` (`f9166f9d94d6`) | MEHP 7.0.0, Polly.Contrib.WaitAndRetry 1.1.1 | net7.0 | xUnit | yes | 9 |
| suucat | `ebubekirdinc/SuuCat` (`aa467180a315`) | Polly 7.2.3 | net7.0 | NUnit | yes | 24 |
| trader | `JorgeCandeias/Trader` (`90ce56f3443d`) | Polly 7.2.3 | net6.0 | xUnit, 10 | partial: `.sqlproj` needs SSDT | 25 |
| uno.check | `unoplatform/uno.check` (`cd4ed86d5a0a`) | Polly 7.2.2 | net472 | xUnit | yes | 10 |

Summary: 15 build cleanly, 4 partially (error only in a non-Polly project or step), 1 does not (gpr). Note: the first
build pass hit a full `C:` drive (NuGet cache); the affected repos were rebuilt with `NUGET_PACKAGES`/`NUGET_HTTP_CACHE_PATH`
on `D:` (`tools\rebuild.sh`, `D:\playground\corpus-polly\nuget\`).

## Frequencies

Hits = regex matches over all 20 repos; "Projects" = repos with at least one hit; "In tests" = hits in paths with `test`.

| API / shape | Hits | Projects | In tests | Draft queue row |
| ----------- | ---: | -------: | -------: | --------------- |
| `Policy.Handle<E>()` / `HandleInner` | 30 | 12 | 0 | 2 |
| `Policy.HandleResult<T>()` | 5 | 5 | 1 | 2 |
| `Policy<T>.Handle<E>()` | 3 | 3 | 0 | 2 |
| `.Or<E>()` / `.OrResult(...)` | 21 | 9 | 0 | 2 |
| `HttpPolicyExtensions.HandleTransientHttpError()` | 18 | 7 | 0 | 6 |
| `.OrTransientHttpStatusCode()` / `OrTransientHttpError()` | 2 | 1 | 0 | 6 |
| `.WaitAndRetryAsync(...)` | 27 | 16 | 0 | 1 |
| `.WaitAndRetry(...)` (sync) | 11 | 2 | 0 | 4 |
| `.Retry(...)` / `.RetryAsync(...)` | 11 | 7 | 1 | 1 |
| `.RetryForever(Async)` / `.WaitAndRetryForever(Async)` | 4 | 3 | 0 | 1 |
| … `onRetry` callback (named) | 36 | — | — | 3 |
| … `Math.Pow(2, attempt)` delay (in Polly files) | 26 | — | — | 1 |
| `.CircuitBreaker(Async)(...)` | 5 | 3 | 0 | 7 |
| `.AdvancedCircuitBreaker(Async)` | 0 | 0 | 0 | — |
| `Policy.Timeout(Async)` | 7 | 5 | 0 | 8 |
| `Policy.Wrap(Async)` / `.WrapAsync(...)` | 17 | 9 | 0 | 9 |
| `.Fallback(Async)` | 1 | 1 | 0 | Manual |
| `Policy.NoOp(Async)` | 2 | 2 | 0 | 10 |
| `Policy.Bulkhead(Async)` | 1 | 1 | 0 | Manual |
| `Policy.Cache(Async)` | 2 | 1 | 0 | Manual (no v8 equivalent) |
| `Policy.RateLimit(Async)` | 3 | 3 | 0 | Manual |
| `policy.ExecuteAsync(...)` | 51 | 12 | 3 | 1 |
| `policy.Execute(...)` (sync) | 12 | 3 | 0 | 4 |
| `.ExecuteAndCapture(Async)` | 4 | 3 | 0 | Manual |
| `new Context(...)` | 14 | 2 | 5 | Manual |
| `IAsyncPolicy` / `IAsyncPolicy<T>` (type use) | 39 | 9 | 4 | 5 |
| `ISyncPolicy` (type use) | 0 | 0 | 0 | — |
| `(Async)RetryPolicy` (type use) | 23 | 6 | 0 | 5 |
| `(Async)PolicyWrap` (type use) | 1 | 1 | 0 | 9 |
| `TimeoutRejectedException` | 7 | 3 | 0 | 8 |
| `BrokenCircuitException` | 0 | 0 | 0 | 7 |
| `AddPolicyHandler(...)` | 32 | 8 | 0 | 6 |
| `AddTransientHttpErrorPolicy(...)` | 2 | 2 | 0 | 6 |
| `PolicyRegistry` / `AddPolicyRegistry` | 29 | 3 | 4 | Manual |
| `AddPolicyHandlerFromRegistry` | 0 | 0 | 0 | — |
| `Backoff.*` (Polly.Contrib.WaitAndRetry) | 1 | 1 | 0 | Manual |
| v8 already: `ResiliencePipeline`, `AddResilienceHandler` | 0 | 0 | 0 | — |

## Observations

- Retry dominates: `WaitAndRetryAsync` + `Handle<E>` + `ExecuteAsync` is the core shape in 16 of 20 repos.
- Exponential delay is written by hand as `Math.Pow(2, attempt)` seconds (2, 4, 8 s). v8 `DelayBackoffType.Exponential`
  with `Delay = 1 s` gives 1, 2, 4 s (plus jitter only if enabled), so a literal mapping changes timing; keeping the
  lambda as `DelayGenerator` is behaviour-preserving.
- `AddPolicyHandler` (MEHP) is the second big group, often with `HttpPolicyExtensions.HandleTransientHttpError()`;
  target is `AddResilienceHandler(name, builder => ...)` with `HttpClientResiliencePredicates`.
- v7 `onRetry` is called *before* the delay with `(exception|outcome, timespan, attempt, context)`; v8 `OnRetry` receives
  `OnRetryArguments<T>` (attempt number is 0-based). Each lambda body needs a parameter mapping.
- Sync `Execute`/`WaitAndRetry` is rare (2–3 repos) but exists (RabbitMQ connection set-up).
- Long tail with no clean v8 equivalent: `Cache`, `Bulkhead`, `Fallback`, `PolicyRegistry`, `Context`,
  `ExecuteAndCapture`, Contrib `Backoff` — Manual first.

## See also

- `claude/tasks/TAAK-308-polly-to-resilience.md` — the task
- `docs/projectinfo/architectuur/corpus-automapper.md` — the model for this survey
- `claude/tools/corpus-polly-repos.txt` — repo list with pinned refs
