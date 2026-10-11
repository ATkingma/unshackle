# TAAK-100 — AssertionScope → `ShouldSatisfyAllConditions`

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Bron**: `claude/tasks/ANALYSE-fa-100.md` (acties 5–10)

## Keuzes

`AssertionScopePattern` draait nu als eerste. Veilige vorm: `using (new AssertionScope()) { … }` of `using var s = new AssertionScope();` (één scope in het blok, variabele verder niet gebruikt), zonder argumenten, en élk statement is een FA-assertion-statement zonder `await` en zonder ref/out-variabelen. Dan `x.ShouldSatisfyAllConditions(() => { a; }, () => { b; });` (receiver = eerste veilige subject; Shouldly print hem alleen) en de assertion-patronen herschrijven de lambda's. Block-bodies omdat Shouldly tijdens de fix nog niet gerefereerd is (expression-lambda's hebben dan geen delegate-type). `FluentAssertions.Execution` staat nu in `RemovedNamespaces` (alleen weg als ongebruikt).
Manual blijft: context-argument, Discard/scope-variabele, andere statements, await.

## Bewijs

Probe: beide verzamelen alle assertion-fouten (2 fouten → 2 meldingen), ook een niet-assertion-exception en een falende Throw; een null-receiver slaagt.
Probe-code: scratchpad `p096/` (FA 7.2.2 + Shouldly 4.3.0, reflectie op `Shouldly.dll`).

## Proofs

_Combined/scope-using-block, scope-using-var (Fixed); AssertionScope/context, other-statement, discard, await (Manual)

## Resultaat

`dotnet build`, `dotnet test` (621) en de SampleShop-tests (38) groen. Scan: `ANALYSE-fa-100.md` sectie "Na acties 5–10".

## Zie ook

- `claude/tasks/ANALYSE-fa-100.md`
- `docs/projectinfo/architectuur/proofs.md`
