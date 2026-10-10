# Flow: Fixer.FixAsync

**Figure 1.** Eén document in `FixAsync`: elk pattern draait op de uitkomst van het vorige, daarna volgen de usings. Weggelaten: annulering, onbekende fix (`ArgumentException`) en documenten van niet-C#-projecten (worden overgeslagen).

<!-- kingmadoc:diagram img/fix-flow-1.d2 -->
![Flow: Fixer.FixAsync](img/fix-flow-1.png)

| Stap | Plek in `src/Unshackle.Core/Fixer.cs` | Wat gebeurt er |
| ---- | ------------------------------------- | -------------- |
| Fix kiezen | `FixAsync` | Zoekt `IFix` op `FixId` (hoofdletterongevoelig) |
| Find | `ApplyPattern` | Een exception wordt één `Manual`-bevinding met regel 0 |
| Rewrite | `Rewrite` | Een exception wordt `Manual("Internal error in …")` |
| Vervangen | `Replace`, `ReplaceWithTrivia` | Overlap met een eerdere vervanging wordt `Manual`; trivia komt van de oude node |
| Usings | `UsingDirectives.UpdateAsync` | Alleen als het document veranderd is; namespaces van de resultaten plus `AddedNamespaces` |

## Zie ook

- [klassen.md](klassen.md) — de betrokken contracten
- [pattern-beslissing.md](pattern-beslissing.md) — wat een pattern in `Rewrite` beslist
- [../README.md](../README.md)
