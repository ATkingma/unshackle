# TAAK-140 — Code die FA-/xunit-exceptions vangt breekt na omzetting

**Status**: open
**Aangemaakt**: 2026-10-11 (regressie-run, `claude/tasks/REGRESSIE-corpus.md`)

## Context

Polly (26 regressies): `Within(TimeSpan, Action)` in `RateLimitSpecsBase` draait de assertions in een lus en vangt alleen `AssertionFailedException` of `XunitException` om opnieuw te proberen (`if (!(e is AssertionFailedException || e is XunitException)) throw;`). Na de fix gooit Shouldly `ShouldAssertException` → wordt doorgegooid → geen retry → test faalt.
Zelfde klasse als TAAK-070 (verwachte assertion-exception), maar nu via een `catch`/`is`-filter in eigen code in plaats van `Throw<T>`.

## Stappen

1. Proof (_Pending → Manual): helper met `catch` + `is AssertionFailedException` en een assertion in de doorgegeven lambda.
2. Detectie: in de solution code die FA-exceptiontypes (`FluentAssertions.Execution.AssertionFailedException`) of testframework-assertie-exceptions vangt/filtert (`catch (T)`, `is T`, `when (e is T)`); assertions in lambda's die aan zo'n methode worden doorgegeven → Manual met uitleg, plus de catch-plek zelf als Manual ("vangt ook `Shouldly.ShouldAssertException` toevoegen").
3. Alternatief onderzoeken: de catch-plek automatisch uitbreiden met `|| e is Shouldly.ShouldAssertException` (veilig, semantisch gelijk) — voorkeur als bewijsbaar.
4. Bewijs: harnas op Polly → 0 regressies.

## Zie ook

- `claude/tasks/archief/TAAK-070-fa-assertion-in-throw-lambda.md`
