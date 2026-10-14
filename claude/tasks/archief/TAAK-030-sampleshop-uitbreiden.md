# TAAK-030 — Extend SampleShop with complex cases for all roadmap libraries

**Status**: done (to be reviewed, not merged)
**Created**: 2026-10-11
**Goal**: `samples/SampleShop` is a realistic user project in which FluentAssertions, MediatR and AutoMapper appear in their tricky forms, so that new patterns (and Manual reporting) can be tested against it.

## Context for the next agent

- Samples are "user code": their own `samples/Directory.*.props` and `.editorconfig`, not in `Unshackle.slnx`, not subject to our code standards.
- Versions pinned: FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0 (latest free versions). No new packages.
- `src/Unshackle.Tests/Integration/SampleShopTests.cs` runs `scan`/`fix` on a copy of `samples/`. `FixTwice` expects FluentAssertions to disappear everywhere after one manual fix.

## Analysis

### Current situation

Per library SampleShop only covers the simple cases: single `Should().X()`, one request/notification/behavior, one profile with `ForMember`/`ReverseMap`/`ProjectTo`.

### Problem

New patterns (MediatR → Mediator, AutoMapper, more complex FA) have no realistic target code. Nobody knows which real constructs currently silently become Manual.

### New situation

- New test project `SampleShop.Advanced.Tests` with the complex FA cases (`.And`, `.Which`, `because`, `AssertionScope`, `BeEquivalentTo` with options, async throws with `WithMessage`, custom assertions, records).
- SampleShop itself gets MediatR streams, generic handlers, multiple behaviors, pre/post processors, exception handlers, publish strategy, `Unit` returns, and AutoMapper resolvers, converters, nested/collections, `AfterMap`, assembly scan.
- Something for roadmap step 6 (generic search and replace): an `[Obsolete]` API with calls.

### Improvement

Every following pattern task immediately has real, green target code and a Manual list as input.

## Approach

### What

Target code + green tests in the sample. No change to Unshackle itself, except the integration test where the sample extension requires it.

### Where

- `samples/SampleShop/SampleShop/` — Domain, Dtos, Mapping, Features, Behaviors, Legacy.
- `samples/SampleShop/SampleShop.Tests/` — tests for the new MediatR/AutoMapper constructs (only simple FA, so that `FixTwice` keeps working).
- `samples/SampleShop/SampleShop.Advanced.Tests/` — complex FA (stays Manual, keeps FA in that project).
- `src/Unshackle.Tests/Integration/SampleShopTests.cs` — adapted to the extra project.

### How

Complex FA in a separate project, because those stay Manual and FluentAssertions therefore stays in that project; that way `FixTwice` keeps its meaning for `SampleShop.Tests`.

### Why this approach

One solution with everything, like a real user; the separate project separates "must migrate completely" from "must report Manual cleanly".

## Steps

- [x] 1. Task file
- [x] 2. MediatR constructs + tests
- [x] 3. AutoMapper constructs + tests
- [x] 4. Complex FA in `SampleShop.Advanced.Tests`
- [x] 5. Roadmap step 6: `[Obsolete]` API
- [x] 6. Checks: `dotnet build`, `dotnet test`, sample tests
- [x] 7. Fill in the Manual list below

## Risks & edge cases

- MediatR 12 registers pre/post processors and exception handlers automatically via assembly scan; generic handlers require `RegisterGenericHandlers = true`.
- After `fix` the sample must still build: a Fixed rewrite in the advanced project that does not compile is a bug in a pattern (note it, do not solve it here).

## Manual cases (input for new patterns)

Scan 2026-10-11: 59 Fixed, 15 Manual (was ≥20 Fixed, 1 Manual + project-file). Lines in `SampleShop.Advanced.Tests/AdvancedAssertionTests.cs`.

**Manual (pattern recognises, but refuses):**

- r47 `.NotBeEmpty().And.HaveCount(2).And.NotContainNulls()`: chain with `.And` (be-empty, have-count).
- r65 `ContainKey(id).WhoseValue...`: contain-key with `.WhoseValue`.
- r73/r74 `Be(25m, "… {0} …", args)` / `HaveCount(2, because: …, 2)`: becauseArgs/named.
- r96/r99/r109 `BeEquivalentTo(..., options => ExcludingMissingMembers/Excluding/WithStrictOrdering)`.
- r123 `(await ThrowAsync<T>().WithMessage(..)).Which.ParamName...`; r126 `ThrowExactlyAsync<T>().Where(..)`.
- r135 `Invoking(..).Should().Throw<T>().WithMessage(..).And.Message...`.
- r148 custom `order.Should().BeCancelled(..).And.NotBeNull()`.

**Not seen (no pattern, silently skipped):** `StartWith`/`EndWith`/`HaveLength`, `NotContainNulls`, `ContainSingle(..).Which`, `NotBe` (with because), `BePositive`, `Equal`, `ContainEquivalentOf`, `BeInAscendingOrder`, `AllBe`, `Invoking`, `NotBeNullOrWhiteSpace`, `BeOfType<T>().Which`, `BeApproximately`, `BeCloseTo`, `Match`/`MatchRegex`, custom `ReferenceTypeAssertions` extension, `AssertionScope`.

**Possible pattern bug:** r85-87 inside `using (new AssertionScope())` become Fixed (`ShouldBe`, `Count.ShouldBe`, `ShouldBePositive`), so the collecting behaviour of the scope silently disappears. This should probably be Manual (or `ShouldSatisfyAllConditions`).

**Integration test:** `FixTwice` in `src/Unshackle.Tests/Integration/SampleShopTests.cs` checked that FluentAssertions disappears from all project files. Because of the advanced project (which deliberately keeps Manual) that is no longer possible; the check is now: gone from `SampleShop.Tests.csproj`, stays in the advanced project and `Directory.Packages.props`. Purely a consequence of the new sample code.

The MediatR and AutoMapper fixes have no patterns yet: everything there is target code for later (streams, `RegisterGenericHandlers`, `TaskWhenAllPublisher`, pre/post processors, `IRequestExceptionHandler`, `IRequest` void + `IRequest<Unit>`, `ISender`/`IMediator` injection; `IValueResolver`, `ITypeConverter`, `IValueConverter`, `AfterMap`, `Ignore`, nested + `ReverseMap`, collections, assembly scan).

## Changed files

- `samples/SampleShop/SampleShop/`: `Domain/Customer.cs`, `Dtos/CustomerDto.cs`, `Mapping/CustomerProfile.cs`, `Data/CustomerRepository.cs`, `Features/Customers/*`, `Features/Common/CountQuery.cs`, `Features/Orders/CancelOrderCommand.cs`, `Features/Orders/OrderFacade.cs`, `Behaviors/{ValidationBehavior,StreamLoggingBehavior,Processors}.cs`, `Legacy/PriceCalculator.cs`, `ServiceCollectionExtensions.cs`
- `samples/SampleShop/SampleShop.Tests/{PipelineTests,CustomerMappingTests}.cs`
- `samples/SampleShop/SampleShop.Advanced.Tests/` (new), `SampleShop.slnx`
- `src/Unshackle.Tests/Integration/SampleShopTests.cs`

## See also

- `claude/tasks/patroon-queue.md`
- `docs/projectinfo/project.md` (roadmap)
- `src/Unshackle.Tests/Integration/SampleShopTests.cs`
