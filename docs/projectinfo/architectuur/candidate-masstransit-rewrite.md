# Candidate rewrite: MassTransit v8 → an open-source bus

Feasibility study for TAAK-310 (roadmap step 10), measured 2026-10-28. Question: is a real code rewrite from MassTransit v8 to an
open-source bus feasible and safe, or should Unshackle stay report-only? Facts come from the real NuGet packages (nuspec, README,
XML docs, reflection on the assemblies, downloaded to `<corpus>/tmp/mtstudy/`); frequencies from `claude/tasks/CORPUSRUN-masstransit.md`.

## Recommendation

**No-go for a rewrite to another bus; go for staying on MassTransit 8.5.x plus report-only; conditional go for a package-ID swap to
an API-identical fork once one ships on NuGet.** Decided unanimously in council round 24 (`claude/tasks/COUNCIL-round-24.md`).
None of the candidate targets keeps delivery semantics, so under the round-8 rule ("Fixed means behaviour proven the same") nothing
of substance could be Fixed, and a partial rewrite leaves an app that compiles but receives nothing.

## Licence situation (primary sources)

| Fact | Source |
| --- | --- |
| MassTransit 8.5.12 is the latest 8.x on NuGet; licence expression `Apache-2.0` | nuspec of `masstransit/8.5.12`; `https://api.nuget.org/v3-flatcontainer/masstransit/index.json` |
| MassTransit 9.0.0 .. 9.2.4: no licence expression, `licenseUrl` `https://massient.com/license`; package README: commercial product that must be licensed, earlier versions remain permissive OSS | nuspec + `NuGet.README.md` of `masstransit/9.2.4`, nuspec of `9.0.0` |
| v8 end of support "end of 2026": third-party claim only | `docs/projectinfo/architectuur/corpus-masstransit.md` |
| OpenTransit: no NuGet package (flat container 404 on 2026-10-28) | `https://api.nuget.org/v3-flatcontainer/opentransit/index.json` |
| PublicTransit: 8.4.0, 8.5.1, 8.5.2 only (behind 8.5.12) | `https://api.nuget.org/v3-flatcontainer/publictransit/index.json` |
| WolverineFx 6.56.0 `MIT`, lib `net9.0`/`net10.0` only | nuspec |
| Rebus 8.9.5 `MIT`, lib `netstandard2.0` .. `net10.0` | nuspec |
| Paramore.Brighter 10.8.1 `MIT`, lib `netstandard2.0`, `net8.0` .. `net10.0` | nuspec |

Consequence: a user who pins `MassTransit < 9` stays on Apache-2.0 code. There is no licence pressure to move today; the pressure is
the future end of v8 patches.

## Semantic distance per target (checked in the packages)

| Concept | Wolverine 6.56 | Rebus 8.9.5 | Brighter 10.8.1 |
| --- | --- | --- | --- |
| Publish | `IMessageBus.PublishAsync`: "ignores the message if there are no known subscribers" | `IBus.Publish`: topic = assembly-qualified type name; consumers need `IBus.Subscribe` | `IAmACommandProcessor.Publish` is in-process; external bus is `Post`/`DepositPost` |
| Send | `SendAsync`: expects at least one subscriber | `IBus.Send`: to the address mapped as owner of the type (routing config) | `Send` in-process, `Post` external |
| Message types | plain classes | plain classes | `TRequest : class, IRequest` (with `Id`) on `Post`/`Publish`/`Send`; `IAmAMessageMapper<T>` per external type |
| Request/response | `InvokeAsync<T>(msg, ct, timeout)` | `Reply`; request client only in `Rebus.Async` | `Call<T,TResponse>` with `ICall`/`IResponse` |
| Handler | convention `Handle` method | `IHandleMessages<T>.Handle(T)` | `RequestHandlerAsync<T>` |
| Saga | `Wolverine.Saga` base class | `Saga<TData>`, `IAmInitiatedBy<T>`, `CorrelateMessages` | none built in |

Every row differs in routing, delivery or contract types, not just in syntax. Wolverine also excludes every project below net9.

## Patterns: semantically equal vs principally Manual

Corpus round 1 (19 repos): consumer 129 (98 `ConsumerFilter` Manual, 14 simple Info), publish-send 107, request-client 72,
transport 66, registration 54, outbox 18, test-harness 17, consumer-definition 10, retry 9, saga 1.

| Pattern | Toward another bus | Toward an API-identical fork |
| --- | --- | --- |
| Simple consumer (14) | Manual: handler shape maps, but retry, concurrency and error-queue behaviour change, and it is useless without registration | Fixed (package ID only) |
| Consumer with filter / definition / context feature (115) | Manual | Fixed (package ID only) |
| Publish / send (107) | Manual: "no subscriber" and routing behaviour differ in all three targets (T4: a routing change is never Fixed) | Fixed |
| Request client (72) | Manual: timeout, fault and response contract differ | Fixed |
| Registration, transport, outbox, retry, test harness, saga | Principally Manual | Fixed, only after an API diff against 8.5.x |

No pattern is semantically equal toward Wolverine, Rebus or Brighter. A consumers-only rewrite under AllOrNothing-per-project keeps the
old package (registration is Manual) and gives two buses or unwired consumers: rejected (T3).

## Council questions (answered in round 24)

1. Q24.1 Go, conditional go or no-go for a real rewrite now?
2. Q24.2 Target: Wolverine, Rebus, Brighter, or stay on v8 with a later fork swap?
3. Q24.3 Which patterns could ever be Fixed?
4. Q24.4 Re-check triggers and date.
5. Q24.5 Is a partial (consumers-only) rewrite ever acceptable?

## Not checked

- No builds or run-time proofs against Wolverine/Rebus/Brighter; the semantic claims rest on XML docs and generic constraints.
- OpenTransit package IDs and API: unknown until a release.

## See also

- `claude/tasks/COUNCIL-round-24.md` — the council round
- `docs/projectinfo/architectuur/corpus-masstransit.md` — first feasibility study and survey
- `claude/tasks/CORPUSRUN-masstransit.md` — corpus frequencies
- `claude/tasks/TAAK-310-masstransit-v8.md` — the task
