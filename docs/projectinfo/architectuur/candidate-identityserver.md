# Candidate: IdentityServer4 (EOL) to OpenIddict or Duende

Scout for TAAK-380, measured 2026-10-22. Light survey: web sources, shallow clones, regex counts, no builds.
Final go/no-go is a council decision (`claude/docs/council.md`).

## Recommendation

**No-go for an automatic `identityserver` fix. Go for a small report-only detector (effort S).**

- IdentityServer4 to **OpenIddict** is a redesign, not a rewrite: different pipeline, no UI, different store schema, different
  claim and resource model. Nearly every shape is `Manual`, and a wrong automatic rewrite fails silently as a security hole.
- IdentityServer4 to **Duende** is mostly a package and namespace rename plus a database migration, but Duende is the
  commercial product. Unshackle exists to leave license problems; moving to Duende swaps an EOL problem for a license
  problem (free only under the Community Edition limits). It can only be an opt-in choice by the user.
- Demand is large (see below), so a report that finds, counts and classifies every IS4 usage site is worth having.

## Facts (primary sources)

| Fact | Source |
| ---- | ------ |
| IdentityServer4 is archived, unmaintained, "went out of support when .NET Core 3.1 end of support was reached (13th Dec 2022)"; README states it "contains multiple known security vulnerabilities and bugs" | https://github.com/IdentityServer/IdentityServer4 (archived; last push 2025-03-06) |
| NuGet `IdentityServer4` 4.1.2 (last updated 2021-03-18) is deprecated: "legacy and has critical bugs", alternative Duende.IdentityServer; `IdentityServer4.EntityFramework` is deprecated and unlisted, advisory GHSA-ff4q-64jc-gx98 | https://www.nuget.org/packages/IdentityServer4 , https://www.nuget.org/packages/IdentityServer4.EntityFramework |
| Downloads: `IdentityServer4` 69.3M, `IdentityServer4.EntityFramework` 23.4M. For comparison `Duende.IdentityServer` 28.2M (8.0.10) and `OpenIddict.AspNetCore` 22.5M (7.7.2), NuGet search API 2026-10-22 | NuGet |
| Duende IdentityServer is a commercial fork of IS4. Free for development, test, personal use. Community Edition: for-profit under USD 1M projected annual gross revenue and under USD 3M capital access; non-profit budget under USD 1M; own infrastructure only, no redistribution or SaaS. Paid: Standard, Advanced, Custom | https://duendesoftware.com/products/communityedition |
| Duende v8.0 targets .NET 10 only; package/namespace rename `IdentityServer4` to `Duende.IdentityServer`, `IdentityModel` to `Duende.IdentityModel`; new EF migrations (`Keys`, `IdentityProviders`, new client columns, redirect URI length 2000 to 400); `CancellationToken` required on all store and service methods; `IClientStore.GetAllClientsAsync` new; `ICache` to `HybridCache`; `IClock` to `TimeProvider`; production needs a license | https://docs.duendesoftware.com/identityserver/upgrades/identityserver4-to-duende-identityserver-v7/ |
| OpenIddict is Apache 2.0, "a framework that requires writing custom code", not turnkey. Client, server and validation stacks; EF Core, EF6, MongoDB stores; ASP.NET Core Identity optional; event-handler pipeline; pass-through mode for custom endpoints. Does not document IS4 migration | https://github.com/openiddict/openiddict-core , https://documentation.openiddict.com/introduction |

Not verified from a primary source: the exact OpenIddict license file text (the README says Apache) and Duende paid prices
(not on the Community page).

## API distance

| IS4 shape | Duende | OpenIddict | Class |
| --------- | ------ | ---------- | ----- |
| Package refs `IdentityServer4*`, `using IdentityServer4.*`, `IdentityModel` | rename only | n/a | Fixed (Duende) / Manual |
| `AddIdentityServer().AddInMemory*` / `new Client{...}`, `GrantTypes.*`, `ApiResource`, `ApiScope`, `IdentityResource` | same API (renamed) | `AddOpenIddict().AddServer()` plus `OpenIddictApplicationDescriptor` with string permissions (`Permissions.Endpoints.*`, `GrantTypes.*`, `Scopes.*`), scopes with resources; `ApiResource` becomes audience, no 1:1 | Fixed (Duende) / Manual (OpenIddict) |
| `AddConfigurationStore` / `AddOperationalStore` (EF) | same, plus new migrations | different entities (Applications, Authorizations, Scopes, Tokens); data migration needed | Manual (both need DB migration) |
| `AddSigningCredential`, `AddDeveloperSigningCredential` | still works, automatic key management optional | `AddSigningCertificate` plus separate encryption certificate; tokens encrypted by default | Manual |
| `IProfileService` (claims in tokens) | same, plus CancellationToken | claims need `SetDestinations` per claim in the token-issuing code | Manual |
| `IResourceOwnerPasswordValidator`, `IExtensionGrantValidator`, `ICustomTokenRequestValidator` | same (ROPC deprecated in OAuth 2.1) | custom event handlers or controller logic | Manual |
| Custom `IClientStore`, `IResourceStore`, `IPersistedGrantStore`, `ICorsPolicyService` | add CancellationToken and `GetAllClientsAsync` | replaced by `IOpenIddict*Store` | Manual |
| UI: `IIdentityServerInteractionService` (login, consent, logout) | same | no equivalent; user writes authorize/logout controllers | Manual |
| API side: `AddIdentityServerAuthentication`, `IdentityServer4.AccessTokenValidation` (also deprecated) | `AddJwtBearer` | `AddOpenIddict().AddValidation()` (encrypted tokens need the shared key) | Manual |

## Survey (11 repos with usage, plus 1 package-only, plus 1 dropped)

Clones in `D:\playground\corpus-candidates\identityserver\src\<name>\` (depth 1, pinned sha, outside repo). Counts from
`scratchpad` regex script over `*.cs` (comments stripped, `bin/obj/node_modules` skipped); indicative only. `cs` = file count.
Columns: AddIS = `AddIdentityServer(`; Mem = `AddInMemoryClients`; Res = `AddInMemoryApiResources/ApiScopes`; EF = `AddConfigurationStore/OperationalStore`;
Id = `AddAspNetIdentity`; PS = `IProfileService`; ROPC = `IResourceOwnerPasswordValidator`; Ext = `IExtensionGrantValidator`;
Val = `ICustomToken/AuthorizeRequestValidator`; Str = custom store/CORS interfaces (including usages); UI = `IIdentityServerInteractionService`;
ATV = AccessTokenValidation; Cli = `new Client`; GT = `GrantTypes.`.

| Repo (sha, last commit, stars) | IS4 version | cs | AddIS | Mem | Res | EF | Id | PS | ROPC | Ext | Val | Str | UI | ATV | Cli | GT |
| ------ | ---- | -- | ----- | --- | --- | -- | -- | -- | ---- | --- | --- | --- | -- | --- | --- | -- |
| skoruba/IdentityServer4.Admin (fdb4cbb8c2, 2024-12, 3571) | 4.1.2 (EF, AspNetId) | 914 | 5 | 0 | 0 | 4 | 2 | 0 | 0 | 0 | 0 | 12 | 16 | 0 | 3 | 22 |
| mastreeno/Merp (46fc0822f1, 2021-04, 503) | 3.1.3 / 4.1.1 | 658 | 2 | 0 | 0 | 4 | 2 | 0 | 0 | 0 | 0 | 8 | 12 | 10 | 12 | 10 |
| DigitalExcellence/dex-backend (324f6b9e7e, 2022-01, 21) | 3.1.0 | 446 | 1 | 1 | 1 | 0 | 0 | 2 | 0 | 0 | 0 | 17 | 10 | 1 | 7 | 8 |
| stulzq/IdentityServer4.Samples (410a627d36, 2019-04, 602) | 2.1 to 2.4 | 345 | 15 | 14 | 14 | 2 | 1 | 1 | 1 | 0 | 0 | 108 | 72 | 5 | 39 | 39 |
| alugili/ModernArchitectureShop (8f92df177f, 2020-12, 280) | 3.1.3 | 282 | 1 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 18 | 10 | 0 | 1 | 1 |
| marcelooliveira/AspNetCore.Microsservicos (64b852a282, 2019-08, 76) | 2.2 / 2.3.1 | 274 | 1 | 1 | 1 | 0 | 1 | 2 | 0 | 0 | 0 | 10 | 8 | 4 | 3 | 1 |
| RockSolidKnowledge/Samples.IdentityServer4.WsFederationIntegration (09153d5169, 2025-03, 1) | 4.1.2 and Duende 7.1.0 | 127 | 4 | 4 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 30 | 48 | 0 | 4 | 0 |
| Nordes/IdentityServer4.LdapExtension (f5abf18c5a, 2021-04, 205) | 4.1.2 | 85 | 1 | 1 | 2 | 0 | 0 | 2 | 1 | 2 | 1 | 8 | 10 | 0 | 19 | 18 |
| damienbod/IdentityServer4VueJs (9519ceb3d3, 2022-02, 127) | 4.1.2 | 82 | 1 | 1 | 2 | 0 | 1 | 3 | 0 | 0 | 0 | 6 | 8 | 2 | 1 | 1 |
| KevinDockx/OpenIDConnectInDepth (87c9c88df6, 2019-10, 65) | 3.0.1 | 43 | 1 | 1 | 1 | 1 | 0 | 0 | 0 | 1 | 0 | 10 | 8 | 4 | 3 | 2 |
| macsux/IdentityServer4TokenExchange (ea8b06d5c0, 2018-12, 0) | no csproj hit | 35 | 1 | 0 | 0 | 0 | 0 | 1 | 1 | 2 | 0 | 19 | 10 | 0 | 0 | 0 |
| anjoy8/ChristDDD (e450254257, 2021-01, 699) | 3.1.3 (package ref only) | 130 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Dropped: `dotnet-architecture/eShopOnContainers` (0d329b713e) no longer contains source on its default branch (README only).
`IdentityServer/IdentityServer4.Samples` does not exist any more.

Observations:

- Every real usage site uses `AddIdentityServer` plus a config of clients and scopes (the Fixable-by-rename part), and nearly
  all have UI/interaction code (the `UI` column) that has no OpenIddict equivalent.
- Custom grant validators or profile services appear in 6 of 11 repos; EF stores in 4 of 11. These are the Manual parts.
- Most repos are demos and frozen at IS4 2.x to 4.1.2, so the corpus mostly proves the rename path, not production behaviour.
  Production IS4 deployments are closed-source servers; a corpus cannot cover their custom validators and stores.
- Only one repo (RockSolid sample) already shows the IS4 to Duende move (both packages side by side).

## Fixed vs Manual sketch

- **Duende (opt-in)**: Fixed: package ids and versions, `using` namespaces (including `IdentityModel` to `Duende.IdentityModel`),
  `CancellationToken` added to custom store signatures (needs token plumbing: Manual if no token is in scope). Manual: target
  framework to .NET 10, EF migrations, license key setup, new required `GetAllClientsAsync`, UI changes.
- **OpenIddict**: Fixed: almost nothing safely. Possibly the descriptor for an in-memory client list with plain grant types. All
  of profile service, validators, stores, UI, token validation, certificates and database data are Manual.

## Proving a migration safe

Unshackle's usual proof (before compiles against old library, after against new) shows only that it compiles. For an auth server
the failure mode is a running server that issues wrong tokens, so a compile proof is not enough. Needed:

1. Protocol conformance tests run against both servers (`WebApplicationFactory`): discovery document, each grant type per client,
   scope and audience in the issued token, claim set per profile, refresh token rotation, redirect URI validation, PKCE enforcement.
   Diff the decoded token claims before and after.
2. Data migration test for the EF stores: seed old schema, run the migration, compare clients, secrets (hashing) and persisted grants.
3. Secure-by-default checks in the report: flag ROPC, implicit flow, missing PKCE, `AddDeveloperSigningCredential`, wildcard CORS.
4. The corpus proof must not be a corpus of demos; it needs at least one real server and a human review gate.

That is a test product larger than the fix itself, and it cannot be built with only source rewriting.

## Demand, effort

- Demand: very high. IS4 69.3M plus EF 23.4M downloads, deprecated and flagged with a security advisory, so every
  scanner (Dependabot, NuGet audit) reports it. But the users who still run it are mostly company auth servers with custom code.
- Effort: report-only detector S (count sites, classify per table above, link docs); Duende rename Fix M (Fixed rename plus
  report items, needs license decision per user); OpenIddict Fix L to XL, not recommended.

## Top risks

1. A silent security regression (claims, audience, lifetime, redirect validation) after an automatic rewrite.
2. Licensing: Duende is commercial, so "fixing" with it only moves the problem; the Community Edition thresholds are narrow.
3. Production data (clients, secrets, grants, signing keys) migration is outside what a source rewriter can do.
4. Corpus is weak (demos, 2018-2022); no real production server to prove against.

## Draft task (if the council says go for the report-only detector)

```markdown
# TAAK-381 — identityserver report-only fix (IdentityServer4 findings)

**Status**: not started
**Goal**: `--fix identityserver` that finds IdentityServer4 usage and emits a Manual-only report (no rewrites), with a mapping
table to Duende and OpenIddict and security flags (ROPC, implicit, no PKCE, developer signing credential).

## Steps
- [ ] 1. IFix `identityserver` with detect-only patterns for the shapes in `candidate-identityserver.md` (AddIdentityServer,
      in-memory config, EF stores, IProfileService, validators, custom stores, interaction service, AccessTokenValidation)
- [ ] 2. Each pattern returns `Manual(reason)` with the replacement guidance for Duende and OpenIddict
- [ ] 3. Proofs (`*.manual.txt`) plus KingmaDoc plan and verify per pattern, README row
- [ ] 4. Run on the corpus in `D:/playground/corpus-candidates/identityserver/`; report counts
```

## Council questions

1. Is a report-only fix acceptable for a library (all patterns Manual)? Do we want that precedent?
2. May Unshackle target Duende at all, given it is commercial (Community Edition limits)? If yes, only opt-in?
3. Is a protocol-conformance harness (two servers, token diff) in scope, or do we refuse security-critical automatic rewrites?
4. Should Moq, Polly, EPPlus and MassTransit go first (higher Fixed share, easier proofs)?

## See also

- `docs/projectinfo/project.md` — roadmap
- `docs/projectinfo/architectuur/corpus-automapper.md` — model survey
- `claude/tasks/TAAK-380-scout-identityserver.md` — this scout task
- `claude/tasks/CLAIMS.md` — file claims and reserved numbers
