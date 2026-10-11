# TAAK-140 — Code die FA-/xunit-exceptions vangt breekt na omzetting

**Status**: klaar (gemerged `d252749`, Polly 0 regressies `e8eb882`)
**Aangemaakt**: 2026-10-11 (regressie-run, `claude/tasks/REGRESSIE-corpus.md`)

## Context

Polly (26 regressies): `Within(TimeSpan, Action)` in `RateLimitSpecsBase` draait de assertions in een lus en vangt alleen `AssertionFailedException` of `XunitException` om opnieuw te proberen (`if (!(e is AssertionFailedException || e is XunitException)) throw;`). Na de fix gooit Shouldly `ShouldAssertException` → wordt doorgegooid → geen retry → test faalt.
Zelfde klasse als TAAK-070 (verwachte assertion-exception), maar nu via een `catch`/`is`-filter in eigen code in plaats van `Throw<T>`.

## Stappen

1. Proof (_Pending → Manual): helper met `catch` + `is AssertionFailedException` en een assertion in de doorgegeven lambda.
2. Detectie: in de solution code die FA-exceptiontypes (`FluentAssertions.Execution.AssertionFailedException`) of testframework-assertie-exceptions vangt/filtert (`catch (T)`, `is T`, `when (e is T)`); assertions in lambda's die aan zo'n methode worden doorgegeven → Manual met uitleg, plus de catch-plek zelf als Manual ("vangt ook `Shouldly.ShouldAssertException` toevoegen").
3. Alternatief onderzoeken: de catch-plek automatisch uitbreiden met `|| e is Shouldly.ShouldAssertException` (veilig, semantisch gelijk) — voorkeur als bewijsbaar.
4. Bewijs: harnas op Polly → 0 regressies.

## Verslag (2026-10-11)

- Gekozen voor stap 3 (automatisch uitbreiden): nieuw patroon `fa.catch-assertion-exception` (`CatchAssertionExceptionPattern`). `e is T` → `e is T || e is ShouldAssertException`; `catch (T)` zonder gebruikte variabele → extra `catch (ShouldAssertException)`; gebruikte variabele of is-pattern → Manual. T = `AssertionFailedException` of `XunitException`. Het oude type blijft (Manual-assertions blijven gevangen); idempotent bij tweede run.
- Proofs `Proofs/FluentAssertions/CatchAssertionException/` (4), unittests `CatchAssertionExceptionPatternTests` (3, waaronder exact de Polly-regel). `ProofTests`: een `after` die een verwijderde namespace houdt compileert tegen oud + nieuw.
- Docs: `docs/projectinfo/patterns/fa-catch-assertion-exception-{plan,modellen,verify}.md`, rij in README.
- `dotnet build` 0 warnings, `dotnet test` 700/700, SampleShop 38/38.
- Stap 4 (harnas Polly) niet gedraaid: in het corpus staat geen ongewijzigde Polly-bron, alleen de omgezette `out/polly/work`.

## Zie ook

- `docs/projectinfo/patterns/fa-catch-assertion-exception-plan.md`

- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md`
