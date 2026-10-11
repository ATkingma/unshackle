# TAAK-046 — Semantische audit van de herschrijvingen (casus PowerApps-Tooling)

**Status**: ✅ voltooid (read-only audit, geen code gewijzigd)
**Aangemaakt**: 2026-10-11
**Casus**: TAAK-019, `microsoft/PowerApps-Tooling` @ 630e6973, gefixte werkkopie (`git diff`), FA 7.2.0 → Shouldly 4.3.0

## Doel

Niet alleen "bouwt en tests groen", maar: faalt de Shouldly-vorm in precies dezelfde gevallen als het FA-origineel?

## Methode

- Steekproef per patroon-id uit `git diff` van de casus (≥5 per patroon, alle als minder).
- Gedrag gemeten, niet uit geheugen: scratch-console `scratchpad/semprobe` met de echte pakketten FluentAssertions **7.2.2** (casus: 7.2.0, zelfde major/minor-gedrag) en Shouldly **4.3.0**, elk geval in try/catch → PASS/FAIL.
- Throw-patronen zijn in de casus allemaal Manual (0 Fixed); die zijn tegen de patrooncode (`ThrowPattern.cs`, `ThrowAsyncPattern.cs`, `ThrowMessage.cs`) en de probe beoordeeld.

## Gemeten gedrag (probe)

| Geval | FA 7.2.2 | Shouldly 4.3.0 |
|---|---|---|
| `Throw<Base>` op `Derived` | PASS | PASS (`Should.Throw<Base>`) |
| `ThrowAsync<Base>` op `Derived` | — | PASS |
| `Throw<Derived>` op `AggregateException(Derived)` | **PASS** (pakt uit) | **FAIL** |
| `ThrowAsync<Derived>` op `AggregateException(Derived)` | **PASS** | **FAIL** |
| `WithMessage("hello world")` op "Hello World" | PASS | PASS (`ShouldBe(.., IgnoreCase)`) |
| `WithMessage("*WORLD")` / `("*lo Wo*")` | PASS | PASS (`ShouldEndWith/ShouldContain(.., Case.Insensitive)`) |
| `WithMessage("Hello World")` op "  Hello World\r\n" | FAIL | FAIL (geen trim in FA 7) |
| `WithMessage("a\nb")` op "a\r\nb" | **PASS** (regeleinden genegeerd) | **FAIL** (`ShouldBe(.., IgnoreCase)`) |
| `WithMessage("*line1\nline2*")` op "Line1\r\nLine2 end" | **PASS** | **FAIL** (`ShouldContain(.., Case.Insensitive)`) |
| idem met `IgnoreCase \| IgnoreLineEndings` | — | PASS |
| `WithMessage("value [x]")` | PASS (`[` geen wildcard) | n.v.t. |
| string `Contain("hello")` op "Hello" | FAIL (hoofdlettergevoelig) | `Case.Sensitive`: FAIL; **default: PASS** |
| string `Be("A")` op "a" / "a\r\nb" vs "a\nb" | FAIL / FAIL | FAIL / FAIL |
| `null.Be(null)` | PASS | PASS |
| `(0.1+0.2).Be(0.3)` | FAIL | FAIL (beide exact) |
| `DateTime` Utc vs Local, zelfde ticks | PASS | PASS (Kind genegeerd) |
| `AllSatisfy` op lege collectie | **FAIL** | (rewrite voegt `ShouldNotBeEmpty()` toe) |
| Shouldly-fout binnen FA `AllSatisfy`-lambda | FAIL | — (fout komt door) |
| `BeEmpty` op null (collectie/string) | FAIL | FAIL |
| `HaveCount(0)` op null | FAIL | FAIL (NRE via `.Count`) |
| `BeEquivalentTo` collectie andere volgorde | PASS | FAIL |
| `BeEquivalentTo` subject runtime-afgeleid type (`Pt2 : Pt`) | **PASS** | **FAIL** |

Conclusies:
- `Contain(string)` → `ShouldContain(.., Case.Sensitive)` is **correct en noodzakelijk**: Shouldly's default is hoofdletter-**on**gevoelig.
- `WithMessage("x")` → `ShouldBe(.., StringCompareShould.IgnoreCase)` is correct voor hoofdletters en `*`, **niet** voor regeleinden (TAAK-047).
- Shouldly `Throw<T>` accepteert afgeleide types, net als FA; de Manual-reden voor `ThrowExactly<T>` klopt.
- FA pakt `AggregateException` uit, Shouldly niet (TAAK-048).

## Steekproef per patroon

| Patroon | Voorbeeld (casus) | Oordeel | Bewijs |
|---|---|---|---|
| fa.should-be | `ErrorTests.cs`: `exception.Message.ShouldBe("Mismatch ... .png")` | gelijk | string ordinal, beide case- en regeleinde-gevoelig |
| fa.should-be | `MsappSerializationTests.cs`: `deserialized.LastPackedDateTimeUtc.ShouldBe(utcNow)` | gelijk | DateTime: beide `Equals`, Kind genegeerd |
| fa.should-be | `PaYamlSerializerTests.cs`: `...App!.Start.ShouldBe(new(1, 6))` | gelijk | struct `Equals` |
| fa.should-be | `PaArchivePathTests.cs`: `PaArchivePath.IsValidSegment(segment).ShouldBe(expectIsValid, "because ...")` | gelijk | because → customMessage, alleen melding |
| fa.should-be | `MsaprSerializationTests.cs`: `MsaprStructureVersion.ShouldBe(new Version(0, 1))` | gelijk | `Version.Equals` |
| fa.should-be | `PaArchivePathTests.cs`: `validSegment.ShouldBe(expectedValidSegment)` (soms null) | gelijk | null==null PASS in beide |
| fa.be-true-false | `ErrorTests.cs`: `errors.HasErrors.ShouldBeTrue()` | gelijk | triviaal |
| fa.be-true-false | `PaArchiveTests.cs`: `ContainsEntry("ENTRYa").ShouldBeTrue()` | gelijk | |
| fa.be-true-false | `MsappPackingServiceTests.cs`: `Directory.Exists(..).ShouldBeTrue("service should ...")` | gelijk | because → customMessage |
| fa.be-true-false | `TestBase.cs`: `JsonNode.DeepEquals(..).ShouldBeTrue($"...")` | gelijk | |
| fa.be-true-false | `PaArchivePathTests.cs`: `archivePath.IsDirectory.ShouldBeFalse()` | gelijk | |
| fa.be-null | `ErrorTests.cs`: `doc.ShouldBeNull()` | gelijk | |
| fa.be-null | `PaArchiveExtensionsExtractTests.cs`: `validFullPath.ShouldNotBeNull()` | gelijk | |
| fa.be-null | `PaArchiveTests.cs`: `paArchive.CreateEntry(entry).ShouldNotBeNull()` | gelijk | |
| fa.be-null | `MsappArchiveTests.cs`: `header.LastSavedDateTimeUTC.ShouldBeNull()` | gelijk | `Nullable<T>` |
| fa.be-null | `PaYamlSerializerTests.cs`: `.Which.ShouldNotBeNull()` | gelijk | |
| fa.have-count | `PaArchiveTests.cs`: `paArchive.Entries.Count.ShouldBe(12, "...")` | gelijk | null: FA FAIL, `.Count` NRE → beide falen |
| fa.have-count | `PaArchiveTests.cs`: `GetEntriesInDirectory(..).Count().ShouldBe(..)` | gelijk | LINQ `Count()` |
| fa.have-count | `PaYamlSerializerTests.cs`: `screen.Properties.ShouldNotBeNull().Count.ShouldBe(..)` | gelijk | TAAK-021 null-keten |
| fa.have-count | `NamedObjectMappingTests.cs`: `namedObjectMapping.Count.ShouldBe(3)` | gelijk | |
| fa.have-count | `PaYamlSerializerTests.cs`: `screen.DescendantControlInstances().Count().ShouldBe(..)` | gelijk | eenmalig opgesomd, zoals FA |
| fa.contain | `MsappSerializationTests.cs`: `json.ShouldContain("LoadFromYaml", Shouldly.Case.Sensitive, "...")` | gelijk | probe: FA Contain = Case.Sensitive |
| fa.contain | `PaArchiveTests.cs`: `.Which.StackTrace.ShouldNotBeNull().ShouldContain("Path.CheckInvalidPathChars", Case.Sensitive)` | gelijk | null: beide falen |
| fa.contain | `PaArchiveTests.cs`: `exThrown.StackTrace.ShouldNotBeNull().ShouldContain("PaArchive.EnsureEntriesInitialized", Case.Sensitive)` | gelijk | |
| fa.contain | `PaArchivePathTests.cs`: `invalidChars.ShouldContain('\b', "BS char ...")` (collectie) | gelijk | element-`Equals` |
| fa.contain | `PaArchivePathTests.cs`: `allSeparatorChars.ShouldContain(Path.DirectorySeparatorChar, "...")` | gelijk | |
| fa.be-empty | `PaArchivePathTests.cs`: `archivePath.FullName.ShouldBeEmpty()` (string) | gelijk | null: beide FAIL |
| fa.be-empty | `PaArchivePathTests.cs`: `invalidChars.ShouldNotBeEmpty()` | gelijk | |
| fa.be-empty | `PaArchiveTests.cs`: `paArchive.Entries.ShouldBeEmpty()` | gelijk | |
| fa.be-empty | `PaArchiveTests.cs`: `capturingLogger.EntriesByName(..).ShouldBeEmpty()` | gelijk | |
| fa.be-empty | `MsappPackingServiceTests.cs`: `msapp.Entries.ShouldNotBeEmpty("...")` | gelijk | |
| fa.all-satisfy | `PaArchiveTests.cs`: `ShouldNotBeEmpty(); foreach (var e in ..) e.Level.ShouldBe(LogLevel.Warning);` | gelijk | probe: FA AllSatisfy faalt op leeg |
| fa.all-satisfy | `MsappPackingServiceTests.cs`: `instructions.ShouldNotBeEmpty(); foreach ... ShouldBeTrue("...")` | gelijk | alleen melding: FA verzamelt alle fouten, foreach stopt bij de eerste |
| fa.all-satisfy | `PaYamlSerializerTests.cs`: `DataSources.ShouldNotBeEmpty(); foreach (var ds ..) ds.Value.ConnectorId.ShouldBeNull();` | gelijk | |
| fa.all-satisfy | `MsappPackingServiceTests.cs`: binnen niet-herschreven `.And.AllSatisfy(i => File.Exists(..).ShouldBeTrue(..))` | gelijk | probe: Shouldly-fout in FA-lambda laat FA falen |
| fa.all-satisfy | `PaYamlSerializerTests.cs`: `.AllSatisfy(ds => ds.Type.ShouldBe(DataSourceType.Table))` (alleen lambda) | gelijk | idem |
| fa.be-equivalent-to-object | `YamlSerializerTests.cs`: `simpleObjectOut.ShouldBeEquivalentTo(simpleObjectIn)` | gelijk in casus | `SimpleObject` heeft alleen string-members, beide statisch én runtime `SimpleObject`. Patroon in het algemeen: zie TAAK-049 |
| fa.throw / fa.throw-async | 0 Fixed in casus (33 Manual) | **afwijkend** (patroon) | probe: regeleinden (TAAK-047), AggregateException (TAAK-048) |
| fa.not-throw | 0 Fixed in casus (3 Manual) | n.v.t. | |

Van de 3 Fixed `fa.be-equivalent-to-object` staat er 1 in de diff-steekproef (de andere twee zijn x3 per TFM van dezelfde regel).

## Bevindingen

1. **Alle in de casus toegepaste herschrijvingen zijn semantisch gelijk** (pass/fail-gedrag); verschillen zitten alleen in foutmeldingen (bv. AllSatisfy verzamelt alle fouten, foreach stopt bij de eerste; NRE i.p.v. assertie-fout bij `.Count` op null).
2. Afwijkingen op patroonniveau, niet geraakt door de casus:
   - TAAK-047 — `WithMessage` negeert regeleinden, de Shouldly-vorm niet → onterecht rood.
   - TAAK-048 — FA `Throw<T>`/`ThrowAsync<T>` pakt `AggregateException` uit, Shouldly niet → onterecht rood.
   - TAAK-049 — `ShouldBeEquivalentTo` eist gelijk runtime-type, FA niet → onterecht rood.
   Alle drie zijn "strenger": een groene FA-test kan rood worden, nooit andersom. Geen gevallen gevonden waar Shouldly ten onrechte groen is.

## Zie ook

- `claude/tasks/TAAK-019-lancering.md` — casus
- `claude/tasks/archief/TAAK-047-fa-withmessage-line-endings.md`
- `claude/tasks/TAAK-048-fa-throw-aggregate-exception.md`
- `claude/tasks/archief/TAAK-049-fa-equivalent-runtime-type.md`
