# TAAK-096 — `.Which`/`.Subject` na waarde-teruggevende assertions

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (acties 5–10)

## Keuzes

Geen nieuw patroon: `FluentAssertionsSyntax.WithWhich` accepteert `.Which`/`.Subject` nu overal (niet alleen `var y = ….Which;`), dus `fa.be-of-type` (BeOfType/BeAssignableTo) en `fa.contain-single-which` vervangen de hele `….Which` door de Shouldly-call die dezelfde waarde teruggeeft (`ShouldBeOfType<T>()`, `ShouldHaveSingleItem()`, `Where(p).ShouldHaveSingleItem()`). `fa.throw` (ook ThrowExactly/Invoking) doet hetzelfde voor `.Which` → `Should.Throw<T>(act)`; `.Subject` daar niet (dat zijn alle exceptions).
Manual blijft: `.And.Which`/`.And` (AndConstraint heeft geen waarde in Shouldly), `.WithMessage(..).Which`, `ThrowAsync(..).Which` (await-vorm), `ContainKey(..).WhoseValue` (`dict[k]` gooit KeyNotFoundException i.p.v. een assertion).

## Bewijs

Probe: `BeOfType<D>().Which.A` = `ShouldBeOfType<D>().A` (pass/fail gelijk), ook `BeAssignableTo`, `ContainSingle().Which/.Subject`, `ContainSingle(p).Which`, `Throw<T>().Which.Message`. Reflectie: `ShouldBeOfType<T>`/`ShouldBeAssignableTo<T>`/`ShouldHaveSingleItem<T>` geven `T`, `Should.Throw<T>` geeft `TException`.
Probe-code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflectie op `Shouldly.dll`).

## Proofs

BeOfType/which-chain, which-multiple, ContainSingle/which-chain, Throw/edge-which-message, Throw/value-used, ThrowExactly/value-used (Manual → Fixed)

## Resultaat

`dotnet build`, `dotnet test` (621) en de SampleShop-tests (38) groen. Scan: `ANALYSE-fa-100.md` sectie "Na acties 5–10".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
