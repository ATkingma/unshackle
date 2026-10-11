# TAAK-071 — fa.be-equivalent-to-object: `BeEquivalentTo(params T[])` op een collectie

**Status**: voltooid
**Aangemaakt**: 2026-10-11
**Gevonden in**: corpus-run FluentResults (50× in één project, `docs/projectinfo/architectuur/corpus.md`)

## Probleem

`names.Should().BeEquivalentTo("a", "b")` gebruikt de `params TExpectation[]`-overload van
`GenericCollectionAssertions`. `BeEquivalentToObjectPattern` leest de extra argumenten als `becauseArgs` en geeft
Manual "with becauseArgs or named arguments". Die reden is fout: er is geen `because`.

## Wat, waar, hoe

- **Waar**: argumentcontrole in `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`.
- **Hoe (minimaal)**: params-overload herkennen (parameter `params`-array in het symbool) → bestaande collectie-reden
  ("IEnumerable is a collection…"). **Later** (eigen keuze): voor value-elementen (string, getallen, enums) Fixed naar
  `x.ShouldBe(new[] { "a", "b" }, ignoreOrder: true)`.
- Proof (pending): `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/BeEquivalentToObject/edge-collection-params.*`.

## Stappen

- [x] Overload-herkenning via `IMethodSymbol.Parameters[0].IsParams`
- [x] Proof terugzetten, groen; keuze Fixed-variant vastleggen
- [x] `dotnet build` + `dotnet test` groen

## Resultaat

Params-overload → Manual met collectie-reden. Fixed-variant (`ShouldBe(..., ignoreOrder: true)`) niet gebouwd en niet geprobed.

## Zie ook

- `docs/projectinfo/architectuur/corpus.md`
- `claude/tasks/TAAK-049-fa-equivalent-runtime-type.md`
