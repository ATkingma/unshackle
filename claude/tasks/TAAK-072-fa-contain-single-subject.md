# TAAK-072 — fa.contain-single-which: `.Subject` naast `.Which`

**Status**: open
**Aangemaakt**: 2026-10-11
**Gevonden in**: corpus-grep (`ContainSingle(...).Subject` 63×, vooral Akka.NET; `docs/projectinfo/architectuur/corpus.md`)

## Probleem

`var x = list.Should().ContainSingle().Subject;` is in FA hetzelfde als `.Which`, maar `ContainSinglePattern`
herkent alleen `.Which` en geeft Manual. `BeOfTypePattern` kent `.Subject` al (proof `BeOfType/which`).

## Wat, waar, hoe

- **Waar**: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ContainSinglePattern.cs`, de `.Which`-check.
- **Hoe**: `Subject` als synoniem van `Which` behandelen, zoals `BeOfTypePattern`.
- Proof (pending): `src/Unshackle.Tests/Proofs/_Pending/FluentAssertions/ContainSingle/edge-subject.*`.

## Stappen

- [ ] `.Subject` toevoegen
- [ ] Proof terugzetten, groen
- [ ] `dotnet build` + `dotnet test` groen

## Zie ook

- `docs/projectinfo/architectuur/corpus.md`
