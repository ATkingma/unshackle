# TAAK-097 — becauseArgs centraal → `string.Format`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (acties 5–10)

## Keuzes

In `FluentAssertionsSyntax.TryGetArguments` (dus elk patroon dat de basisklasse of die helper gebruikt): becauseArgs worden `string.Format(System.Globalization.CultureInfo.InvariantCulture, because, args)` als customMessage. Patronen die `assertion.ArgumentList` hergebruikten (ShouldBe, Contain, ContainKey, HaveCount, HaveLength) bouwen de lijst nu uit `Values` + `Because`.
Manual blijft: named arguments, een `because` die geen constante is, of een format die niet past (FA slikt die FormatException, `string.Format` zou gooien), en AllSatisfy met because (kan niet in de loop).

## Bewijs

Probe onder nl-NL: FA formatteert `{0}` met 1.5 als "1.5" → InvariantCulture. Foute format `{1}` met één arg: FA slaagt/meldt WARNING; daarom vooraf `string.Format` op de constante proberen.
Probe-code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflectie op `Shouldly.dll`).

## Proofs

29 because-args-proofs Manual → Fixed; nieuw ShouldBe/because-args-bad-format en because-args-variable (Manual); AndChain/link-manual hernoemd naar link-because-args

## Resultaat

`dotnet build`, `dotnet test` (621) en de SampleShop-tests (38) groen. Scan: `ANALYSE-fa-100.md` sectie "Na acties 5–10".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
