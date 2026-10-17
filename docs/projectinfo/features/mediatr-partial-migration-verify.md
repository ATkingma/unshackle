---
kingmadoc: 1
feature: "mediatr-partial-migration"
status: verified
plan: mediatr-partial-migration-plan.md
---

# Verify: MediatR partial migration

|              |                                                                        |
| ------------ | ---------------------------------------------------------------------- |
| **Plan**     | [mediatr-partial-migration-plan.md](mediatr-partial-migration-plan.md) |
| **Verified** | 2026-10-17                                                             |

## Built vs planned

| Planned file                                           | Built                                                                                                                                                                                                                  |
| ------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Abstractions/IPartialFix.cs`, `Models/PartialPlan.cs` | as planned                                                                                                                                                                                                             |
| `Fixer.cs`                                             | `FixAsync` → `RunAsync` (one run) + `RunPartialAsync` (retry, max 4) + `KeepsKeptCodeAsync` (kept text unchanged) + `InOriginalAsync` (unmatched-use line in the original); kept nodes filtered in `ApplyPatternAsync` |
| `UsingDirectives.cs`, `ProjectPackages.cs`             | `IsGlobalQualified`: a `global::` name needs no using and keeps the package                                                                                                                                            |
| `MediatROwners.cs`                                     | as planned, plus `MediatROwner.cs` and `MediatROwnerGraph.cs` (union-find)                                                                                                                                             |
| `MediatRPartialTests.cs`                               | 6 tests (2 proof cases in `Proofs/MediatR/_Combined/Partial/`)                                                                                                                                                         |

Deviations from the plan, found while building:

- Kept references are **always** qualified: a file whose MediatR names all stay would otherwise lose them in the using swap
  (the swap is the whole migration of `INotification`; fullcart `Domain/Common/BaseEvent.cs`, build error CS0311).
- `IMediator`/`ISender`/`IPublisher` as a type argument or `typeof` in a statement (`AddTransient<ISender>(sp => ...)`) is
  part of MediatR's runtime and stays with its registration.
- Extra REQ-7 check: a handler that moves must be visible from the project that gets `AddMediator` (itself or a project it
  references), otherwise the generator would not register it.
- A body block is not an owner (only the statements in it), and a statement with `return` joins its member's signature.

## Requirements

| Req          | Evidence                                                                                                                               |
| ------------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1        | `FixAsync_MediatRRegisteredByHand_MovesRequestsAndKeepsNotifications`                                                                  |
| REQ-2        | `RunPartialAsync` + `KeepsKeptCodeAsync`; fullcart round 1 (a kept global using row was a root and gave up)                            |
| REQ-3, REQ-4 | `FixAsync_NothingCanMove_ChangesNothing` (one field sends and publishes), `generic-request-stays` (per message)                        |
| REQ-5        | all `after` texts compile against MediatR + Mediator + generator; fullcart `Domain.csproj` keeps MediatR next to Mediator.Abstractions |
| REQ-6        | inserted `AddMediatR` → `AddMediator(cfg => { cfg.ServiceLifetime = ServiceLifetime.Scoped; })` in every case                          |
| REQ-7        | `FixAsync_HandlerOutsideTheRegisteringProject_ChangesNothing`; starter, miru, SampleShop (AddMediatR) unchanged                        |
| REQ-8        | full suite green (1519), FA/AutoMapper and fully migrating MediatR repos unchanged                                                     |

Behaviour: `FixAsync_PartlyMigratedApp_RunsLikeTheOriginal` compiles, loads and runs the original and the partly migrated app:
both log `brand 7|before,handle 7,after,dispatch,placed 3` (request through Mediator with its DI behavior, notification
through MediatR with its decorator).

## Corpus (`after`: fix → build → own tests; tool from this branch, `<corpus>/corpus-blind/_results/274/run`)

| Repo          | Before (TAAK-271)             | Now                                                                                                                                                                             | Tests before → after | Regressions |
| ------------- | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------- | ----------: |
| fullcart      | 0 Fixed / 33 Manual (blocked) | **27 Fixed / 20 Manual** (15 "stays on MediatR", 2 roots, 3 package rows): requests, behaviors and controllers on Mediator; notifications, decorator and dispatchers on MediatR | 9/9 → 9/9            |           0 |
| starter       | 0 / 11                        | unchanged (root is `AddMediatR`)                                                                                                                                                | 45/49 → 45/49        |           0 |
| miru          | 0 / 98                        | unchanged (root is `AddMediatR`)                                                                                                                                                | 400/474 → 391/474    |         0 ¹ |
| realworld     | 10 / 0                        | unchanged                                                                                                                                                                       | 10/10 → 10/10        |           0 |
| mediatrsample | 25 / 0                        | unchanged                                                                                                                                                                       | 14/21 → 14/21        |           0 |
| cashflow      | 6 / 0                         | unchanged                                                                                                                                                                       | no tests             |           0 |

¹ The tool wrote no file; the 8 extra failures are Selenium tests whose Chrome session could not start (machine load).

## Open

- `AddMediatR(cfg => ...)` that has to stay is not split into two registrations (plan, out of scope): SampleShop and any
  app on `AddMediatR` with a blocking Manual still get all-or-nothing.
- A fully kept file whose kept code calls MediatR members (`_publisher.Publish`) gets `using Mediator;` it does not need
  (harmless for the build unless IDE0005 is an error).

## See also

- [mediatr-partial-migration-plan.md](mediatr-partial-migration-plan.md), `claude/tasks/archief/TAAK-274-mediatr-partial-migration.md`
