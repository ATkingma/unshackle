# TAAK-046 — Semantic audit of the rewrites (PowerApps-Tooling case)

**Status**: ✅ completed (read-only audit, no code changed)
**Created**: 2026-10-11
**Case**: TAAK-019, `microsoft/PowerApps-Tooling` @ 630e6973, fixed working copy (`git diff`), FA 7.2.0 → Shouldly 4.3.0

## Goal

Not only "builds and tests green", but: does the Shouldly form fail in exactly the same cases as the FA original?

## Method

- Sample per pattern id from the case's `git diff` (≥5 per pattern, all if fewer).
- Behaviour measured, not from memory: scratch console `scratchpad/semprobe` with the real packages FluentAssertions **7.2.2** (case: 7.2.0, same major/minor behaviour) and Shouldly **4.3.0**, every case in try/catch → PASS/FAIL.
- The Throw patterns are all Manual in the case (0 Fixed); they were judged against the pattern code (`ThrowPattern.cs`, `ThrowAsyncPattern.cs`, `ThrowMessage.cs`) and the probe.

## Measured behaviour (probe)

| Case | FA 7.2.2 | Shouldly 4.3.0 |
|---|---|---|
| `Throw<Base>` on `Derived` | PASS | PASS (`Should.Throw<Base>`) |
| `ThrowAsync<Base>` on `Derived` | — | PASS |
| `Throw<Derived>` on `AggregateException(Derived)` | **PASS** (unwraps) | **FAIL** |
| `ThrowAsync<Derived>` on `AggregateException(Derived)` | **PASS** | **FAIL** |
| `WithMessage("hello world")` on "Hello World" | PASS | PASS (`ShouldBe(.., IgnoreCase)`) |
| `WithMessage("*WORLD")` / `("*lo Wo*")` | PASS | PASS (`ShouldEndWith/ShouldContain(.., Case.Insensitive)`) |
| `WithMessage("Hello World")` on "  Hello World\r\n" | FAIL | FAIL (no trim in FA 7) |
| `WithMessage("a\nb")` on "a\r\nb" | **PASS** (line endings ignored) | **FAIL** (`ShouldBe(.., IgnoreCase)`) |
| `WithMessage("*line1\nline2*")` on "Line1\r\nLine2 end" | **PASS** | **FAIL** (`ShouldContain(.., Case.Insensitive)`) |
| same with `IgnoreCase \| IgnoreLineEndings` | — | PASS |
| `WithMessage("value [x]")` | PASS (`[` no wildcard) | n/a |
| string `Contain("hello")` on "Hello" | FAIL (case-sensitive) | `Case.Sensitive`: FAIL; **default: PASS** |
| string `Be("A")` on "a" / "a\r\nb" vs "a\nb" | FAIL / FAIL | FAIL / FAIL |
| `null.Be(null)` | PASS | PASS |
| `(0.1+0.2).Be(0.3)` | FAIL | FAIL (both exact) |
| `DateTime` Utc vs Local, same ticks | PASS | PASS (Kind ignored) |
| `AllSatisfy` on an empty collection | **FAIL** | (the rewrite adds `ShouldNotBeEmpty()`) |
| Shouldly failure inside an FA `AllSatisfy` lambda | FAIL | — (the failure comes through) |
| `BeEmpty` on null (collection/string) | FAIL | FAIL |
| `HaveCount(0)` on null | FAIL | FAIL (NRE via `.Count`) |
| `BeEquivalentTo` collection in another order | PASS | FAIL |
| `BeEquivalentTo` subject of a runtime-derived type (`Pt2 : Pt`) | **PASS** | **FAIL** |

Conclusions:
- `Contain(string)` → `ShouldContain(.., Case.Sensitive)` is **correct and necessary**: Shouldly's default is case-**in**sensitive.
- `WithMessage("x")` → `ShouldBe(.., StringCompareShould.IgnoreCase)` is correct for case and `*`, **not** for line endings (TAAK-047).
- Shouldly `Throw<T>` accepts derived types, just like FA; the Manual reason for `ThrowExactly<T>` is right.
- FA unwraps `AggregateException`, Shouldly does not (TAAK-048).

## Sample per pattern

| Pattern | Example (case) | Verdict | Evidence |
|---|---|---|---|
| fa.should-be | `ErrorTests.cs`: `exception.Message.ShouldBe("Mismatch ... .png")` | equal | string ordinal, both case- and line-ending-sensitive |
| fa.should-be | `MsappSerializationTests.cs`: `deserialized.LastPackedDateTimeUtc.ShouldBe(utcNow)` | equal | DateTime: both `Equals`, Kind ignored |
| fa.should-be | `PaYamlSerializerTests.cs`: `...App!.Start.ShouldBe(new(1, 6))` | equal | struct `Equals` |
| fa.should-be | `PaArchivePathTests.cs`: `PaArchivePath.IsValidSegment(segment).ShouldBe(expectIsValid, "because ...")` | equal | because → customMessage, message only |
| fa.should-be | `MsaprSerializationTests.cs`: `MsaprStructureVersion.ShouldBe(new Version(0, 1))` | equal | `Version.Equals` |
| fa.should-be | `PaArchivePathTests.cs`: `validSegment.ShouldBe(expectedValidSegment)` (sometimes null) | equal | null==null PASS in both |
| fa.be-true-false | `ErrorTests.cs`: `errors.HasErrors.ShouldBeTrue()` | equal | trivial |
| fa.be-true-false | `PaArchiveTests.cs`: `ContainsEntry("ENTRYa").ShouldBeTrue()` | equal | |
| fa.be-true-false | `MsappPackingServiceTests.cs`: `Directory.Exists(..).ShouldBeTrue("service should ...")` | equal | because → customMessage |
| fa.be-true-false | `TestBase.cs`: `JsonNode.DeepEquals(..).ShouldBeTrue($"...")` | equal | |
| fa.be-true-false | `PaArchivePathTests.cs`: `archivePath.IsDirectory.ShouldBeFalse()` | equal | |
| fa.be-null | `ErrorTests.cs`: `doc.ShouldBeNull()` | equal | |
| fa.be-null | `PaArchiveExtensionsExtractTests.cs`: `validFullPath.ShouldNotBeNull()` | equal | |
| fa.be-null | `PaArchiveTests.cs`: `paArchive.CreateEntry(entry).ShouldNotBeNull()` | equal | |
| fa.be-null | `MsappArchiveTests.cs`: `header.LastSavedDateTimeUTC.ShouldBeNull()` | equal | `Nullable<T>` |
| fa.be-null | `PaYamlSerializerTests.cs`: `.Which.ShouldNotBeNull()` | equal | |
| fa.have-count | `PaArchiveTests.cs`: `paArchive.Entries.Count.ShouldBe(12, "...")` | equal | null: FA FAIL, `.Count` NRE → both fail |
| fa.have-count | `PaArchiveTests.cs`: `GetEntriesInDirectory(..).Count().ShouldBe(..)` | equal | LINQ `Count()` |
| fa.have-count | `PaYamlSerializerTests.cs`: `screen.Properties.ShouldNotBeNull().Count.ShouldBe(..)` | equal | TAAK-021 null chain |
| fa.have-count | `NamedObjectMappingTests.cs`: `namedObjectMapping.Count.ShouldBe(3)` | equal | |
| fa.have-count | `PaYamlSerializerTests.cs`: `screen.DescendantControlInstances().Count().ShouldBe(..)` | equal | enumerated once, like FA |
| fa.contain | `MsappSerializationTests.cs`: `json.ShouldContain("LoadFromYaml", Shouldly.Case.Sensitive, "...")` | equal | probe: FA Contain = Case.Sensitive |
| fa.contain | `PaArchiveTests.cs`: `.Which.StackTrace.ShouldNotBeNull().ShouldContain("Path.CheckInvalidPathChars", Case.Sensitive)` | equal | null: both fail |
| fa.contain | `PaArchiveTests.cs`: `exThrown.StackTrace.ShouldNotBeNull().ShouldContain("PaArchive.EnsureEntriesInitialized", Case.Sensitive)` | equal | |
| fa.contain | `PaArchivePathTests.cs`: `invalidChars.ShouldContain('\b', "BS char ...")` (collection) | equal | element `Equals` |
| fa.contain | `PaArchivePathTests.cs`: `allSeparatorChars.ShouldContain(Path.DirectorySeparatorChar, "...")` | equal | |
| fa.be-empty | `PaArchivePathTests.cs`: `archivePath.FullName.ShouldBeEmpty()` (string) | equal | null: both FAIL |
| fa.be-empty | `PaArchivePathTests.cs`: `invalidChars.ShouldNotBeEmpty()` | equal | |
| fa.be-empty | `PaArchiveTests.cs`: `paArchive.Entries.ShouldBeEmpty()` | equal | |
| fa.be-empty | `PaArchiveTests.cs`: `capturingLogger.EntriesByName(..).ShouldBeEmpty()` | equal | |
| fa.be-empty | `MsappPackingServiceTests.cs`: `msapp.Entries.ShouldNotBeEmpty("...")` | equal | |
| fa.all-satisfy | `PaArchiveTests.cs`: `ShouldNotBeEmpty(); foreach (var e in ..) e.Level.ShouldBe(LogLevel.Warning);` | equal | probe: FA AllSatisfy fails on empty |
| fa.all-satisfy | `MsappPackingServiceTests.cs`: `instructions.ShouldNotBeEmpty(); foreach ... ShouldBeTrue("...")` | equal | message only: FA collects all failures, foreach stops at the first |
| fa.all-satisfy | `PaYamlSerializerTests.cs`: `DataSources.ShouldNotBeEmpty(); foreach (var ds ..) ds.Value.ConnectorId.ShouldBeNull();` | equal | |
| fa.all-satisfy | `MsappPackingServiceTests.cs`: inside a non-rewritten `.And.AllSatisfy(i => File.Exists(..).ShouldBeTrue(..))` | equal | probe: a Shouldly failure in an FA lambda makes FA fail |
| fa.all-satisfy | `PaYamlSerializerTests.cs`: `.AllSatisfy(ds => ds.Type.ShouldBe(DataSourceType.Table))` (lambda only) | equal | same |
| fa.be-equivalent-to-object | `YamlSerializerTests.cs`: `simpleObjectOut.ShouldBeEquivalentTo(simpleObjectIn)` | equal in the case | `SimpleObject` has only string members, both statically and at runtime `SimpleObject`. Pattern in general: see TAAK-049 |
| fa.throw / fa.throw-async | 0 Fixed in the case (33 Manual) | **deviating** (pattern) | probe: line endings (TAAK-047), AggregateException (TAAK-048) |
| fa.not-throw | 0 Fixed in the case (3 Manual) | n/a | |

Of the 3 Fixed `fa.be-equivalent-to-object`, 1 is in the diff sample (the other two are x3 per TFM of the same line).

## Findings

1. **All rewrites applied in the case are semantically equal** (pass/fail behaviour); differences are only in error messages (e.g. AllSatisfy collects all failures, foreach stops at the first; NRE instead of an assertion failure for `.Count` on null).
2. Deviations at pattern level, not hit by the case:
   - TAAK-047 — `WithMessage` ignores line endings, the Shouldly form does not → wrongly red.
   - TAAK-048 — FA `Throw<T>`/`ThrowAsync<T>` unwraps `AggregateException`, Shouldly does not → wrongly red.
   - TAAK-049 — `ShouldBeEquivalentTo` requires an equal runtime type, FA does not → wrongly red.
   All three are "stricter": a green FA test can turn red, never the other way round. No cases found where Shouldly is wrongly green.

## See also

- `claude/tasks/TAAK-019-lancering.md` — case
- `claude/tasks/archief/TAAK-047-fa-withmessage-line-endings.md`
- `claude/tasks/archief/TAAK-048-fa-throw-aggregate-exception.md`
- `claude/tasks/archief/TAAK-049-fa-equivalent-runtime-type.md`
