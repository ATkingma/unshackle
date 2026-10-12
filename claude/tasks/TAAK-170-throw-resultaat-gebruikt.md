# TAAK-170 — `Throw<T>().And/.Which/.Where` herschrijven via de teruggegeven exception

**Status**: stap 1/4 — nog te starten
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: de grootste Manual-groep van het corpus (499×, waarvan 428 in Polly) wordt Fixed.

## Context voor de volgende agent

- Manual-reden "The result of Throw<T>() is used (.Which/.And/.WithInnerException/.Where or a value)" komt uit `ThrowPattern.cs:60`, `ThrowAsyncPattern`, `ThrowExactlyPattern` en `InvokingPattern`.
- Voorbeeld uit Polly 7.2.4, `src/Polly.Specs/Bulkhead/BulkheadAsyncSpecs.cs:26`: `policy.Should().Throw<ArgumentOutOfRangeException>().And.ParamName.Should().Be("maxParallelization");`
- Shouldly's `Should.Throw<T>(act)` en `await Should.ThrowAsync<T>(act)` geven de exception terug.

## Nieuwe situatie

- `.And.M` / `.Which.M` → `Should.Throw<T>(act).M`; de rest van de keten loopt via de gewone patronen.
- `.Where(e => p)` → `Should.Throw<T>(act).ShouldSatisfyAllConditions(e => p(e).ShouldBeTrue())` of `ShouldMatch`: kies de vorm met de duidelijkste foutmelding.
- `.WithInnerException<TI>()` → `….InnerException.ShouldBeAssignableTo<TI>()` (in FA is het inner type assignable, niet exact).
- Meerdere vervolgstappen → `var ex = Should.Throw<T>(act);` als statement, gevolgd door losse asserties.

## Verbetering

Ongeveer 500 Manuals minder in het corpus, ongeveer 1,3 procentpunt meer Fixed.

## Stappen

- [ ] 1. Proofs per vorm: And/Which-member, Where, WithInnerException, en combinatie met WithMessage.
- [ ] 2. Gedeelde helper in `ThrowSyntax.cs` voor sync, async, exactly en invoking.
- [ ] 3. Manual laten voor: resultaat als waarde doorgegeven, of `Where` met side effects.
- [ ] 4. Bewijs: Polly-harnas, 0 regressies.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
