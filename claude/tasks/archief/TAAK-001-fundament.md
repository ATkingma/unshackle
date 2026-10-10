# TAAK-001 — Fundament: contracten, Fixer, proof-runner + `fa.should-be`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-10
**Doel**: Core-contracten, `Fixer`, `FluentAssertionsFix`, proof-runner en het eerste patroon `fa.should-be`, met groene build en tests.

## Context voor de volgende agent

- Rij 0 in `claude/tasks/patroon-queue.md`; alle patroon-agents wachten hierop en mogen de contracten niet wijzigen.
- Code-standaarden: `claude/docs/code-standaarden.md`. Architectuur: `ARCHITECTURE.md` sectie "Binnen Core".
- Patroon-agents: gebruik `FluentAssertionsSyntax` (zie Keuzes) en kijk naar `ShouldBePattern` als voorbeeld.

## Analyse

### Huidige situatie

Alleen lege mappen met `.gitkeep`; `src/Unshackle.Interface/Program.cs` geeft `0` terug. Geen tests (exitcode 8).

### Probleem

Zonder contracten en proof-runner kan geen patroon-agent beginnen of zijn werk bewijzen.

### Nieuwe situatie

`IPattern`/`IFix`/`IFixer`, modellen, `Fixer`, `FluentAssertionsFix` met `ShouldBePattern`, testhelpers, `ProofTests` en `FixerTests`.

### Verbetering

Een nieuw patroon = één klasse + één regel in `Patterns` + proofbestanden. Proofs controleren dat before met FA en after met Shouldly compileert.

## Aanpak

### Wat / Waar

- `src/Unshackle.Core/Abstractions/` `IPattern.cs`, `IFix.cs`, `IFixer.cs`
- `src/Unshackle.Core/Models/` `Finding.cs`, `PatternResult.cs`, `FixedResult.cs`, `ManualResult.cs`, `FixRequest.cs`, `FixReport.cs`
- `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Core/UsingDirectives.cs`
- `src/Unshackle.Core/Fixes/FluentAssertions/` `FluentAssertionsFix.cs`, `FluentAssertionsSyntax.cs`, `Patterns/ShouldBePattern.cs`
- `src/Unshackle.Tests/Helpers/` `TestReferences.cs`, `TestSolution.cs`; `Core/FixerTests.cs`; `ProofTests.cs` + `ProofFix.cs` (projectroot: `Proofs/**` wordt niet gecompileerd); `Proofs/FluentAssertions/ShouldBe/*`

### Waarom deze aanpak

Zie **Keuzes**.

## Keuzes

| # | Keuze | Reden |
|---|---|---|
| 1 | `IPattern { Id; Description; IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model); PatternResult Rewrite(SyntaxNode node, SemanticModel model); }`, synchroon, zonder CancellationToken | Roslyn-werk op één boom is CPU-bound; de Fixer checkt het token tussen patterns. `Find` geeft óók nodes die alleen Manual kunnen (die komen zo in het rapport) |
| 2 | Een pattern kiest zelf welke node het vervangt (expressie, `.WithMessage`-keten, of hele statement) | `Throw` (statementvorm verandert) en ketens: Find geeft de buitenste invocatie of het statement terug |
| 3 | `FixedResult(IReadOnlyList<SyntaxNode> NewNodes, IReadOnlyList<string> RequiredNamespaces)`; fabrieken `Fixed(node, params namespaces)` en `Fixed(IEnumerable<SyntaxNode>, params namespaces)` | Meerdere nodes = statement splitsen (`.And`-ketens). Namespaces: eis uit TAAK-002 (`Count()` → `System.Linq`). Afwijking van het voorbeeld in code-standaarden §2, bewust |
| 4 | Herschrijven in de Solution: per document, per pattern op volgorde, steeds met een vers SemanticModel. Alle `Rewrite`s op de originele boom, dan `TrackNodes` + `ReplaceNode` in documentvolgorde (buitenste eerst); overlap → Manual "run the fix again". `Solution.WithDocumentSyntaxRoot` | Immutable Roslyn: input-solution blijft intact (dry-run gratis). Patterns zien elkaars resultaat; geen conflicten tussen patterns |
| 5 | De Fixer zet trivia: één node → `WithTriviaFrom(origineel)`; meerdere → eerste krijgt leading trivia, de rest alleen inspringing, allemaal de trailing trivia | Patterns bouwen kale nodes; consistente opmaak |
| 6 | Usings op documentniveau in de Fixer (`UsingDirectives`): `IFix.AddedNamespaces` (`Shouldly`) + `RequiredNamespaces` van de results worden toegevoegd als ze er niet staan (ook niet als `global using` in de compilatie); `IFix.RemovedNamespaces` (`FluentAssertions`) gaat weg als geen simple name in het document nog naar die namespace bindt. Bij weghalen + toevoegen wordt de using op dezelfde plek omgezet | Eén keer per document i.p.v. per pattern; nooit FA weghalen zolang er nog FA-code (bijv. Manual) staat. Global usings en usings in namespace-blokken blijven ongemoeid (shortcut) |
| 7 | `Finding { FilePath, Line (1-based, 0 = hele bestand), FixId, PatternId, Result }`; `FixReport { Solution, Findings }`; `FixRequest { FixId }` | Dekt de CSV-kolommen uit ARCHITECTURE.md; `Result` geeft Status + Reason |
| 8 | Exceptions in `Find`, `Rewrite` én bij het vervangen → `Manual("Internal error in <id>: …")` | Code-standaarden §8 |
| 9 | Hulpklasse `FluentAssertionsSyntax`: `FindInvocations(root, model, names)` (alleen FA-symbolen), `GetSubject` (x in `x.Should().M()`), `IsResultDiscarded` (statement of void-lambda), `TryGetArguments` (waarden + `because`; false bij `becauseArgs`/named args, via `IInvocationOperation`) | 14 FA-patronen hebben dit allemaal nodig; patroon-agents mogen alleen hun eigen bestand wijzigen |
| 10 | `fa.should-be`: `x.ShouldBe(y[, because])` met de originele argumentlijst. Manual bij: niet `x.Should().Be`, resultaat gebruikt (`.And`), `becauseArgs`/named args, comparer, `FluentAssertions.Xml`/`.Types` (andere gelijkheid), typeverschil zonder veilige vorm (bijv. `int` vs `int?`, Guid vs string) | `ShouldBe<T>` leidt T af uit beide kanten en de receiver mag geen numerieke/nullable conversie; FA converteerde het argument |
| 11 | Type-afhankelijk: constante die alleen impliciet naar het subjecttype converteert krijgt een cast (`byte`: `(byte)1`, enum: `(Status)0`); `long` met `1` blijft zonder cast | Anders compileert `b.ShouldBe(1)` niet (T = int) |
| 12 | Proofs: `ProofTests` draait per proof alleen het pattern uit de mapnaam (`<Map>Pattern`) via een NSubstitute-`IFix` met de namespaces van de echte fix. `before` compileert tegen FA, `after` exact gelijk én compileert tegen alleen Shouldly; `manual.txt` = alle findings Manual, reden bevat de tekst, document ongewijzigd | Isolatie tussen parallelle patroon-agents. Een proof met nog-FA-code naast Shouldly past daardoor niet: dat dekt `FixerTests` |
| 13 | Testnamen `Method_State_Expected` (**voorlopig**); CA1707 uit voor `src/Unshackle.Tests/**` in `.editorconfig` | ARCHITECTURE.md: testnamen nog te beslissen |
| 14 | `TestSolution` gebruikt één statische `AdhocWorkspace` | Solution is immutable; geen dispose-gedoe |

## Stappen

- [x] 1. Onderzoek FA 7.2.2 `Be` / Shouldly 4.3.0 `ShouldBe` en samples
- [x] 2. Contracten en modellen
- [x] 3. Fixer + usings
- [x] 4. `FluentAssertionsFix`, helper, `ShouldBePattern`
- [x] 5. Testhelpers, `ProofTests`, proofs, `FixerTests`
- [x] 6. Build + test groen (17 tests), afronden

## Risico's & randgevallen

| Risico                                                         | Mitigatie                                                                     |
| -------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Overlappende findings van één pattern                          | Fixer past toe in documentvolgorde; overlap → `Manual` ("run the fix again")  |
| `using FluentAssertions` weghalen terwijl er nog FA-code staat | Alleen weghalen als geen symbool in het document nog naar die namespace bindt |

## Gewijzigde bestanden

- Core: `Abstractions/{IPattern,IFix,IFixer}.cs`, `Models/{Finding,PatternResult,FixedResult,ManualResult,FixRequest,FixReport}.cs`, `Fixer.cs`, `UsingDirectives.cs`, `Fixes/FluentAssertions/{FluentAssertionsFix,FluentAssertionsSyntax}.cs`, `Fixes/FluentAssertions/Patterns/ShouldBePattern.cs`
- Tests: `Helpers/{TestReferences,TestSolution}.cs`, `Core/FixerTests.cs`, `ProofTests.cs`, `ProofFix.cs`, `Proofs/FluentAssertions/ShouldBe/` (simple, because, constant-cast, lambda; manual: chain, because-args, nullable-expected, comparer, xml)
- `.editorconfig` (CA1707 uit in tests), `.gitkeep`s weg in gevulde mappen

### Open punten

- FA-onderzoek `Be`: FA 7.2.2 heeft `Be(expected, because, params becauseArgs)` op 20 assertion-types; `ObjectAssertions` ook met `IEqualityComparer`. Samples: `OrderTests.cs:24,61`, `OrderHandlerTests.cs:26,27,69` (lambda in `AllSatisfy`).
- Csproj-werk (FA-package eruit, Shouldly erin, `<Using Include="FluentAssertions" />`) valt buiten Roslyn: later in DataAccess/`ISolutionStore`.
- `ISolutionStore`/`IReportWriter` en de CLI-wiring zijn nog niet gebouwd (buiten rij 0).

## Zie ook

- `claude/docs/INDEX.md`
- `ARCHITECTURE.md`
- `claude/docs/patroon-loop.md`
