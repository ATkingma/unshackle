# TAAK-002 — Patroon fa.have-count

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-10
**Doel**: `x.Should().HaveCount(n)` wordt type-afhankelijk herschreven naar Shouldly, met proofs, groen in `dotnet test`.

## Context voor de volgende agent

- Rij 1 in `claude/tasks/patroon-queue.md`. Wacht op rij 0 (TAAK-001): `IPattern`, `Fixer`, `FluentAssertionsFix`, `ProofTests` bestonden bij aanmaak nog niet (alleen lege mappen in `src/Unshackle.Core/`).
- Bij hervatten: controleer eerst de echte vorm van `IPattern` (Find/Rewrite, hoe `using`-toevoegingen kunnen) en pas **Aanpak** daarop aan; wijzig de contracten niet.
- Sample-voorkomens: `samples/SampleShop/SampleShop.Tests/OrderTests.cs:33` (List), `:42` (IEnumerable via `Where`), `OrderHandlerTests.cs:68` (`List<OrderDto>`), `RepositoryTests.cs:16` (dictionary → Manual).

## Analyse

### Huidige situatie

FA 7.2.2: `GenericCollectionAssertions<TCollection,T,TAssertions>` heeft
`HaveCount(int expected, string because = "", params object[] becauseArgs)` en
`HaveCount(Expression<Func<int,bool>> countPredicate, string because = "", params object[] becauseArgs)`.
Verwante methodes (andere patronen, niet hier): `NotHaveCount`, `HaveCountGreaterThan(OrEqualTo)`, `HaveCountLessThan(OrEqualTo)`.
`Should()` bestaat voor `IEnumerable<T>` (ook dictionaries, arrays, lists), non-generic `IEnumerable`, en `string` (geen HaveCount).
Resultaat is `AndConstraint<..>`: ketens met `.And`/`.Which` mogelijk.

### Shouldly 4.3.0

Er is **geen** `ShouldHaveCount`. Equivalent is de teller zelf asserten: `x.Count.ShouldBe(n)` met
`ShouldBe<T>(this T actual, T expected, string? customMessage = null)` (ook `Func<string>`-variant).
`customMessage` is een kale string: geen `{0}`-formatting met args zoals FA's `becauseArgs`.
Gedrag: Shouldly meldt bij falen "x.Count should be 2 but was 3" (de teller, niet de inhoud); FA toont de hele collectie. Acceptabel verlies.

### Probleem

De juiste teller hangt van het type af (`Count` property, `Length`, LINQ `Count()`), en niet elk geval is veilig te vertalen.

### Nieuwe situatie

Pattern `fa.have-count` (`HaveCountPattern`) herschrijft de veilige gevallen en geeft voor de rest `Manual(reden)`.

### Verbetering

Consistent, en gebruiker hoeft alleen echt dubbelzinnige gevallen (dictionary, predicate) zelf te doen.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | Eén pattern `HaveCountPattern` (ID `fa.have-count`) met type-switch intern | Queue-rij is één patroon; 3 uitkomsten + Manual blijven klein. Splitsen (zoals ARCHITECTURE.md toestaat) alleen als het te groot wordt |
| 2 | Typebepaling via `SemanticModel` op de ontvanger van `.Should()`; geen error type → `Manual("type onbekend")` | Voorkomt stille fouten bij ontbrekende referenties |
| 3 | Dictionary (`IDictionary<,>`, `IReadOnlyDictionary<,>`, of type implementeert ze, incl. `Dictionary<,>`) → `Manual("Dictionary: controleer of Count of Keys bedoeld is")` | Vastgelegd in queue/ARCHITECTURE.md (rapportvoorbeeld). `Count` zou technisch kloppen, maar de intentie is vaak "aantal keys" |
| 4 | Array (1 dimensie) → `x.Length.ShouldBe(n)` | Array heeft publiek `Length`; `Count` alleen via expliciete `ICollection` (compileert niet) |
| 5 | Type met publieke `int Count` property (`List<T>`, `HashSet<T>`, `ICollection<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>` via interface, `Collection<T>`) → `x.Count.ShouldBe(n)` | Geen enumeratie, O(1), side-effect-vrij. `IList<T>` erft `Count` van `ICollection<T>`: meenemen in de zoektocht naar het property (ook via `AllInterfaces`) |
| 6 | `ImmutableArray<T>` (heeft `Length`, geen `Count`) → `.Length` als het type een publieke int `Length` heeft en geen `Count` | Zelfde regel als array, generiek geformuleerd: eerst `Count`, dan `Length` |
| 7 | Overig `IEnumerable<T>` (LINQ-resultaten, `IQueryable`, iterators) → `x.Count().ShouldBe(n)` en `using System.Linq;` toevoegen als die ontbreekt (ook als global/implicit using ze al levert: niets toevoegen) | Spec uit queue. `Count()` enumereert, FA ook; gedrag gelijk |
| 8 | Non-generic `IEnumerable` → `Manual("Non-generic IEnumerable: gebruik Cast<T>().Count()")` | Geen `Count()` zonder `Cast`; elementtype onbekend |
| 9 | `HaveCount(predicate)` (Expression-lambda) → `Manual("Predicate: herschrijf naar Count.ShouldBeGreaterThan/LessThan/...")` | Shouldly kent geen count-predicate; lambda omzetten vergt semantiek per operator. Eventueel later aparte patronen voor `HaveCountGreaterThan` e.d. |
| 10 | `because` zonder `becauseArgs` (string-literal of string-expressie) → tweede argument van `ShouldBe` (`customMessage`) | Directe 1:1 |
| 11 | `because` met `becauseArgs` (`{0}`-placeholders) → `Manual("because met format-args")` | Shouldly formatteert niet; stil weglaten of `string.Format` bouwen is gokken |
| 12 | Alleen herschrijven als de `HaveCount`-aanroep een losse expressie-statement is (`x.Should().HaveCount(2);`, ook met `await`-vrije context). Staat er `.And`/`.Which` achter, of wordt het resultaat gebruikt (lambda-body, return, toewijzing) → `Manual("keten/waarde gebruikt")` | Shouldly-asserts geven (bijna) niets bruikbaars terug; keten splitsen is werk van een ander patroon (rij 10) |
| 13 | Ontvanger met lagere precedence (`a ?? b`, `c ? a : b`, cast, await, binaire operator) krijgt haakjes; conditional access (`a?.b.Should()`) → Manual | Behoud van semantiek bij `.Count`-toegang |
| 14 | `Should()` moet de FA-extensie zijn (symbol in namespace `FluentAssertions`); anders overslaan (geen Finding) | Voorkomt valse treffers op eigen `Should()` |
| 15 | Argument `n`: ongewijzigd overgenomen (expressie met `int`-type) | Geen herberekening nodig |
| 16 | `NotHaveCount`, `HaveCountGreaterThan` e.d. buiten scope | Andere patronen; hier alleen exacte `HaveCount` |

## Aanpak

### Wat / Waar

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` (Id `fa.have-count`, Description `x.Should().HaveCount(n) → x.Count.ShouldBe(n)`).
- Eén regel in de `Patterns`-lijst van `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.
- Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/`.

### Hoe

1. `Find`: invocaties met naam `HaveCount` waarvan het symbool in `FluentAssertions.Collections.*` zit.
2. `Rewrite`: Manual-checks (keuzes 2, 3, 8, 9, 11, 12), daarna type-switch (4–7), bouw `Receiver.Count.ShouldBe(n[, because])`.
3. `using System.Linq` alleen voor keuze 7; mechanisme hangt af van de foundation-contracten (open punt hieronder).

### Open punt

Kan een pattern via de contracten een `using` toevoegen? Zo niet: terugvallen op `System.Linq.Enumerable.Count(x)` is lelijk; dan liever stap `[!]` en gebruiker/foundation vragen. Beslissen bij hervatten.

## Proofs (te maken)

Map `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/`; `before` compileert tegen FA, `after` tegen Shouldly.

| Bestand | Inhoud |
|---|---|
| `list.before.cs` / `list.after.cs` | `List<int>`: `x.Should().HaveCount(2)` → `x.Count.ShouldBe(2)` |
| `list-because.before.cs` / `.after.cs` | `HaveCount(2, "two items expected")` → `x.Count.ShouldBe(2, "two items expected")` |
| `collection-interfaces.before.cs` / `.after.cs` | `ICollection<T>`, `IReadOnlyCollection<T>`, `IReadOnlyList<T>`, `HashSet<T>` → `.Count` |
| `array.before.cs` / `array.after.cs` | `int[]` → `.Length.ShouldBe(n)` |
| `enumerable.before.cs` / `enumerable.after.cs` | `IEnumerable<T>` / `list.Where(...)` → `.Count().ShouldBe(n)` + `using System.Linq;` (variant mét en zonder bestaande using) |
| `enumerable-because.before.cs` / `.after.cs` | idem met `because` |
| `receiver-parens.before.cs` / `.after.cs` | `(a ?? b).Should().HaveCount(1)` → `(a ?? b).Count.ShouldBe(1)` |
| `dictionary.before.cs` / `dictionary.manual.txt` | `Dictionary<,>` → Manual (reden keuze 3) |
| `predicate.before.cs` / `predicate.manual.txt` | `HaveCount(c => c > 1)` → Manual |
| `because-args.before.cs` / `because-args.manual.txt` | `HaveCount(2, "x {0}", y)` → Manual |
| `chain.before.cs` / `chain.manual.txt` | `x.Should().HaveCount(2).And.Contain(1)` → Manual |
| `non-generic.before.cs` / `non-generic.manual.txt` | non-generic `IEnumerable` → Manual |

## Stappen

- [x] 1. Claimen (rij 1) en taak aanmaken
- [x] 2. Onderzoek FA 7.2.2 / Shouldly 4.3.0 / sample-voorkomens, keuzes vastgelegd
- [x] 3. Rij 0 klaar; contract gecontroleerd (`PatternResult.Fixed(node, namespaces)` voegt usings toe)
- [x] 4. `HaveCountPattern` + registratie in `FluentAssertionsFix`
- [x] 5. Proofs (zie tabel)
- [x] 6. `dotnet build` en `dotnet test` groen; taak archiveren, rij `klaar`

## Risico's & randgevallen

| Risico | Mitigatie |
|---|---|
| Foundation-contract kan geen `using` toevoegen | Open punt; bij hervatten beslissen |
| `Count` op custom type met `Count` die geen `int` is | Alleen `int`-property accepteren, anders Manual |
| Lazy `IEnumerable` met side-effects | FA en `Count()` enumereren beide één keer: gelijk gedrag |
| Foutmelding wordt korter (alleen teller) | Gedocumenteerd verschil, geen Manual nodig |

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/HaveCountPattern.cs` (nieuw)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (+1 regel)
- `src/Unshackle.Tests/Proofs/FluentAssertions/HaveCount/` (12 proofs)

## Aanpassingen bij realisatie (t.o.v. onderzoek)

- Open punt `using System.Linq` opgelost: `PatternResult.Fixed(node, "System.Linq")`; het Fixer voegt het alleen toe als het ontbreekt (proofs `enumerable` en `enumerable-existing-using`).
- Keuze 8 (non-generic `IEnumerable`) is wel geïmplementeerd (Manual) maar **geen proof**: FA 7.2.2 heeft geen `HaveCount` op de `Should()` van non-generic `IEnumerable` (resolvet naar `ObjectAssertions`, compileert niet).
- Predicate herkend via de eerste parameter van de resolved methode (`int` of niet), niet via de argumentvorm. Conditional access (`a?.b`) is Manual (keuze 13).
- `enumerable`-proof gebruikt een eigen iterator-methode i.p.v. `Where`, omdat `before` zonder `using System.Linq` moet compileren.
- Proofs: 12 (zie lijst in de tabel hierboven; `enumerable-existing-using` erbij, `non-generic` vervallen).

## Zie ook

- `claude/tasks/patroon-queue.md`
- `claude/docs/patroon-loop.md`
- `ARCHITECTURE.md` sectie "Testen"
- `samples/SampleShop/SampleShop.Tests/OrderTests.cs`
