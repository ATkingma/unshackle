# TAAK-030 — SampleShop uitbreiden met complexe gevallen voor alle roadmap-bibliotheken

**Status**: klaar (te reviewen, niet gemerged)
**Aangemaakt**: 2026-10-11
**Doel**: `samples/SampleShop` is een realistisch gebruikersproject waarin FluentAssertions, MediatR en AutoMapper in hun lastige vormen voorkomen, zodat nieuwe patronen (en Manual-rapportage) erop getest kunnen worden.

## Context voor de volgende agent

- Samples zijn "gebruikerscode": eigen `samples/Directory.*.props` en `.editorconfig`, niet in `Unshackle.slnx`, niet onder onze code-standaarden.
- Versies vast: FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0 (laatste vrije versies). Geen nieuwe pakketten.
- `src/Unshackle.Tests/Integration/SampleShopTests.cs` draait `scan`/`fix` op een kopie van `samples/`. `FixTwice` verwacht dat FluentAssertions na één handmatige fix overal verdwijnt.

## Analyse

### Huidige situatie

SampleShop dekt per bibliotheek alleen de eenvoudige gevallen: losse `Should().X()`, één request/notification/behavior, één profiel met `ForMember`/`ReverseMap`/`ProjectTo`.

### Probleem

Nieuwe patronen (MediatR → Mediator, AutoMapper, complexere FA) hebben geen realistische doelcode. Niemand weet welke echte constructies nu stil Manual worden.

### Nieuwe situatie

- Nieuw testproject `SampleShop.Advanced.Tests` met de complexe FA-gevallen (`.And`, `.Which`, `because`, `AssertionScope`, `BeEquivalentTo` met options, async throws met `WithMessage`, custom assertions, records).
- SampleShop zelf krijgt MediatR-streams, generieke handlers, meerdere behaviors, pre/post-processors, exception handlers, publish-strategie, `Unit`-returns, en AutoMapper-resolvers, converters, nested/collecties, `AfterMap`, assembly-scan.
- Iets voor roadmap-stap 6 (generiek zoek-en-vervang): een `[Obsolete]` API met aanroepen.

### Verbetering

Elke volgende patroontaak heeft direct echte, groene doelcode en een Manual-lijst als input.

## Aanpak

### Wat

Doelcode + groene tests in de sample. Geen wijziging aan Unshackle zelf, behalve de integratietest waar de sample-uitbreiding dat afdwingt.

### Waar

- `samples/SampleShop/SampleShop/` — Domain, Dtos, Mapping, Features, Behaviors, Legacy.
- `samples/SampleShop/SampleShop.Tests/` — tests voor de nieuwe MediatR/AutoMapper-constructies (alleen eenvoudige FA, zodat `FixTwice` blijft werken).
- `samples/SampleShop/SampleShop.Advanced.Tests/` — complexe FA (blijft Manual, houdt FA in dat project).
- `src/Unshackle.Tests/Integration/SampleShopTests.cs` — aangepast aan het extra project.

### Hoe

Complexe FA in een apart project, omdat die Manual blijven en FluentAssertions dus in dat project blijft staan; zo houdt `FixTwice` voor `SampleShop.Tests` zijn betekenis.

### Waarom deze aanpak

Eén solution met alles, zoals een echte gebruiker; het aparte project scheidt "moet volledig migreren" van "moet netjes Manual rapporteren".

## Stappen

- [x] 1. Taakbestand
- [x] 2. MediatR-constructies + tests
- [x] 3. AutoMapper-constructies + tests
- [x] 4. Complexe FA in `SampleShop.Advanced.Tests`
- [x] 5. Roadmap-stap 6: `[Obsolete]` API
- [x] 6. Checks: `dotnet build`, `dotnet test`, sample-tests
- [x] 7. Manual-lijst hieronder invullen

## Risico's & randgevallen

- MediatR 12 registreert pre/post-processors en exception handlers automatisch via assembly-scan; generieke handlers vragen `RegisterGenericHandlers = true`.
- Na `fix` moet de sample nog bouwen: een Fixed-herschrijving in het advanced-project die niet compileert is een bug in een patroon (noteren, niet hier oplossen).

## Manual-gevallen (input voor nieuwe patronen)

Scan 2026-10-11: 59 Fixed, 15 Manual (was ≥20 Fixed, 1 Manual + project-file). Regels in `SampleShop.Advanced.Tests/AdvancedAssertionTests.cs`.

**Manual (patroon herkent, maar weigert):**

- r47 `.NotBeEmpty().And.HaveCount(2).And.NotContainNulls()`: keten met `.And` (be-empty, have-count).
- r65 `ContainKey(id).WhoseValue...`: contain-key met `.WhoseValue`.
- r73/r74 `Be(25m, "… {0} …", args)` / `HaveCount(2, because: …, 2)`: becauseArgs/named.
- r96/r99/r109 `BeEquivalentTo(..., options => ExcludingMissingMembers/Excluding/WithStrictOrdering)`.
- r123 `(await ThrowAsync<T>().WithMessage(..)).Which.ParamName...`; r126 `ThrowExactlyAsync<T>().Where(..)`.
- r135 `Invoking(..).Should().Throw<T>().WithMessage(..).And.Message...`.
- r148 custom `order.Should().BeCancelled(..).And.NotBeNull()`.

**Niet gezien (geen patroon, stil overgeslagen):** `StartWith`/`EndWith`/`HaveLength`, `NotContainNulls`, `ContainSingle(..).Which`, `NotBe` (met because), `BePositive`, `Equal`, `ContainEquivalentOf`, `BeInAscendingOrder`, `AllBe`, `Invoking`, `NotBeNullOrWhiteSpace`, `BeOfType<T>().Which`, `BeApproximately`, `BeCloseTo`, `Match`/`MatchRegex`, custom `ReferenceTypeAssertions`-extensie, `AssertionScope`.

**Mogelijke patroonbug:** r85-87 binnen `using (new AssertionScope())` worden Fixed (`ShouldBe`, `Count.ShouldBe`, `ShouldBePositive`), waardoor het verzamelgedrag van de scope stil verdwijnt. Waarschijnlijk hoort dit Manual te zijn (of `ShouldSatisfyAllConditions`).

**Integratietest:** `FixTwice` in `src/Unshackle.Tests/Integration/SampleShopTests.cs` controleerde dat FluentAssertions uit álle projectbestanden verdwijnt. Door het (bewust Manual-houdende) advanced-project kan dat niet meer; de check is nu: weg uit `SampleShop.Tests.csproj`, blijft in het advanced-project en `Directory.Packages.props`. Puur gevolg van de nieuwe sample-code.

MediatR- en AutoMapper-fixes hebben nog geen patronen: alles daar is doelcode voor later (streams, `RegisterGenericHandlers`, `TaskWhenAllPublisher`, pre/post-processors, `IRequestExceptionHandler`, `IRequest` void + `IRequest<Unit>`, `ISender`/`IMediator`-injectie; `IValueResolver`, `ITypeConverter`, `IValueConverter`, `AfterMap`, `Ignore`, nested + `ReverseMap`, collecties, assembly-scan).

## Gewijzigde bestanden

- `samples/SampleShop/SampleShop/`: `Domain/Customer.cs`, `Dtos/CustomerDto.cs`, `Mapping/CustomerProfile.cs`, `Data/CustomerRepository.cs`, `Features/Customers/*`, `Features/Common/CountQuery.cs`, `Features/Orders/CancelOrderCommand.cs`, `Features/Orders/OrderFacade.cs`, `Behaviors/{ValidationBehavior,StreamLoggingBehavior,Processors}.cs`, `Legacy/PriceCalculator.cs`, `ServiceCollectionExtensions.cs`
- `samples/SampleShop/SampleShop.Tests/{PipelineTests,CustomerMappingTests}.cs`
- `samples/SampleShop/SampleShop.Advanced.Tests/` (nieuw), `SampleShop.slnx`
- `src/Unshackle.Tests/Integration/SampleShopTests.cs`

## Zie ook

- `claude/tasks/patroon-queue.md`
- `docs/projectinfo/project.md` (roadmap)
- `src/Unshackle.Tests/Integration/SampleShopTests.cs`
