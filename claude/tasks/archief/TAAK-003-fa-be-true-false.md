# TAAK-003 — Patroon fa.be-true-false

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-10
**Doel**: `x.Should().BeTrue()` / `BeFalse()` op `bool` en `bool?` wordt herschreven naar Shouldly, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 2 in `claude/tasks/patroon-queue.md`. Wacht op rij 0 (TAAK-001, bij aanmaak nog `bezig`).
- Bij aanmaak bestaan al (half af): `IPattern`/`IFix`/`IFixer`, `FluentAssertionsFix`, `FluentAssertionsSyntax` (`FindInvocations`, `GetSubject`, `IsResultDiscarded`, `TryGetArguments`) en `ShouldBePattern`. Volg dat voorbeeld; controleer bij hervatten de echte vorm en wijzig de contracten niet.
- Aanname (van de opdrachtgever): het fundament voegt `using Shouldly;` toe en haalt `using FluentAssertions;` weg via `IFix.AddedNamespaces`/`RemovedNamespaces` en `PatternResult.Fixed(node, namespaces)`. Dit pattern hoeft zelf geen usings te beheren.
- Sample-voorkomen: `samples/SampleShop/SampleShop.Tests/OrderTests.cs:62` `(order.Status == OrderStatus.Cancelled).Should().BeTrue();` (`bool`). Geen `BeFalse` of `bool?` in de samples.

## Analyse

### Huidige situatie

FA 7.2.2: `BooleanAssertions<TAssertions>` heeft `BeTrue(string because = "", params object[] becauseArgs)` en `BeFalse(...)`. `Should()` op `bool` geeft `BooleanAssertions`, op `bool?` `NullableBooleanAssertions` (erft van `BooleanAssertions`, dus dezelfde `BeTrue`/`BeFalse`). Resultaat is `AndConstraint<..>`: `.And` ketens mogelijk. Op `bool?` faalt `BeTrue` ook bij `null`. `NotBeTrue`/`NotBeFalse` bestaan alleen op `NullableBooleanAssertions` (buiten scope).

### Shouldly 4.3.0 (via reflectie op `Shouldly.dll` gecontroleerd)

`ShouldBeBooleanExtensions.ShouldBeTrue(bool actual, string customMessage)` en `ShouldBeFalse(bool actual, string customMessage)`. Er is **geen** overload voor `bool?`: `nullable.ShouldBeTrue()` compileert niet (geen impliciete `bool?` → `bool`). Voor `bool?` is het equivalent `x.ShouldBe(true[, msg])` (`ShouldBe<T>(T actual, T expected, string? customMessage)` met `T = bool?`); faalt ook bij `null`, dus gelijk aan FA. `customMessage` is een kale string, zonder `{0}`-formatting.

### Probleem

- Naïef `BeTrue` → `ShouldBeTrue` breekt `bool?` (compileert niet).
- FA's `because` kent format-args, Shouldly niet.
- FA geeft een `AndConstraint` terug, Shouldly niets (void).

### Nieuwe situatie

`BeTrueFalsePattern` (ID `fa.be-true-false`) herschrijft de veilige gevallen en geeft anders `Manual(reden)`.

### Verbetering

Foutmeldingen noemen in Shouldly de expressie ("`order.IsPaid` should be `True` but was `False`"); FA schrijft "Expected ... to be true". Consistent, en alleen echt dubbelzinnige gevallen blijven voor handwerk.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Eén pattern `BeTrueFalsePattern`, ID `fa.be-true-false`; `Find` levert beide `BeTrue` en `BeFalse` (twee `FindInvocations`-aanroepen samengevoegd) | Queue-rij is één patroon; de logica is identiek op de naam na |
| 2 | Alleen herschrijven als de FA-methode `BeTrue`/`BeFalse` uit `FluentAssertions.Primitives` is (symbool-check via `FindInvocations`) | Voorkomt valse treffers op eigen `BeTrue` |
| 3 | Subject van type `bool` → `x.ShouldBeTrue([msg])` / `x.ShouldBeFalse([msg])` | Directe 1:1 |
| 4 | Subject van type `bool?` (`Nullable<bool>`) → `x.ShouldBe(true[, msg])` / `x.ShouldBe(false[, msg])` | Shouldly heeft geen `bool?`-overload (zie boven); `ShouldBe(true)` faalt ook op `null`, gelijk aan FA |
| 5 | Type van het subject onbekend/error type, of niet `bool`/`bool?` → `Manual("type onbekend")` | Voorkomt stille fouten bij ontbrekende referenties |
| 6 | `because` zonder `becauseArgs` → ongewijzigd als `customMessage` | Directe 1:1 via `TryGetArguments`; het "because"-voorvoegsel van FA vervalt, Shouldly zet "Additional Info:" ervoor: gedocumenteerd verschil |
| 7 | `because` met `becauseArgs`, of benoemde argumenten → `Manual("because met format-args")` | Shouldly formatteert niet; weglaten of `string.Format` bouwen is gokken |
| 8 | Alleen herschrijven als het resultaat wordt genegeerd (`IsResultDiscarded`); `.And`/`.Which`-vervolg, waarde gebruikt of niet in de vorm `x.Should().BeTrue()` (`GetSubject` null) → `Manual` | ShouldBeTrue geeft niets terug; ketens splitsen is rij 10. Zelfde regel als `ShouldBePattern` |
| 9 | Subject-expressie ongewijzigd overnemen (zoals `ShouldBePattern`): `(a == b).Should().BeTrue()` → `(a == b).ShouldBeTrue()` | De haakjes staan al in de bron omdat `.Should()` ze vereist; geen extra precedence-werk |
| 10 | `NotBeTrue`/`NotBeFalse` (alleen `bool?`) en `Be(true)` buiten scope | Andere semantiek (`null` slaagt) resp. ander patroon (`fa.should-be`) |
| 11 | Geen eigen `using`-beheer | Aanname fundament: `Shouldly` erbij, `FluentAssertions` eraf via de Fix |

## Aanpak

### Wat / Waar

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs` (`Id => "fa.be-true-false"`, `Description => "x.Should().BeTrue() → x.ShouldBeTrue()"`).
- Eén regel `new BeTrueFalsePattern(),` in `Patterns` van `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.
- Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/`.

### Hoe

1. `Find`: `FindInvocations(root, model, "BeTrue").Concat(FindInvocations(root, model, "BeFalse"))`.
2. `Rewrite`: Manual-checks (keuzes 8, 7, 5), dan type-switch: `bool` → `Should{True|False}`, `bool?` → `ShouldBe` met `true`/`false`-literal als eerste argument en eventuele `because` als tweede.
3. Bouw `InvocationExpression(MemberAccess(subject.WithoutTrailingTrivia(), name), args)` zoals `ShouldBePattern`.

## Proofs (te maken)

Map `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/`; `before` compileert tegen FA, `after` tegen Shouldly.

| Bestand | Inhoud |
|---|---|
| `be-true.before.cs` / `.after.cs` | `bool`: `flag.Should().BeTrue()` → `flag.ShouldBeTrue()` |
| `be-false.before.cs` / `.after.cs` | `bool`: `BeFalse()` → `ShouldBeFalse()`; ook `(a == b).Should().BeFalse()` |
| `because.before.cs` / `.after.cs` | `BeTrue("must be paid")` → `ShouldBeTrue("must be paid")` (en BeFalse) |
| `nullable.before.cs` / `.after.cs` | `bool?`: `BeTrue()` → `ShouldBe(true)`, `BeFalse("why")` → `ShouldBe(false, "why")` |
| `because-args.before.cs` / `.manual.txt` | `BeTrue("x {0}", y)` → Manual (keuze 7) |
| `chain.before.cs` / `.manual.txt` | `flag.Should().BeTrue().And.Be(true)` en `.BeTrue()` als waarde (`var c = ...`) → Manual (keuze 8) |
| `unknown-type.before.cs` / `.manual.txt` | indien een error type te maken is zonder dat `before` faalt in de compileercheck: anders overslaan en met unittest dekken |

## Stappen

- [x] 1. Claimen (rij 2) en taak aanmaken
- [x] 2. Onderzoek FA 7.2.2 / Shouldly 4.3.0 / sample-voorkomens, keuzes vastgelegd
- [x] 3. Rij 0 gecontroleerd: fundament klaar, contract (`Fixed`/`Manual`, usings via `IFix`) komt overeen met de aanname
- [x] 4. `BeTrueFalsePattern` + registratie in `FluentAssertionsFix`
- [x] 5. Proofs (zie tabel)
- [x] 6. `dotnet build` en `dotnet test` groen; taak archiveren, rij `klaar`

## Risico's & randgevallen

| Risico | Mitigatie |
|---|---|
| `bool?` met `ShouldBeTrue` compileert niet | Keuze 4; proof `nullable` met compileercheck |
| Contract voor usings anders dan aangenomen | Bij hervatten controleren; wijzig het contract niet |
| Foutmelding anders van vorm (Additional Info) | Gedocumenteerd verschil, geen Manual |

## Aanpassingen bij realisatie

- `unknown-type`-proof vervallen: `before` compileert dan niet; type-check alleen via code bewaakt.
- Extra proof `value-used` (`var c = flag.Should().BeFalse()`); `chain` gebruikt `.And.NotBe(false)` zodat alleen BeTrue een finding geeft (ProofTests eist de reden bij alle findings).
- Bij `bool?` krijgt `because` een leidende spatie (`ShouldBe(false, "why")`), anders plakt `SeparatedList` de argumenten aan elkaar.
- Contract klopte met de aanname; geen wijzigingen aan contracten.

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeTrueFalsePattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeTrueFalse/` (7 proofs)
- `docs/projectinfo/patterns/fa-be-true-false-{plan,verify}.md`, `docs/projectinfo/README.md` (rij)

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/docs/patroon-loop.md`
- `docs/projectinfo/architectuur/testen.md`
- `samples/SampleShop/SampleShop.Tests/OrderTests.cs`
