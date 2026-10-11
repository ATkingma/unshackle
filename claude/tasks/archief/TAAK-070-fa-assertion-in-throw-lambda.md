# TAAK-070 — fa.should-be (e.a.): assertion in een lambda waarvan de exception verwacht wordt

**Status**: klaar
**Aangemaakt**: 2026-10-11
**Gevonden in**: corpus-run FluentResults (`docs/projectinfo/architectuur/corpus.md`)

## Probleem

Testcode van assertion-extensies test dat een FA-assertion faalt:
`Action act = () => x.Should().Be(3); act.Should().Throw<XunitException>();` (of `AssertionFailedException`).
`fa.should-be` herschrijft de binnenste assertion naar `x.ShouldBe(3)`; die gooit `ShouldAssertException`, dus
`Throw<XunitException>` faalt. Gevolg in FluentResults: 2 groene tests rood (regressie, bouwt wel).

## Wat, waar, hoe

- **Waar**: gedeelde check in `FluentAssertionsSyntax` (alle patterns die een assertion in een lambda herschrijven).
- **Hoe**: assertion binnen een lambda die (via lokale variabele of direct) het subject is van `Throw<T>`/`ThrowExactly<T>`
  met `T` = `XunitException`/`AssertionFailedException`/`Exception`-basistype van die twee → Manual
  "the exception type of this assertion is checked: Shouldly throws ShouldAssertException".
- Proof (pending): `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/ShouldBe/edge-assertion-in-throw-lambda.*`.

## Stappen

- [x] Detectie (lokale `Action`/`Func`, directe lambda, Invoking/Awaiting, Shouldly `Should.Throw<T>`): `FluentAssertionsSyntax.IsInsideExpectedFailure` (binnenste assertion) en `ExpectsAssertionFailure` (buitenste Throw), beide Manual met `ExpectedFailureReason`. Veilig (geen Manual) als T `Exception` of `ShouldAssertException` is.
- [x] Proof uit `_Pending` naar `ShouldBe/` en `Throw/` (manual), groen
- [x] Probe (FA 7.2.2 + Shouldly 4.3.0 + xunit.assert): Shouldly-assertion in `Throw<XunitException>` faalt, in `Throw<Exception>` slaagt
- [x] `dotnet build` + `dotnet test` groen; FluentResults corpus-regressie (`corpus-regression.sh one`): 576/576 groen vóór en na, 0 regressies

## Zie ook

- `docs/projectinfo/architectuur/corpus.md` — corpus-run
- `docs/projectinfo/architectuur/edge-cases.md` — edge cases
