# Corpus: MassTransit 8 feasibility and API usage in 24 open-source projects

Measured on 2026-10-22 for TAAK-310 steps 1–2. Source-level only: nothing was built. Go/no-go and the migration target
are council questions (see the end of this document); this document only gathers facts.

## Feasibility (step 1)

### License and v8 support end

- MassTransit v9 is commercial (Massient, Inc.); the NuGet listing of v9 says it "must be licensed" and that earlier
  versions stay under a permissive open-source license. v8 is Apache-2.0. Latest v8 on NuGet: **8.5.12**; 8.5.10 was
  published 2026-06-04, so v8 still gets patch releases. Latest v9: 9.2.3.
  Sources: <https://www.nuget.org/packages/MassTransit>, <https://masstransit.massient.com/support>.
- **The "end of 2026" v8 support date is not confirmed by a primary source.** The v9 announcement page
  (<https://masstransit.massient.com/introduction/v9-announcement>) and the support page give no v8 end date; the
  MassTransit README and GitHub issues/discussions have none either. The only source for "end of 2026" is the
  OpenTransit announcement (third party, 2025-12-05,
  <https://dev.to/nakib/introducing-opentransit-a-free-open-source-fork-of-masstransit-v8-2eb3>). Treat it as
  "reportedly". Related hard date: .NET 8 and 9 reach end of support on 2026-11-10.

### OpenTransit (v8 community fork)

- Repo <https://github.com/OpenTransitLab/OpenTransit>, created 2025-10-24, Apache-2.0, 130 stars, last push 2026-08-12.
- **Status: not released.** No GitHub releases, no NuGet package (`OpenTransit` does not exist on nuget.org). Site
  (<https://opentransitlab.github.io/OpenTransit/>) says it will publish only once v8 reaches end of support, will target
  **.NET 10+ only**, and currently accepts documentation contributions only. It advises staying on MassTransit v8 in
  production until then.
- **API compatibility: identical today.** The repo is a straight fork: projects are still `src/MassTransit*`, namespaces
  `MassTransit`, the README is upstream's, and recent commits are "Merge branch 'MassTransit:develop' into develop".
  Package IDs and namespaces after a release are unannounced.
- **Maintainers: unnamed.** The GitHub contributor list is upstream history (phatboyg, drusellers, ...); the only named
  person is the post author (S. Nakib, "contributor"). "Will not rely on a single maintainer" is a goal, not a fact yet.
- Second fork: **PublicTransit** on NuGet (<https://www.nuget.org/profiles/PublicTransit>), 8.5.2, "public,
  non-commercial fork", last update July 2025: not tracking v8 patches.
- Consequence for Unshackle: a fork swap would be a package-ID swap (namespaces likely unchanged) and does not need
  Roslyn rewrites at all, unless the fork renames namespaces. Today there is nothing to swap to.

### API distance to Wolverine (WolverineFx 6.x) and Rebus (8.9.x)

| Concept | MassTransit 8 | Wolverine | Rebus | Mechanical? |
| --- | --- | --- | --- | --- |
| Consumer | `class C : IConsumer<T>` + `Task Consume(ConsumeContext<T> ctx)`, message via `ctx.Message` | convention handler: any `Handle(T msg, ...)` method, DI services as parameters, no interface | `IHandleMessages<T>` + `Task Handle(T msg)`; context via injected `IMessageContext` | Rebus: close (interface + signature swap, `ctx.Message` → `msg`); Wolverine: signature reshape. Both Manual when the consumer uses `ctx.Headers`, `ctx.Redeliver`, `ctx.RespondAsync`, etc. |
| Publish / send | `IPublishEndpoint.Publish`, `ISendEndpointProvider.GetSendEndpoint(uri).Send`, `ctx.Publish` | `IMessageBus.PublishAsync` / `SendAsync`, or cascading return values | `IBus.Publish` / `IBus.Send` (routing by type map) | Publish: near 1:1. Send with explicit `queue:` URIs: Manual (routing lives in config). |
| Request/response | `IRequestClient<TReq>.GetResponse<TRes>()`, `ctx.RespondAsync` | `IMessageBus.InvokeAsync<TRes>(req)`, handler returns the response | `Rebus.Async` `SendRequest<TRes>` + `bus.Reply` (separate package, discouraged by the author) | Wolverine: plausible for the simple single-response shape; multi-response `GetResponse<A,B>` Manual. |
| Sagas | `MassTransitStateMachine<TState>` (Automatonymous DSL: `Event`, `State`, `Initially/During/When/TransitionTo`), or `ISaga` consumers | `Saga` subclass with `Start`/`Handle` methods, state is the class itself | `Saga<TData>` + `IAmInitiatedBy<T>`, `CorrelateMessages` | **Manual**: no structural mapping from the state-machine DSL. |
| DI registration | `services.AddMassTransit(x => { x.AddConsumer<C>(); x.UsingRabbitMq((ctx,cfg) => { cfg.Host(...); cfg.ConfigureEndpoints(ctx); }); })` | `builder.UseWolverine(opts => { opts.UseRabbitMq(...); ... })` (host-level, handler discovery by convention) | `services.AddRebus(c => c.Transport(t => t.UseRabbitMq(cs, queue)).Routing(...))`, `AutoRegisterHandlersFromAssemblyOf<T>` | Manual: transport topology, endpoint naming, retry, outbox are semantics. Could be emitted as a TODO skeleton. |
| Retry / outbox / filters | `UseMessageRetry`, `AddEntityFrameworkOutbox`, `IFilter<>` | own policies, durable inbox/outbox (EF Core, Postgres, SQL Server) | `RetryStrategy`, outbox in `Rebus.SqlServer` etc. | Manual. |
| Testing | `ITestHarness` / `AddMassTransitTestHarness` | `TrackActivity()` / `InvokeMessageAndWaitAsync` | `FakeBus`, `SagaFixture` | Manual. |

Sources: Wolverine docs <https://wolverinefx.net/guide/messaging/> and <https://wolverinefx.net/guide/durability/sagas.html>,
Rebus wiki <https://github.com/rebus-org/Rebus/wiki>, MassTransit docs <https://masstransit.massient.com/>. Licenses:
Wolverine MIT (JasperFx), Rebus MIT; Rebus "Pro" add-ons are commercial but not needed for the above.

## Survey (step 2)

### Method

- 24 repos found by GitHub code search for `PackageReference`/`PackageVersion Include="MassTransit" Version="8`
  (list: `claude/tools/corpus-masstransit-repos.txt`), downloaded at `HEAD` (2026-10-22) as tarballs into
  `<corpus>/corpus-masstransit/<owner>_<repo>/`. Not pinned, not built.
- Counts are regex occurrences over `*.cs` (no comment stripping, no Roslyn). `Publish`/`Send` are **noisy**: they also
  catch MediatR `Send`, SignalR, other buses (`Nox` 2296 and `YourBrand` 825 `Send` hits are mostly not MassTransit).
  Read them as "possible", the rest as reliable to ±10%.

### Per repo

| Repo | MT versions | files `using MassTransit` | `IConsumer<` | `ConsumerDefinition<` | `.Publish(` | `.Send(` | `IRequestClient<` | `RespondAsync` | state machine | `AddMassTransit` | `AddConsumer` | RabbitMQ | Azure SB | in-memory | outbox | retry | filters | test harness |
| --- | --- | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: |
| Altinn/altinn-auth | 7.3.1, 8.5.11 | 2 | 0 | 0 | 0 | 20 | 0 | 0 | 0 | 1 | 0 | 0 | 1 | 1 | 25 | 0 | 0 | 0 |
| AntonioFalcaoJr/Reminders | 8.1.0 | 4 | 1 | 0 | 1 | 1 | 0 | 0 | 0 | 1 | 1 | 1 | 0 | 0 | 0 | 0 | 2 | 0 |
| ardalis/VerticalCleanModularMicroservices | 8.3.4 | 5 | 1 | 0 | 9 | 63 | 0 | 0 | 0 | 2 | 1 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| baranacikgoz/modular-monolith-ddd-vsa-webapi | 8.5.11 | 18 | 2 | 1 | 1 | 0 | 0 | 1 | 0 | 1 | 2 | 1 | 0 | 1 | 0 | 2 | 0 | 0 |
| EdwinVW/cloud-native-net | 8.0.7 | 4 | 2 | 0 | 5 | 0 | 0 | 0 | 0 | 5 | 3 | 3 | 0 | 0 | 0 | 1 | 0 | 0 |
| elsa-workflows/elsa-extensions | 8.5.7 | 36 | 19 | 2 | 15 | 7 | 0 | 0 | 0 | 31 | 12 | 3 | 3 | 3 | 2 | 0 | 4 | 0 |
| evolutionary-architecture/…-by-example | 8.3.2 | 24 | 4 | 2 | 18 | 6 | 0 | 0 | 0 | 8 | 6 | 4 | 0 | 0 | 14 | 2 | 0 | 22 |
| firebend/auto-crud | 8.5.3 | 15 | 1 | 0 | 2 | 0 | 0 | 0 | 0 | 1 | 1 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| firebend/jobba | 8.5.11 | 20 | 2 | 0 | 1 | 0 | 1 | 2 | 0 | 4 | 1 | 2 | 0 | 6 | 0 | 1 | 0 | 4 |
| gncyyldz/Saga-Orchestration | 7.3.1, 8.0.14 | 5 | 5 | 0 | 0 | 11 | 0 | 0 | 1 | 4 | 5 | 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| iblazhko/eventsourced-processmanager | 8.5.8 | 7 | 48 | 0 | 10 | 0 | 0 | 0 | 0 | 3 | 4 | 1 | 0 | 0 | 0 | 1 | 0 | 0 |
| iongritco/ModularMonolith | 8.5.7 | 1 | 2 | 0 | 3 | 14 | 0 | 0 | 0 | 3 | 1 | 0 | 0 | 1 | 0 | 0 | 0 | 0 |
| jokk-itu/MassTransit-Outbox-Sample | 8.0.4-develop | 1 | 1 | 1 | 1 | 3 | 0 | 0 | 0 | 1 | 1 | 1 | 0 | 0 | 4 | 0 | 0 | 0 |
| Letterbook/Letterbook | 8.5.10 | 28 | 4 | 3 | 40 | 0 | 0 | 0 | 0 | 12 | 6 | 0 | 0 | 2 | 0 | 2 | 0 | 18 |
| marinasundstrom/YourBrand | 8.2.0 | 80 | 96 | 0 | 49 | 825* | 45 | 24 | 0 | 40 | 44 | 28 | 7 | 3 | 0 | 0 | 12 | 9 |
| NikiforovAll/social-media-app-aspire | 8.2.2 | 4 | 2 | 0 | 2 | 0 | 0 | 0 | 0 | 1 | 2 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| nkz-soft/microservice-template | 7.3.1, 8.3.0 | 0 | 2 | 0 | 3 | 21 | 0 | 0 | 0 | 3 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| NoxOrg/Nox | 8.3.0 | 16 | 0 | 0 | 5 | 2296* | 0 | 0 | 0 | 2 | 0 | 0 | 1 | 1 | 13 | 0 | 0 | 4 |
| onebeyond/monaco | 8.5.7 | 9 | 1 | 0 | 3 | 9 | 0 | 0 | 0 | 8 | 0 | 4 | 2 | 0 | 3 | 0 | 0 | 6 |
| sloweyyy/cloud-native-ecommerce-platform | 8.5.11 | 13 | 4 | 0 | 7 | 41 | 0 | 0 | 0 | 4 | 4 | 3 | 0 | 1 | 0 | 1 | 0 | 3 |
| SwissLife-OSS/Confix-Unloaded | 8.0.13 | 8 | 1 | 0 | 0 | 0 | 0 | 1 | 0 | 1 | 1 | 1 | 1 | 1 | 0 | 0 | 0 | 0 |
| thangchung/coffeeshop-on-nomad | 8.0.8 | 10 | 4 | 4 | 7 | 4 | 0 | 0 | 0 | 3 | 4 | 3 | 0 | 0 | 4 | 4 | 0 | 0 |
| thecodewrapper/CH.CleanArchitectureBlazor | 8.5.7, 9.0.1 | 7 | 2 | 0 | 1 | 0 | 0 | 6 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| xilapa/SiteWatcher | 8.2.2 | 8 | 3 | 0 | 3 | 18 | 0 | 0 | 0 | 2 | 5 | 1 | 0 | 0 | 4 | 1 | 0 | 4 |

`*` mostly non-MassTransit `Send`. 7.3.1 entries are other packages (e.g. old `MassTransit.Analyzers`/side projects);
`thecodewrapper` already references 9.0.1 in one project. No repo uses Amazon SQS or Courier (routing slips).

### Totals (repos using the feature / occurrences)

| Feature | Repos | Occurrences | Likely category |
| --- | --: | --: | --- |
| `IConsumer<T>` consumers | 22 / 24 | 207 (96 in YourBrand) | Rebus: Fixed candidate; Wolverine: reshape |
| `ConsumerDefinition<>` | 6 | 13 | Manual |
| `IRequestClient<>` / `RespondAsync` | 2 / 5 | 46 / 34 | Wolverine `InvokeAsync`: candidate; else Manual |
| Saga state machine | 1 | 1 | Manual |
| `AddMassTransit` registration | 23 | 141 | Manual (config) |
| RabbitMQ / Azure SB / in-memory | 18 / 5 / 10 | 64 / 15 / 20 | Manual (topology) |
| Outbox | 7 | 67 | Manual |
| Retry / redelivery | 10 | 15 | Manual |
| Filters | 3 | 18 | Manual |
| Test harness | 8 | 70 | Manual |

Reading: consumers + publish dominate the code volume; everything else is concentrated in `Program.cs`/DI modules. Sagas
are rare in this sample (1 repo), request/response is concentrated in one repo. A Wolverine/Rebus fix would therefore
rewrite consumer classes and publish calls and leave the registration as a Manual block per repo.

## Council questions

1. Target: (a) fork swap (OpenTransit, unreleased, package IDs unknown), (b) Wolverine, (c) Rebus, or (d) report-only scan
   (inventory + Manual for everything)? Fork swap has no Roslyn work but nothing to swap to today.
2. Timing: wait for an OpenTransit release (announced for "v8 end of support", itself only a third-party date) or
   build now?
3. If Wolverine/Rebus: is a consumer rewrite worth doing when the registration (in 23/24 repos) is always Manual and the
   app does not run until a human rewrites it? Or emit a generated TODO registration skeleton?
4. Is "Fixed" acceptable for a consumer whose semantics change (MassTransit endpoint per consumer vs. Wolverine
   convention routing), or must every rewrite carry a review note?
5. Unit-test types: MassTransit 8.5.x (Apache-2.0) is fine to pin; Wolverine (MIT) / Rebus (MIT) too. Confirm.

## See also

- `claude/tasks/TAAK-310-masstransit-v8.md` — the task
- `claude/tools/corpus-masstransit-repos.txt` — repo list
- `docs/projectinfo/architectuur/corpus-automapper.md` — model for this survey
- `claude/docs/council.md` — decision council
