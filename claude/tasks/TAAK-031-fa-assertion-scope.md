# TAAK-031 — FA-assertions binnen `AssertionScope` niet stil herschrijven

**Status**: stap 0/4 — nog te starten
**Aangemaakt**: 2026-10-11
**Doel**: een assertion binnen `using (new AssertionScope())` wordt nooit meer stil `Fixed`; de scope-semantiek (alle fouten verzamelen) blijft behouden of wordt als `Manual` gemeld.

## Context voor de volgende agent

- Gevonden in TAAK-030: `samples/SampleShop/SampleShop.Advanced.Tests/AdvancedAssertionTests.cs` r83-88.
- Scan 2026-10-11: r85 (`fa.should-be`), r86 (`fa.have-count`), r87 (`fa.be-greater-less`, `BePositive`) worden `Fixed`; de `using (new AssertionScope())` zelf wordt niet gezien.
- Gedeelde helpers staan in `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` (`GetSubject`, `IsResultDiscarded`, `TryGetArguments`). Contracten (`Abstractions/`, `Models/`, `Fixer`) niet wijzigen.

## Analyse

### Huidige situatie

Elk patroon kijkt alleen naar de eigen invocation. Een `AssertionScope` eromheen (ook `using var scope = new AssertionScope();` of `new AssertionScope("context")`) wordt genegeerd.

### Probleem

- Na de fix stopt de test bij de eerste falende `ShouldBe`; FA meldde alle fouten tegelijk. Gedrag verandert stil, wat de belofte "Fixed = gelijkwaardig" breekt.
- De `using FluentAssertions.Execution;` en de scope blijven staan, dus FA blijft in het project hangen zonder dat de gebruiker weet waarom.

### Nieuwe situatie

Elke FA-assertion waarvan een voorouder (statement-block) onder een `AssertionScope`-using valt, krijgt `Manual("Inside an AssertionScope: ...")`. Een apart patroon `fa.assertion-scope` meldt de scope zelf als `Manual` met advies `ShouldSatisfyAllConditions`.

### Verbetering

Geen stille gedragswijziging meer; de gebruiker ziet in het rapport precies welke scopes handwerk vragen.

## Aanpak

### Wat

1. Helper `FluentAssertionsSyntax.IsInsideAssertionScope(SyntaxNode, SemanticModel)`: loopt de voorouders af naar `UsingStatementSyntax` met `new AssertionScope(...)` en naar `LocalDeclarationStatementSyntax` met `using` (geldig tot einde blok, dus alleen statements ná de declaratie). Type via het semantisch model (`FluentAssertions.Execution.AssertionScope`), niet op naam.
2. Alle patronen in `Patterns/` roepen de helper als eerste check aan → `Manual`.
3. Nieuw patroon `AssertionScopePattern` (`fa.assertion-scope`): vindt `new AssertionScope(...)` en geeft altijd `Manual` met de suggestie.

### Waar

- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs`
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/*.cs` (alle 14 patronen + `ThrowMessage.cs` indien nodig)
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AssertionScopePattern.cs` (nieuw) + regel in `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AssertionScope/` + KingmaDoc `docs/projectinfo/patterns/fa-assertion-scope-{plan,verify,modellen}.md` + README-rij

### Hoe

Eén gedeelde check in plaats van per patroon eigen logica (DRY, één plek voor `using var`-randgevallen). Proofs: `using (...)`-blok, `using var`, scope met context-string, assertion vóór `using var` (moet wél Fixed blijven).

### Waarom deze aanpak

Optie B, automatisch herschrijven naar `x.ShouldSatisfyAllConditions(() => ..., () => ...)`, is afgewezen voor nu: Shouldly heeft die API (`ShouldSatisfyAllConditions` in 4.3.0, bevestigd in `Shouldly.dll`), maar die verwacht één subject of losse lambdas, scopes bevatten vaak ook niet-assertion-statements, geneste scopes en `scope.Discard()`/`AddReportable`. Manual is veilig; een herschrijving kan later als eigen patroon voor het simpele geval (blok met alleen assertion-statements).

## Stappen

- [ ] 1. Helper + unittests (`using`-blok, `using var`, geneste scope, assertion buiten scope)
- [ ] 2. Alle patronen laten afvragen; bestaande proofs blijven groen
- [ ] 3. `AssertionScopePattern` + proofs + KingmaDoc
- [ ] 4. Scan op SampleShop: r85-87 `Manual`; `dotnet build`, `dotnet test`, sample-tests groen

## Risico's & randgevallen

- `using var scope` geldt tot einde blok: alleen statements na de declaratie in hetzelfde blok en dieper.
- Lambdas binnen de scope (bijv. `act` die later wordt uitgevoerd) vallen syntactisch binnen de scope; Manual is dan te voorzichtig, maar veilig.
- Custom scope-subklassen: check met `InheritsFrom`/`SymbolEqualityComparer` op `AssertionScope`.

## Zie ook

- `claude/tasks/archief/TAAK-030-sampleshop-uitbreiden.md` (vondst)
- `claude/tasks/patroon-queue.md`
- `docs/projectinfo/architectuur/core.md`
