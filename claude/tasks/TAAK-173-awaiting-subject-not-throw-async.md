# TAAK-173 — `x.Awaiting(..).Should().NotThrowAsync()` herschrijven

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: 131 Manuals in Polly met de reden "The subject is built with FluentAssertions (e.g. FluentActions.Awaiting)" worden Fixed.

## Context voor de volgende agent

- Polly 7.2.4, `src/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerAsyncSpecs.cs:201`: `await breaker.Awaiting(x => x.ExecuteAsync(() => TaskHelper.EmptyTask)).Should().NotThrowAsync();`
- `InvokingPattern` herschrijft `Awaiting(..).Should().ThrowAsync<T>()` al; `NotThrowAsyncPattern` doet dat nog niet.
- Dezelfde receiver-regel als in `InvokingPattern`: alleen een lokale variabele, parameter, veld of `this`, anders Manual.

## Nieuwe situatie

- → `await Should.NotThrowAsync(() => breaker.ExecuteAsync(() => TaskHelper.EmptyTask));`
- `FluentActions.Awaiting(() => …)` → de lambda wordt direct doorgegeven.

## Stappen

- [ ] 1. Proofs: Awaiting op een lokale variabele, `FluentActions.Awaiting`, en een receiver met side effect (→ Manual).
- [ ] 2. De lambda-opbouw uit `InvokingPattern` hergebruiken in `NotThrowAsyncPattern`.
- [ ] 3. Bewijs: Polly-harnas.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
