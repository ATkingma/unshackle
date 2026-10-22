# Candidate: Swashbuckle.AspNetCore to Microsoft.AspNetCore.OpenApi

Scout report for TAAK-410, 2026-10-22. Light research: web sources plus shallow clones, regex counts, no builds.
Verdict: **no-go for now** (see [Recommendation](#recommendation)).

## Status facts (primary sources)

| Fact | Source |
| ---- | ------ |
| Announced 2024-03-18: the .NET 9 web API template drops `Swashbuckle.AspNetCore` for `Microsoft.AspNetCore.OpenApi`, reason "no longer actively maintained by its community owner" (68 open PRs, last merged Nov 2022, no .NET 8 release at the time). Later converted to discussion #58103. | <https://github.com/dotnet/aspnetcore/issues/54599> |
| The status was disputed in the thread: a 6.6.x release with .NET 8 support and a new core team followed. | same thread |
| Since then the project is **active**: v10.2.0 (adds `MapSwaggerUI`/`MapReDoc`), v10.2.1 (Microsoft.OpenApi 2.7.5, security fix GHSA-v5pm-xwqc-g5wc), v10.2.2, v10.2.3, v10.3.0, v10.3.1 (19 Oct). The releases page shows day and month only; the 10.x line matches .NET 10, so these are 2026. | <https://github.com/domaindrivendev/Swashbuckle.AspNetCore/releases> |
| License: MIT, free, no sponsor or commercial gate (from general knowledge; not re-fetched, confirm in the repo `LICENSE` before the council decides). | repo |
| Microsoft docs (updated 2026-10-08): `Microsoft.AspNetCore.OpenApi` generates documents only (JSON, YAML at runtime; OpenAPI 3.1 default in .NET 10, 3.2 in .NET 11); **no UI included**, Swagger UI or Scalar must be added separately; build-time generation via `Microsoft.Extensions.ApiDescription.Server`. The docs still show Swashbuckle as a working combination. | <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi?view=aspnetcore-10.0> |

Conclusion on motivation: this is **not** a license/sponsor/EOL case like FluentAssertions, MediatR or AutoMapper. It is a
"vendor preference" migration.

## API distance

| Swashbuckle shape | Microsoft.AspNetCore.OpenApi equivalent | Class |
| ----------------- | ---------------------------------------- | ----- |
| `AddSwaggerGen()` | `AddOpenApi()` | Fixed |
| `SwaggerDoc("v1", new OpenApiInfo{...})` | `AddOpenApi("v1", o => o.AddDocumentTransformer(...))` (info set in a document transformer) | Fixed for literal `OpenApiInfo`, else Manual |
| `UseSwagger()` | `MapOpenApi()` (route changes from `/swagger/v1/swagger.json` to `/openapi/v1.json`) | Fixed, client URLs change |
| `UseSwaggerUI(c => c.SwaggerEndpoint(...))` | no built-in UI: Scalar (`MapScalarApiReference`) or keep `Swashbuckle.AspNetCore.SwaggerUI` alone | Manual (UI choice) |
| `AddSecurityDefinition` / `AddSecurityRequirement` | document transformer for `Components.SecuritySchemes` plus operation transformer for requirements | Manual |
| `IOperationFilter`, `ISchemaFilter`, `IDocumentFilter` | `IOpenApiOperationTransformer`, `IOpenApiSchemaTransformer`, `IOpenApiDocumentTransformer`; different context types, Microsoft.OpenApi 2.x model | Manual (logic is user code) |
| `IncludeXmlComments` | built in since .NET 10 via compiler source generator (`GenerateDocumentationFile`) | Fixed (delete call), output differs |
| `[SwaggerOperation]`, `[SwaggerResponse]`, `[SwaggerSchema]` (Annotations) | `[EndpointSummary]`, `[EndpointDescription]`, `[ProducesResponseType]`, `[Description]`; no equivalent for several schema attributes | Mixed; `[SwaggerSchemaFilter]`, `[SwaggerSchema]` Manual |
| `MapType<T>`, `UseOneOfForPolymorphism`, `CustomSchemaIds`, `SelectSubTypesUsing` | schema transformer or none | Manual |
| `Swashbuckle.AspNetCore.Filters` (examples) | none, transformer by hand | Manual |
| Newtonsoft support package | System.Text.Json only | Manual |

The two generators produce **different documents** (schema naming, nullability, `oneOf`, enums, `$ref` reuse, 3.0 vs 3.1).
Code intent can be preserved, byte-identical output cannot.

## Behaviour proof idea

The proof format (`before` compiles against old, `after` against new) cannot show output equivalence. A real proof needs
a runnable sample (like `samples/SampleShop`): start the app before and after, fetch the document, compare semantically
(path set, operationIds, schema set, required lists, response codes) while tolerating known differences (3.0 vs 3.1
nullable, schema ids). That is a bespoke harness, needs build and run, so slow and CI-only.

## Corpus survey

Shallow clones (`--depth 1`) under `D:\playground\corpus-candidates\swashbuckle\<name>`; no builds. Counts are the
**number of files** matching a regex over `*.cs`, `*.csproj`, `*.props` (bin, obj, node_modules skipped). Script:
`D:\playground\corpus-candidates\tmp\m.sh`, raw output `out.txt` next to it. 14 repos cloned; 9 use Swashbuckle.

| Repo (pinned sha) | AddSwaggerGen | SwaggerDoc | SecDef/Req | OpFilter | SchFilter | DocFilter | XmlComments | UseSwaggerUI | Annotations | Filters pkg |
| ----------------- | -: | -: | -: | -: | -: | -: | -: | -: | -: | -: |
| `bitwarden/server` (`3327e2fdedeb`) | 6 | 3 | 2 | 4 | 5 | 4 | 1 | 2 | 0 | 0 |
| `jellyfin/jellyfin` (`c7ce6a1b56ee`) | 1 | 1 | 1 | 6 | 3 | 3 | 1 | 1 | 0 | 0 |
| `Altinn/altinn-auth` (`ab0bd8670b13`) | 10 | 5 | 8 | 10 | 22 | 2 | 3 | 10 | 39 | 22 |
| `abpframework/abp` (`5dc6dbbceb8d`) | 9 | 28 | 2 | 0 | 2 | 2 | 0 | 8 | 0 | 0 |
| `Letterbook/Letterbook` (`dace8fbfb156`) | 1 | 1 | 1 | 2 | 3 | 0 | 0 | 1 | 13 | 0 |
| `VMelnalksnis/Gnomeshade` (`0ae798923f67`) | 1 | 1 | 1 | 4 | 5 | 0 | 1 | 0 | 0 | 0 |
| `Kareadita/Kavita` (`2d225367f031`) | 1 | 1 | 1 | 0 | 2 | 0 | 1 | 1 | 0 | 1 |
| `Ombi-app/Ombi` (`697d35647bf8`) | 1 | 1 | 1 | 0 | 0 | 0 | 1 | 1 | 0 | 0 |
| `dotnet-architecture/eShopOnWeb` (`4da8212117e8`) | 1 | 1 | 1 | 0 | 2 | 0 | 0 | 1 | 3 | 0 |

No Swashbuckle (0 hits): `btcpayserver/btcpayserver` (`564d9d0ad1c5`), `neozhu/CleanArchitectureWithBlazorServer`
(`55e506946be7`), `davidtimovski/personal-assistant` (`ac7e1085c3db`), `Dotnet-Boxed/Templates` (`05951902c537`),
`skoruba/Duende.IdentityServer.Admin` (`06386cc726f1`, NSwag in 8 files). Caveats: `abp` wraps Swashbuckle in its own
`Volo.Abp.Swashbuckle` package; some "SecDef" hits may be the same file counted for several patterns; no commit dates
recorded. Versions seen in the corpus: 6.4.0, 6.5.0, 6.9.0, 8.1.4, 9.0.6, 10.1.7, 10.2.3, 10.3.0 (Annotations in 5 repos,
Filters in 3, ReDoc 1, Newtonsoft 1).

Annotation usage (altinn-auth, Letterbook, eShopOnWeb): `[SwaggerOperation]` 54, `[SwaggerSchemaFilter]` 21,
`[SwaggerSchema]` 11, `[SwaggerResponse]` 4, plus example providers.

Reading: the vanilla shape (`AddSwaggerGen` + `SwaggerDoc` + `UseSwagger` + `UseSwaggerUI`) is in all 9; 6 of 9 also have
custom operation, schema or document filters (Manual), and 8 of 9 a security definition. Only Ombi, Kavita and eShopOnWeb
are close to mechanical, and even they need a UI decision and change the document URL.

## Fixed vs Manual estimate

Fixed: package swap, `AddSwaggerGen()` to `AddOpenApi()`, `UseSwagger()` to `MapOpenApi()`, `IncludeXmlComments` removal,
simple `SwaggerDoc` info. Manual: UI (product choice), security schemes, every filter, annotations without equivalent,
examples, polymorphism, URL change. Expected: roughly 1 in 3 repos fully Fixed, the rest "Fixed skeleton + Manual list".

## Demand

High volume (Swashbuckle is the dominant generator in existing apps, 5.5k stars, active), but migration demand comes
from "new template defaults", not from license or EOL pressure. Teams can stay on 10.x at no cost.

## Effort

**L**. The skeleton is S, but the transformer model (Microsoft.OpenApi 2.x types), the UI choice and the lack of a cheap
equivalence proof push it to L.

## Recommendation

**No-go for now (defer).** Why: Swashbuckle is MIT and actively maintained (10.3.1), there is no license/sponsor/EOL
problem, most real repos have custom filters that stay Manual, and output equivalence cannot be proven with the existing
proof format. Revisit if Swashbuckle stalls again or a customer asks for ".NET 10 template alignment".

Top risks if built anyway: (1) different generated documents break API clients (URL, schema ids, 3.1); (2) filters are
bespoke and mostly Manual; (3) UI choice is not ours; (4) Microsoft.OpenApi 1.x/2.x type churn; (5) needs a run-time diff
harness.

### Draft task (if the council says go)

```
# TAAK-411 — Swashbuckle to Microsoft.AspNetCore.OpenApi fix (skeleton only)
Goal: fix `swashbuckle` with patterns AddSwaggerGen->AddOpenApi, UseSwagger->MapOpenApi, IncludeXmlComments removal;
filters, security, UI, annotations reported as Manual with reason.
Steps: 1 proofs + SampleShop API variant; 2 patterns; 3 Manual report entries; 4 document-diff harness note;
5 corpus run on Kavita, Ombi, eShopOnWeb, Letterbook.
```

## Council questions

1. Is a preference migration (no license/EOL trigger) in scope for Unshackle at all?
2. If yes, which UI: Scalar, or keep `Swashbuckle.AspNetCore.SwaggerUI` unchanged?
3. Is "skeleton Fixed + long Manual list" acceptable, or do we need filter-to-transformer rewrites?
4. Do we invest in a document-diff behaviour proof, or accept compile-only proofs?
5. Should Moq/Polly/EPPlus/MassTransit (real license/sponsor pressure) go first?

## See also

- `docs/projectinfo/architectuur/corpus-automapper.md` — model survey
- `claude/tasks/TAAK-410-scout-swashbuckle.md` — this scout's task
- `docs/projectinfo/project.md` — roadmap
