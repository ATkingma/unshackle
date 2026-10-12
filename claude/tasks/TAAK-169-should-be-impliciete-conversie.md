# TAAK-169 — `Be(x)` met een eigen impliciete conversie bouwt niet

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (corpus-run, `claude/tasks/CORPUSRUN-fa.md`)
**Doel**: `bsonValue.Should().Be(42)` wordt `bsonValue.ShouldBe((BsonValue)42)` (of Manual), niet een `ShouldBe(42)` die niet compileert.

## Context voor de volgende agent

- LiteDB (`7b71bc4`): 11 regels breken de build (80× CS1929, 8× CS0411). Voorbeelden: `LiteDB.Tests/Document/Json_Tests.cs:71` (`JsonSerializer.Deserialize(..).ShouldBe(positiveInt32)`), `Database/Document_Size_Tests.cs:39` (`d0["name"].ShouldBe("John")`) en `Issues/Issue2471_Test.cs:27` (`ShouldBeGreaterThan(0)`). Het subject `BsonValue` heeft `implicit operator BsonValue(int/string)`.
- Baseline: 4 925 tests groen per TFM. Dit is de enige reden dat LiteDB na de fix niet bouwt.

## Analyse

### Huidige situatie

`FluentAssertionsSyntax.ConvertExpected` (gebruikt in `ShouldBePattern.cs:46` en `BeGreaterLessPattern`) accepteert een impliciete conversie. Typeinferentie van `ShouldBe<T>` gebruikt echter geen conversies die de gebruiker zelf definieert.

### Probleem

Inferentie vindt geen `T`, dus CS1929/CS0411.

### Nieuwe situatie

Bij een eigen impliciete conversie (`Conversion.IsUserDefined`) zet de fix een expliciete cast naar het subjecttype. FA `ObjectAssertions.Be` gebruikt `subject.Equals(expected)`; met de cast vergelijkt Shouldly `BsonValue` met `BsonValue`. Overschrijft het type `Equals(object)` niet, dan Manual.

### Verbetering

LiteDB bouwt na de fix, en de 6 874 Fixed-findings komen echt in de code terecht.

## Stappen

- [ ] 1. Proof met een eigen type met `implicit operator` (`Be` + `BeGreaterThan`).
- [ ] 2. Cast toevoegen in `ConvertExpected`.
- [ ] 3. Bewijs: LiteDB-harnas met `EXTRA=-m:1` (parallel bouwen geeft een file-lock in LiteDB.Fuzz), 0 regressies.

## Zie ook

- `claude/tasks/CORPUSRUN-fa.md`
