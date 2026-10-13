# TAAK-198 — Conflict-Manual bij eigen `Should*`-extensies verkleinen (Akka: 3 449 findings)

**Status**: ✅ voltooid (2026-10-13)
**Aangemaakt**: 2026-10-13 (TAAK-174 stap 3, Akka-herhaling)
**Doel**: bestanden met een eigen `ShouldBe`/`ShouldBeTrue` in scope (Akka.TestKit) toch grotendeels migreren, zonder CS0121 en zonder volledig gekwalificeerde Shouldly-aanroepen.

## Context voor de volgende agent

- Na TAAK-174 bouwt Akka.NET (`23a8566`) groen, maar de prijs is hoog: **3 449 van 5 126 Manuals** zijn "Not rewritten: Shouldly.… is ambiguous with Akka.TestKit.AkkaSpecExtensions.… once Shouldly is imported (CS0121)". Fixed daalde van 8 488 (kapotte build) naar 5 463.
- Verdeling van die reden: `ShouldBe` 2 018, `ShouldBeTrue` 1 114, `ShouldBeFalse` 172, `ShouldBeGreater…` 104, rest klein.
- De conflictcheck markeert het **hele bestand** Manual, ook de calls waarvan de Shouldly-naam niet botst (bijv. `Throw` → `Should.Throw`, `Contain` → `ShouldContain`).
- Volledig gekwalificeerde aanroepen (`Shouldly.ShouldBeTestExtensions.ShouldBe(x, y)`) zijn in TAAK-174 bewust afgewezen.

## Analyse

### Mogelijke aanpakken

1. **Gerichte `using static`**: in plaats van `using Shouldly;` alleen de Shouldly-klassen importeren die geen botsende namen bevatten (`using static Shouldly.ShouldThrowExtensions;` e.d.; `Should` als type via `using Should = Shouldly.Should;`). Calls die op een botsende naam uitkomen blijven Manual; de rest wordt Fixed. Let op: `using static` brengt alle extensies van die klasse binnen, dus per klasse controleren.
2. **Eigen extensie hergebruiken**: als de eigen methode semantisch hetzelfde doet (Akka `ShouldBe<T>(T, T, string)` = gelijkheid, `ShouldBeTrue(bool, string)`), `x.Should().Be(y)` herschrijven naar de eigen `x.ShouldBe(y)` zonder Shouldly-import. Vergt een whitelist of een semantische check (signatuur + naam); risico op ander gedrag (berichten, `IEquatable`).
3. Per bestand: alleen Manual als minstens één herschreven call een botsende naam nodig heeft (nu: zodra er een botsende extensie in scope is?) — eerst nagaan wat de huidige check precies doet in `UsingDirectives.cs`/Fixer.

### Overige foutcodes uit de eerste Akka-run

De 8× CS8130, 8× CS0119, 6× CS1061 en 6× CS0452 uit de eerste run zijn na TAAK-174 verdwenen (build 0 fouten). Het opgeslagen `build1.log` van die run bevat ze niet meer per regel, dus de exacte plekken zijn niet te herleiden; ze zaten vrijwel zeker in bestanden die nu Manual zijn (gevolgfouten van dezelfde dubbelzinnigheid, bijv. inferentie bij deconstructie en methodgroepen). Als aanpak 1 of 2 die bestanden weer migreert, opnieuw op deze codes letten.

## Stappen

- [x] 1. Huidige conflictcheck lezen (`Fixer.cs`, `UsingDirectives.AmbiguityWithAddedAsync`: na de rewrite, op het herschreven document; één dubbelzinnige call → hele bestand teruggedraaid) en bepalen waar per call ingegrepen kan worden.
- [x] 2. Unittests: bestand met eigen `ShouldBe` + FA-calls die wel/niet botsen.
- [x] 3. Aanpak 1 (gerichte imports) implementeren; aanpak 2 alleen na overleg met de gebruiker.
- [x] 4. Bewijs: Akka-herhaling bouwt groen, Fixed stijgt, 0 testregressies; let op CS8130/CS0119/CS1061/CS0452.

## Resultaat

- Gekozen: aanpak 1 (gerichte imports), datagedreven. Pas als de bestaande check (TAAK-174/199) een conflict vindt, draait `Fixer.FixWithTargetedImportsAsync`: geen `using Shouldly;` maar `using static` per gebruikte Shouldly-klasse plus een alias per genoemd type (`using Should = Shouldly.Should;`). Rondes: CS0121 → die klasse blijft weg en die naam wordt geblokkeerd; misbinding → naam geblokkeerd; een herschrijving die een geblokkeerde naam aanroept wordt per call Manual (`Fixer.BlockedCall`) en blijft FA. Controle zonder de globale model-using; komt er een andere compileerfout bij, dan de oude terugval per bestand. Aanpak 2 niet gedaan.
- Code: `Fixer.cs` (`FixFileAsync` uit de lus gehaald, `FilePass`), `UsingDirectives.cs` (`ConflictsWithAddedAsync`, `AddedUsageAsync`, `AddTargetedImports`, `ErrorCountAsync`), `AddedConflict`, `AddedUsage`.
- Tests: 3 nieuwe in `AmbiguousAddedNamespaceTests` (gemengd bestand, namespace-variant, terugval bij botsende alias); proof `Proofs/FluentAssertions/BeTrueFalse/own-shouldbe-in-scope.*`. 1066 tests groen.
- Akka (`23a8566`, schoon, restore, fix, build): Fixed 5 463 → **6 016**, Manual 5 518 → **4 649**; conflict-Manuals 3 449 → 2 943 (nu per call: `ShouldBe` 1 832, `ShouldBeTrue` 506, `ShouldBeOfType` 252 (klasse weggelaten), `ShouldBeFalse` 146 …). 584 bestanden geschreven (was 431). Build **0 fouten** (geen CS8130/CS0119/CS1061/CS0452). Tests gelijk aan de eerdere logs (`t2-akka`, `t2-ddata`, t1): Akka.Tests 1553/0 gefaald, Akka.TestKit.Tests 337/0, DData 201/1 gefaald — dezelfde bekende `ReplicatorMessageSerializerSpec` (TAAK-200, waarschuwing). 0 regressies.

## Zie ook

- `claude/tasks/archief/TAAK-174-shouldly-dubbelzinnig-met-eigen-shouldbe.md`
- `claude/tasks/CORPUSRUN-fa.md`
