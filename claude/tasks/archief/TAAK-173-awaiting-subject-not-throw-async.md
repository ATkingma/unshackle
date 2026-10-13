# TAAK-173 — `x.Awaiting(..).Should().NotThrowAsync()` herschrijven

**Status**: klaar
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

- [x] 1. Proofs: Awaiting op een lokale variabele, `FluentActions.Awaiting`, en een receiver met side effect (→ Manual).
- [x] 2. De lambda-opbouw uit `InvokingPattern` hergebruiken in `NotThrowAsyncPattern`.
- [x] 3. Bewijs: Polly-harnas.

## Resultaat

- Polly 7.2.4 (`b1a5a17`, 2026-10-13, harnas zonder kloon-wijzigingen): Fixed 4449 → 5255, Manual 964 → 171, build 0 fouten, tests 5685/5685 → 5685/5685, 0 regressies.
- `NotThrowAsyncPattern` gebruikt `ThrowSyntax.TryGetAction`; `FluentActions.Invoking/Awaiting(d)` → `d`; een door de fix geschreven lambda slaat de delegate-typecheck over (waardetype-resultaten Fixed, ook voor Throw/ThrowAsync via Invoking). Proofs `NotThrowAsync/awaiting*`, `fluent-actions-lambda`, `Invoking/fluent-actions`, `value-result`.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
