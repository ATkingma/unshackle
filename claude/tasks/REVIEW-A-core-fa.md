# REVIEW-A: Core + FluentAssertions-fix

Read-only review op `main` 3bd36cf. Scope: `src/Unshackle.Core/` (Fixer, UsingDirectives, ProjectPackages, Abstractions, Models) en `src/Unshackle.Core/Fixes/FluentAssertions/`.
Niet gedaan: build/tests draaien, proofs nalopen per patroon, Shouldly-overloads per patroon verifiëren tegen de echte assembly.

## Hoog

### H1. BeGreaterLess levert niet-compilerende code op voor byte/short/sbyte/ushort

`Fixes/FluentAssertions/Patterns/BeGreaterLessPattern.cs:135-141`

- **Probleem:** `byte b; b.Should().BeGreaterThan(1)` wordt `b.ShouldBeGreaterThan(1)`. Shouldly's `ShouldBeGreaterThan<T>(this T, T)` leidt `T = int` af, en de receiver mag geen numerieke conversie hebben, dus dat geeft CS1929. Hetzelfde geldt voor `BeInRange`/`NotBeInRange`. FluentAssertions zette de literal om naar het type van het subject. Het resultaat is Fixed, terwijl de build stukgaat.
- **Waarom:** `ShouldBePattern.ConvertExpected` lost precies dit op (een cast naar het subjecttype) en `NotBePattern` gebruikt het al. Dit patroon is daarbij vergeten.
- **Fix:** haal elke waarde in `values` door `ConvertExpected(value.Expression, type, model, link.SpanStart)`. Bij `null` geef je Manual. Voeg een proof toe met `byte` en `short`.

### H2. AndChain verliest `FixedResult.Warning` van een link

`Fixes/FluentAssertions/Patterns/AndChainPattern.cs:108-119`, `140`

- **Probleem:** `BeEquivalentToObjectPattern:101` zet een `Warning` (gedragsverschil bij afgeleide types). Komt die link via een `.And`-keten binnen, dan behoudt `RewriteLink` hem nog (`fixedResult with {...}`), maar `Rewrite` bouwt een nieuwe `PatternResult.Fixed(statements, ...)` en gooit de waarschuwing weg. Het rapport toont dan een stille Fixed terwijl er een semantisch verschil is.
- **Fix:** verzamel de warnings van de links en zet ze op het resultaat: `(FixedResult)PatternResult.Fixed(...) with { Warning = string.Join(" ", warnings) }` (of `null` als de lijst leeg is).

### H3. Een Fixer-crash buiten de try/catch breekt de hele run af

`Fixer.cs:107-108`

- **Probleem:** `DescendantTrivia().First(t => DisabledTextTrivia)` loopt op de root _vóór_ de usings-update, terwijl `KeepsRemovedUsing` op de root _ná_ de update wordt geëvalueerd. Patronen die tekst opnieuw parsen (`AllSatisfyPattern` doet `ParseStatement` met lambdabody-tekst) kunnen disabled trivia verplaatsen of laten ontstaan. Dan gooit `First` een `InvalidOperationException` die niemand vangt, en is de hele `FixAsync` weg in plaats van één Manual-regel.
- **Fix:** gebruik `updatedRoot.DescendantTrivia().FirstOrDefault(...)` met fallback naar regel 0. Gebruik dezelfde root als waar de conditie op draait.

## Midden

### M1. Dezelfde preamble in ~20 patronen (Template Method ontbreekt)

Alle `Patterns/*.cs` (bijv. `ShouldBePattern.cs:20-47`, `HaveCountPattern.cs:20-50`, `BeNullPattern.cs:20-40`, `BeEmptyPattern.cs:20-40`, `BeTrueFalsePattern.cs:20-40`, ...)

- **Probleem:** steeds dezelfde reeks: `IsInsideAssertionScope` → Manual, cast naar `InvocationExpressionSyntax`, `GetSubject` → Manual "not in the form x.Should()…", `IsResultDiscarded` → Manual, `TryGetArguments` → Manual "becauseArgs or named arguments". Alleen de teksten wijken af, en net niet consistent (zie M5).
- **Fix:** een abstracte basis `FluentAssertionPattern : IPattern` (Template Method, GoF) met een sealed-vrije `Rewrite` die de checks doet en `RewriteAssertion(AssertionContext ctx)` aanroept. De context bevat `Assertion`, `Subject`, `Values`, `Because`, `Model` en `Name`. Patronen met afwijkende nodes (statement in plaats van invocation: AllSatisfy, OnlyContain, BeGreaterLess, ContainSingle/BeOfType met `.Which`) overriden een hook `GetAssertion(node)`. Zo verdwijnen zo'n 300 regels.

### M2. Vier versies van "veilig om twee keer te evalueren"

`AndChainPattern.cs:156-167` (`IsFreeOfSideEffects`, symboolgebaseerd), `AllSatisfyPattern.cs:105-111`, `BeGreaterLessPattern.cs:150-156`, `OnlyContainPattern.cs:78-84` (3× identieke `IsSimple`, alleen syntax)

- **Probleem:** gekopieerde code, en ook net een andere semantiek: AndChain weigert bijvoorbeeld indexers of methodegroepen via symbolen, `IsSimple` kijkt niet naar symbolen. Hetzelfde subject kan in het ene patroon Fixed worden en in het andere Manual.
- **Fix:** één `FluentAssertionsSyntax.IsSafeToEvaluateTwice(ExpressionSyntax, SemanticModel)` op basis van AndChains symboolversie. Zet de `shortcut: property getters are assumed pure`-opmerking daar één keer neer.

### M3. Cross-pattern helpers ad hoc `internal` op concrete patronen

`HaveCountPattern.cs:91` (`IsSafeReceiver`, gebruikt door BeApproximately, HaveLength), `ShouldBePattern.cs:82` (`ConvertExpected`, door NotBe), `ThrowPattern.cs:20`, `NotThrowPattern.cs:19`, `ThrowAsyncPattern.cs:21` (`internal static Find(..., owns)`), `InvokingPattern.cs:25/41/51`, `ThrowExactlyPattern.cs:21`

- **Probleem:** patronen hangen van elkaar af via statics op andere patronen. `HaveCountPattern.IsSafeReceiver` heeft niets met count te maken. Throw, ThrowExactly, Invoking en NotThrow vormen samen een web van wederzijdse `owns`-filters en `IsInvoking`-checks.
- **Fix:** verplaats `IsSafeReceiver` en `ConvertExpected` naar `FluentAssertionsSyntax`. Bundel `IsExactly`, `IsInvoking` en `TryGetAction` in een `ThrowSyntax`-helper (naast `ThrowMessage`, of voeg ze daarin samen), zodat de patronen geen andere patronen meer aanroepen.

### M4. Ketensplitsing bestaat twee keer; BeGreaterLess-ketenpad is in de praktijk dood en geeft dubbele findings

`BeGreaterLessPattern.cs:35-48, 57-104` versus `AndChainPattern.cs`

- **Probleem:** `AndChainPattern` draait eerst en splitst ook `BeGreaterThan(..).And.BeLessThan(..)`. Het eigen `.And`-pad in BeGreaterLess wordt alleen nog bereikt als AndChain Manual gaf, en dan meldt BeGreaterLess op dezelfde statement nóg een Manual. Algemener geldt: bij een Manual-keten rapporteert elk linkpatroon zijn eigen link ook als Manual ("not in the form … after .And" of "result is used"). Eén keten levert zo 2-4 rapportregels op.
- **Fix:** haal de ketenlogica uit BeGreaterLess (Find wordt dan `FindInvocations(...)`, `links` is altijd 1). Laat linkpatronen in `Find` invocations overslaan waarvan `GetSubject` null is en die in een `.And`-keten zitten. AndChain is daar de eigenaar van, zoals Throw al `owns` gebruikt.

### M5. Null-beleid van het subject verschilt per patroon

`ContainSinglePattern.cs:58` (altijd `NotNull`), `HaveCountPattern.cs:58` / `HaveLengthPattern.cs:53` (alleen bij een veilige receiver, anders haakjes zonder `NotNull`), `ShouldBePattern.cs:74`, `OnlyContainPattern.cs:65`, `EqualSequencePattern` (nooit), `BeApproximatelyPattern.cs:67` (Manual)

- **Probleem:** dezelfde situatie (een nullable subject in een nullable-enabled context) wordt Fixed met `ShouldNotBeNull()`, Fixed zonder (met kans op CS8602/CS8604, wat bij `TreatWarningsAsErrors` in gebruikerscode de build breekt) of Manual. Bij HaveCount levert een complex subject de vorm `(expr).Count.ShouldBe(n)` op zonder null-check, terwijl een simpel subject er wel een krijgt.
- **Fix:** kies één regel in `FluentAssertionsSyntax.NotNull` en pas die toe op elke Shouldly-receiver die niet nullable is (`NotNull` kan zelf haakjes zetten voor onveilige receivers). Leg per patroon in een opmerking vast waar het bewust afwijkt, zoals NotBe al doet.

### M6. AndChain: elke link draait alle 23 `Find`s over het hele bestand op een nieuwe compilation

`AndChainPattern.cs:96, 123-128`

- **Probleem:** per keten volgt `ReplaceSyntaxTree` (een compilation-fork) en een nieuw semantic model. Daarna doet `RewriteLink` per link `pattern.Find(root, model)` over de **hele** split-root voor elk linkpatroon, en elke `FindInvocations` bindt symbolen op het verse model. Dat kost O(ketens × links × patronen × invocaties in bestand), en bij testbestanden met veel ketens loopt dat snel op.
- **Fix:** geef `linkStatement` als root mee aan `pattern.Find(linkStatement, model)`. De patronen gebruiken alleen `DescendantNodes` op root en het model, dus dat werkt. Maak de check `found == statement` daarop passend (de statement zelf zit niet in `DescendantNodes`, dus `DescendantNodesAndSelf` is nodig voor AllSatisfy en OnlyContain). Optioneel: splits alle ketens van een bestand in één boom en één fork.

### M7. Het hele solution wordt drie keer volledig gebonden voor usingsanalyse

`UsingDirectives.cs:156-162` (`UsedNamespaces`), aangeroepen uit `Fixer.cs:87` (elk bestand), `Fixer.cs:179` (elk document van een global-using-project) en `ProjectPackages.cs:62-66` (elk document opnieuw)

- **Probleem:** `GetSymbolInfo` op élke `SimpleNameSyntax` van elk document, drie keer, plus `GetSemanticModelAsync` voor documenten die geen enkel patroon raakten. Dit is de dominante kost op grote solutions.
- **Fix:** één pass per document die `(namespaces, assemblies)` van gebonden symbolen verzamelt (in een `Dictionary<DocumentId, ...>` binnen `FixAsync`) en die hergebruikt. Sla bestanden zonder enige `FluentAssertions`-token over met een snelle tekst- of syntaxcheck vóór het binden.

### M8. Drie verschillende manieren om een statement te splitsen

`OnlyContainPattern.cs:16-19` (blokcheck in `Find`), `AllSatisfyPattern.cs:19, 74` (statement in `Find`, blokcheck in `Rewrite`), `AndChainPattern.cs:32, 46` en `BeGreaterLessPattern.cs:96` (blokcheck in `Rewrite`)

- **Probleem:** inconsistent. OnlyContain rapporteert bij `if (c) x.Should().OnlyContain(..);` een andere node (de invocation) en een andere reden dan AllSatisfy in dezelfde situatie.
- **Fix:** een gedeelde helper `FluentAssertionsSyntax.TryGetSplittableStatement(InvocationExpressionSyntax, out ExpressionStatementSyntax)` (Parent is `BlockSyntax` of `SwitchSectionSyntax`), aangeroepen vanuit `Rewrite`, met één gedeelde reden-tekst.

## Laag

### L1. Het because-argument wordt op 5 verschillende manieren toegevoegd

`BeEmptyPattern.cs:49`, `BeNullPattern.cs:49`, `BeOfTypePattern.cs:76`, `BeGreaterLessPattern.cs:135`, `ContainSinglePattern.cs:59`, `NotThrowPattern.cs:80`, `EqualSequencePattern.cs:72`, `OnlyContainPattern.cs:63`, met `ThrowMessage.Arguments` als de nette variant

- **Fix:** verplaats `ThrowMessage.Arguments(first, because)` naar `FluentAssertionsSyntax.Arguments` en gebruik die overal. Dat lost meteen de wisselende spatie-trivia op (soms `WithLeadingTrivia(Space)`, soms niet).

### L2. De "becauseArgs"-reden klopt niet als het aantal waarden afwijkt

`BeGreaterLessPattern.cs:124-127`, `ThrowPattern.cs:88-91, 110-113`, `NotThrowPattern.cs:75-78`

- **Probleem:** `!TryGetArguments(...) || values.Count != n` geeft in beide gevallen "becauseArgs or named arguments", ook bij een onverwacht aantal argumenten. Dat misleidt in het rapport.
- **Fix:** splits de check in twee aparte checks met een eigen reden (BeOfType doet dat al goed, `:63-70`).

### L3. AndChain geeft een misleidende reden bij een link met meerdere statements

`AndChainPattern.cs:108-116`

- **Probleem:** een link die Fixed teruggeeft met meer dan één node (OnlyContain, AllSatisfy) valt in `default` en wordt dan "which no pattern rewrites". Dat klopt niet: er is een patroon, het geeft alleen twee statements terug.
- **Fix:** accepteer `FixedResult { NewNodes: var nodes } when nodes.All(n => n is StatementSyntax)` en voeg ze allemaal toe. Dat lost het meteen op in plaats van alleen de tekst aan te passen.

### L4. `IComparable<T>`-check met overbodige conditie

`BeGreaterLessPattern.cs:113`

- `i.OriginalDefinition.SpecialType == SpecialType.None` voegt niets toe (`IComparable<T>` heeft geen SpecialType). Vergelijk liever met `model.Compilation.GetTypeByMetadataName("System.IComparable`1")`via`SymbolEqualityComparer`, zoals `ThrowMessage.MayThrowAggregate` dat met AggregateException doet.

### L5. Namespacecheck op string in plaats van symbool

`FluentAssertionsSyntax.cs:22-24, 134-135`, `ShouldBePattern.cs:38`, `NotBePattern.cs:37`, `BeOfTypePattern.cs:58`

- `ContainingNamespace.ToDisplayString()` maakt een string per aanroep, en `FindInvocations` roept dit aan voor elke invocation met een passende naam in elk patroon. Dat is goedkoop maar het telt op: 25 patronen × alle invocations. Optioneel: cache de `INamespaceSymbol` van FluentAssertions per compilation. Dit is laag, alleen relevant samen met M7.

### L6. `HashSet` wordt per project opnieuw gebouwd

`ProjectPackages.cs:23`. Zet `removed` vóór de `foreach`. Dit is een nit.

### L7. ShouldBe op een `object`-subject met een collectie verandert de semantiek (onzeker)

`ShouldBePattern.cs:71-76`

- FluentAssertions' `ObjectAssertions.Be` vergelijkt met `Equals` (referentie bij `List<T>`). Shouldly's `ShouldBe<T>` vergelijkt, voor zover ik weet, enumerables element voor element. Een test die faalde kan na de rewrite slagen. Dit is niet geverifieerd: controleer het met een proof (`object o = new List<int>{1}; o.Should().Be(new List<int>{1});`). Klopt het, geef dan Manual als het runtime- of statische type `IEnumerable` implementeert en geen string is.

### L8. `shortcut:`-opmerkingen: status

- `UsingDirectives.cs:7` (usings in namespaceblokken worden niet aangeraakt): **nu oplossen.** File-scoped namespaces met usings erin zijn gangbaar in testcode. Dan blijft `using FluentAssertions;` staan en wordt `using Shouldly;` bovenaan toegevoegd, wat compileert, maar `HasUnusedRemovedUsingAsync` ziet de oude using niet en het pakket blijft mogelijk referenced via `ProjectPackages`. Verifieer met een integratietest op een bestand met `namespace X; using FluentAssertions;`-stijl (usings binnen een `namespace {}`-blok).
- `UsingDirectives.cs:53`, `:121`, `ProjectPackages.cs:22`: acceptabel, laten staan.
- `OnlyContain/AllSatisfy/BeGreaterLess` "property getters assumed pure": samenvoegen via M2.
- `InvokingPattern.cs:92`: laten staan.

### L9. Code-standaarden

Geen `sealed`, geen primary constructors, modellen zijn classes met `required init`, de `PatternResult`-records zijn de toegestane uitzondering, `CancellationToken` zit overal in de async API's en Core verwijst naar geen ander project. Er zijn **geen schendingen gevonden.** Eén randgeval: `IPattern.Find`/`Rewrite` hebben geen `CancellationToken`. Ze zijn synchroon, dus de regel geldt formeel niet, maar `AndChainPattern` doet dure werk (M6) zonder te kunnen annuleren. Overweeg een token in `IPattern` als M6 niet volstaat.

## Top-5 eerst fixen

1. **H1**: BeGreaterLess byte/short naar `ConvertExpected`. Nu wordt het stil Fixed terwijl de build breekt.
2. **H3**: `First` naar `FirstOrDefault` in `Fixer.cs:107`. Eén regel, voorkomt dat de hele run crasht.
3. **H2**: Warning doorgeven in AndChain, zodat het rapport niet liegt.
4. **M4 + M8**: ketens hebben één eigenaar (AndChain), de ketenlogica gaat uit BeGreaterLess en linkpatronen slaan `.And`-links over. Dat ruimt dode code en dubbele rapportregels op.
5. **M1 + M2 + M3**: basisklasse `FluentAssertionPattern` met gedeelde helpers in `FluentAssertionsSyntax`. Hierdoor worden M5 en L1 en L2 bijna gratis consistent. Doe dit vóór er nog meer patronen bijkomen.

## Status (uitgevoerd op 3e7f807, ongecommit)

| Bevinding | Status | Toelichting |
| --------- | ------ | ----------- |
| H1 | Opgelost | `BeGreaterLessPattern` haalt elke waarde door `FluentAssertionsSyntax.ConvertExpected`; null → Manual. Proof `BeGreaterLess/byte-short`. |
| H2 | Opgelost | `AndChainPattern` verzamelt `Warning` van de schakels. Unittest `AndChainPatternTests.Rewrite_LinkWithWarning_KeepsWarning`. |
| H3 | Opgelost | `Fixer.cs` (enige wijziging, regels ~106-109): `First` → `FirstOrDefault`, regel 0 als de disabled trivia alleen in de geüpdatete root zit. |
| M1 | Opgelost | `FluentAssertionPattern` (Template Method) + `AssertionContext`; 19 patronen erven ervan. Bewust niet: Throw, ThrowAsync, ThrowExactly, NotThrow, Invoking (andere node-vorm: `WithMessage`/`await` als node, eigen volgorde van checks); die delen `ThrowSyntax`. |
| M2 | Opgelost | Eén `FluentAssertionsSyntax.IsSafeToEvaluateTwice` (symboolgebaseerd, uit AndChain), gebruikt door AndChain, AllSatisfy, OnlyContain. BeGreaterLess heeft hem niet meer nodig (M4). |
| M3 | Opgelost | `IsSafeReceiver`, `ConvertExpected` → `FluentAssertionsSyntax`; `IsExactly`, `IsInvoking`, `TryGetAction`, de `Find(owns)`-varianten en de dispatch → `ThrowSyntax`. NotBe is een subklasse van ShouldBe (alleen de Shouldly-naam verschilt). |
| M4 | Deels | Ketenlogica uit BeGreaterLess weg (proofs `and-chain`, `chain-lambda` → `AndChain/greater-less`, `AndChain/lambda`; `chain-other`, `chain-no-block`, `chain-subject-call`, `chain-used` vervallen, gedekt door `AndChain`-proofs en `BeGreaterLess/value-used`). Bewust niet: linkpatronen `.And`-schakels laten overslaan. ~25 proofs per patroon leggen die Manual vast, en een patroon zonder AndChain (proofrunner, of een fix zonder AndChain) zou de keten dan stil negeren. Dubbele regels ontstaan alleen als AndChain Manual geeft. |
| M5 | Deels | `FluentAssertionsSyntax.NotNullReceiver`: HaveCount en HaveLength krijgen nu ook bij een complex subject `ShouldNotBeNull()` (zelfde regel). Bewust niet voor ShouldBe/NotBe (generieke `T` accepteert nullable; NotBe zou falen op null), EqualSequence/OnlyContain/StringStartEnd: vereist per Shouldly-overload verificatie met proofs (nullable-enabled), eigen taak waard. |
| M6 | Opgelost | `RewriteLink` doet `Find` op de ouder van het schakelstatement in plaats van het hele bestand. Bewust niet: alle ketens in één fork (optioneel). |
| M7 | Deels | `HasUnusedRemovedUsingAsync` en `MayUseRemovedNamespaceAsync` binden pas als het syntactisch nodig is (bestanden zonder using van een verwijderde namespace slaan het binden over). Bewust niet: cache over de drie passes; de compilation verschilt per pass (na elk bestand, na de global usings), dus hergebruik zou semantisch fout kunnen zijn. `ProjectPackages` niet aangeraakt (andere agent, TAAK-060/063). |
| M8 | Opgelost | `FluentAssertionsSyntax.TryGetSplittableStatement` + `NotSplittableReason`, gebruikt door AllSatisfy, OnlyContain en AndChain. OnlyContain vindt nu net als AllSatisfy altijd het statement. |
| L1 | Opgelost | `FluentAssertionsSyntax.Arguments`/`Invoke` (zonder trivia, spatie na komma) in de geraakte patronen. `ThrowMessage.Arguments` blijft voor de Throw-familie (behoudt trivia van `because`). |
| L2 | Opgelost | Base splitst becauseArgs; een afwijkend aantal waarden heeft een eigen reden (BeGreaterLess, BeApproximately, BeEquivalentToString). Throw/NotThrow: bewust niet (Throw-familie niet gemigreerd). |
| L3 | Opgelost | AndChain accepteert een schakel die meerdere statements geeft. Unittest `Rewrite_LinkWithSeveralStatements_KeepsAll`. |
| L4 | Opgelost | `IComparable<T>` via `Compilation.GetTypeByMetadataName` (`System.IComparable` met arity 1). |
| L5 | Bewust niet | Optioneel en alleen relevant samen met M7-cache. |
| L6 | Bewust niet | `ProjectPackages.cs` is van de andere agent (merge-conflict vermijden); nit. |
| L7 | Bewust niet | Onzeker, vereist runtime-verificatie van Shouldly's `ShouldBe` op `object` met een lijst; eigen taak. |
| L8 | Opgelost | `UsingDirectives`: usings binnen een namespace-declaratie (blok en na file-scoped) worden geswapt/verwijderd en tellen voor `HasUnusedRemovedUsingAsync`/`KeepsRemovedUsing`. Proofs `ShouldBe/edge-using-in-namespace`, `ShouldBe/edge-using-after-file-scoped-namespace`. Overige shortcuts gelaten. |
| L9 | Geen actie | Geen schendingen. |
