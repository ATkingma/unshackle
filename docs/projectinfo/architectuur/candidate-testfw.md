# Candidate: test-framework upgrades (xUnit v2 → v3, NUnit 3 → 4)

Scout report for TAAK-440 (2026-10-22). Question: does a version-upgrade fix fit Unshackle although there is no license or EOL
problem? Light scout: primary sources, shallow clones, grep counts, no builds. Numbers are indicative.

## Verdict

| Framework | Verdict | Effort | Why |
|---|---|---|---|
| xUnit v2 → v3 | **Conditional go, low priority** (after Moq/Polly) | M | No tool exists; the C# part is small and mechanical, the project-file part (Exe, package renames) is where the pain is |
| NUnit 3 → 4 | **No-go** | S (if forced) | The official migration path already is a Roslyn code fix (NUnit.Analyzers) plus a one-line `global using` alias; our value-add is near zero |

## Primary sources

- xUnit v3 migration guide: <https://xunit.net/docs/getting-started/v3/migration> and the extensibility guide
  <https://xunit.net/docs/getting-started/v3/migration-extensibility>. Both describe no automated tooling; `xunit.analyzers` is
  unchanged (rule `xUnit1049` flags `async void`, <https://xunit.net/xunit.analyzers/rules/xUnit1049>, v3 projects only).
  The only code-fixing tool found is TUnit's xUnit→TUnit converter (a different target, <https://tunit.dev/docs/migration/xunit/>).
- NUnit 4 migration guide: <https://docs.nunit.org/articles/nunit/release-notes/Nunit4.0-MigrationGuide.html>. NUnit 4.0 is
  "neither binary nor source code compatible" with 3.14. Two breaking changes: classic asserts (`ClassicAssert`,
  `CollectionAssert`, `StringAssert`, `DirectoryAssert`, `FileAssert`) moved to `NUnit.Framework.Legacy`; `Assert.That`
  overloads with format spec/`params` removed.

## What is mechanical, what is Manual

### xUnit v2 → v3

| Change | Class | Note |
|---|---|---|
| Package renames (`xunit` → `xunit.v3`, `xunit.assert` → `xunit.v3.assert`, ...), drop `xunit.abstractions`/`xunit.console`, `xunit.runner.visualstudio` ≥ 3.0 | Fixed | csproj/props edit (we already do package swaps); central package management makes it harder |
| `<OutputType>Exe</OutputType>` in test projects | Fixed | needs "is this a test project" detection (has an `xunit` reference) |
| `using Xunit.Abstractions;` (`ITestOutputHelper`) → `Xunit` | Fixed | types moved to `Xunit`; the guide does not name `ITestOutputHelper` explicitly, verify in a proof |
| `async void` tests → `Task` | Fixed | v3 fast-fails at runtime; trivial rewrite |
| `IAsyncLifetime`: `Task InitializeAsync/DisposeAsync` → `ValueTask` | Fixed, with care | only the interface implementation; bodies such as `return Task.CompletedTask` need `default`/`ValueTask.CompletedTask` |
| Class implementing both `IAsyncDisposable` and `IDisposable`: now only `DisposeAsync` runs | Manual | behavioural, must be reported |
| `Record.ExceptionAsync` now `ValueTask` | Fixed (usually `await` stays) | rare |
| `[PropertyData]` → `[MemberData]`, string-based `[CollectionBehavior]`/`[TestCaseOrderer]` → `typeof(...)` | Fixed | rare |
| Ambiguous new `Assert` overloads (v3 assert is a superset of 2.9) | Manual | only visible at compile time |
| Extensibility (custom `DataAttribute`, discoverers, orderers, reflection abstractions removed, reporters) | Manual | whole sub-API redesigned; report only |

Proof idea (fits the existing model): `before` compiles against xunit 2.9.3, `after` against the xunit.v3 library packages; the
user's own tests are the behaviour proof after a fix, like FluentAssertions. Caveat: xunit.v3 test projects are exes, so proofs
must only compile, not run.

### NUnit 3 → 4

| Change | Class | Existing tool |
|---|---|---|
| `Assert.AreEqual(...)` etc. → `ClassicAssert.AreEqual(...)` + `using NUnit.Framework.Legacy;` | Fixed, trivial | The guide itself suggests a `global using` alias to `ClassicAssert`; IDE0002 converts back |
| Classic → constraint model (`Assert.That(a, Is.EqualTo(b))`) | Fixed, harder | **NUnit.Analyzers code fixes** (rules NUnit2001..NUnit2050 per the guide), batch fix supported, `CollectionAssert` and `StringAssert` covered, `FileAssert`/`DirectoryAssert` not |
| `Assert.That(x, msg, args)` format spec / `params` | Fixed | analyzer fix "Replace Format Specification with Formattable String" (≥ 3.10.0) |
| Extension libraries not NUnit 4 compliant | Manual | `MissingMethodException` at runtime |

Caveat from the guide: analyzers only run when the code compiles, so the fix must run before the `nunit` bump. That ordering is
the one thing a CLI could add, but a `global using` line plus the analyzer batch fix does the same in two steps.

## Corpus survey (29 clones, shallow, pinned)

Clones in `<corpus>\corpus-candidates\testfw\src\<name>\`; list `<corpus>\corpus-candidates\testfw\repos.txt`; scripts
`measure.sh`, `m3.sh` next to it. Counts are lines matching a regex over `*.cs` (comments not stripped). Versions read from
`Directory.Packages.props`/csproj; shas are the shallow HEAD of 2026-10-22.

### Demand signal: active repos already moved

At HEAD, **xUnit v3 already**: akka.net (3.2.2), StackExchange.Redis (3.2.2), refit (4.0.2). **NUnit 4/5 already**: AutoFixture
(4.x range), fluent-nhibernate 4.4.0, markdig 4.6.1, NSubstitute 4.6.1, npgsql 4.6.1, ILSpy/selenium/GitVersion/refit 5.0.0.
Active projects migrate on their own; the ones still on the old major are mostly stable or slow-moving.

### xUnit v2 repos (still on 2.x)

| Repo | sha | xUnit | `[Fact]/[Theory]` | `Xunit.Abstractions` | `async void` | `MemberData` | `TheoryData<` | `IAsyncLifetime` |
|---|---|---|---|---|---|---|---|---|
| Bogus | 6ece18c | 2.6.3 | 590 | 32 | 0 | 5 | 0 | 0 |
| Dapper | eb47546 | 2.9.3 | 421 | 5 | 3 | 1 | 0 | 0 |
| FluentValidation | 299ef37 | 2.2.0 (props) | 839 | 1 | 2 | 2 | 1 | 0 |
| NLog | a38a6d3 | 2.9.3 | 1842 | 2 | 1 | 9 | 0 | 0 |
| RestSharp | 466e5f8 | 2.9.2 | 472 | 0 | 0 | 1 | 0 | 0 |
| serilog | 81af75f | 2.9.2 | 437 | 0 | 0 | 7 | 0 | 0 |
| Open-XML-SDK | 431ab05 | 2.x (not resolved) | 1000 | 0 | 0 | 96 | 0 | 0 |
| html-agility-pack | 248dea3 | 2.4.0 | 36 | 0 | 0 | 0 | 0 | 0 |
| Newtonsoft.Json | 52fa3ae | 2.3.1 (props) | 50 | 2 | 0 | 3 | 0 | 0 |

Reading: the C# changes v3 forces are tiny: 44 `Xunit.Abstractions` usings, 6 `async void`, zero `IAsyncLifetime`, zero
`PropertyData` in nine repos. The real work is project-level: package renames, `OutputType Exe`, runner package. Survey gap:
the set is library-heavy; integration suites use `IAsyncLifetime` far more.

### NUnit 3 repos (still on 3.x or unresolved)

`classic` = `Assert.AreEqual/IsTrue/IsNull/...`.

| Repo | sha | NUnit | `[Test*]` | classic | `Assert.That` | `CollectionAssert` | `StringAssert` | File/DirAssert |
|---|---|---|---|---|---|---|---|---|
| castleproject/Core | 9631074 | 3.14.0 | 1807 | 2592 | 1 | 71 | 62 | 0 |
| nhibernate-core | eea3e81 | 3.14.0 | 11248 | 9237 | 18126 | 81 | 0 | 0 |
| lucenenet | 9f23040 | var (3.14 / 4.2) | 8137 | 9666 | 7 | 16 | 0 | 0 |
| AngleSharp | d4cef0c | central, unresolved | 4171 | 41635 | 77 | 65 | 4 | 0 |
| cecil | 882ca5e | central, unresolved | 446 | 1659 | 10 | 11 | 0 | 0 |
| SharpZipLib | ff2d7c3 | unresolved | 370 | 681 | 172 | 1 | 0 | 2 |
| Newtonsoft.Json | 52fa3ae | 3.11.0 | 3331 | 12242 | 0 | 99 | 632 | 0 |
| log4net | 92cedbf | var | 531 | 0 | 1213 | 0 | 0 | 0 |
| html-agility-pack | 248dea3 | mixed | 92 | 189 | 0 | 0 | 0 | 0 |

Reading: classic asserts dominate (tens of thousands of sites, all mechanically renameable); `FileAssert`/`DirectoryAssert`
(no analyzer fix) are nearly absent (2). Everything the guide lists is covered by the analyzer code fix or the alias, so a
Unshackle pattern would only reproduce an existing batch fix.

## Demand

- NUnit: the forced migration exists (3.14 is the last 3.x) but the path is documented and tooled; repos that move already moved
  (8 of ~17 NUnit repos are on 4/5).
- xUnit: v2 is optional for now and many suites will stay on 2.9.x for years. No tooling exists, so Unshackle would be the only
  automated path, but the C# diff is small; the value is mostly project files.
- Neither is a license problem; the mission statement is license/sponsor/EOL.

## Effort

- xUnit v3: **M**. Skeleton + about 5 patterns (usings, async void, IAsyncLifetime, package/OutputType swap, attribute type
  args), Manual reports for extensibility. New: csproj-level edits keyed on "test project", central package versions.
- NUnit 4: **S** for rename + using, but redundant; the constraint-model rewrite is **L** and already done by NUnit.Analyzers.

## Recommendation

1. NUnit 3 → 4: no-go. Point to the NUnit.Analyzers batch fix before bumping.
2. xUnit v2 → v3: conditional go as a later, small fix (after Moq, Polly), only if the council accepts version-upgrade fixes.

## Draft task (taak-start format)

```
# TAAK-441 — xUnit v2 → v3 fix (`xunit3`)

**Status**: not started (blocked on council: are version upgrades in scope?)
**Created**: 2026-10-22
**Goal**: An `xunit3` fix that migrates xUnit v2 test projects to xunit.v3 (package renames, OutputType Exe, Xunit.Abstractions
usings, async void, IAsyncLifetime ValueTask, PropertyData, typeof attribute args), Manual for extensibility and
dual-disposal classes; 100% on a corpus where every project builds after the fix.

## Steps
- [ ] 1. Council: scope and Fixed/Manual line (see candidate-testfw.md)
- [ ] 2. Corpus: 10-15 xUnit 2.x repos incl. integration suites with IAsyncLifetime (survey gap), pinned shas, builds
- [ ] 3. Skeleton Fixes/XUnit3/ with csproj/props edit + proofs (before vs xunit 2.9.3, after vs xunit.v3 packages)
- [ ] 4. Patterns, one task each (usings, async void, IAsyncLifetime, PropertyData, typeof attrs)
- [ ] 5. Corpus round, blind round, roadmap row
```

## Council questions

1. Are version-upgrade fixes (no license/EOL trigger) in Unshackle's scope?
2. If yes: is a csproj-level fix (OutputType, package renames, central versions) acceptable as a first in the engine?
3. NUnit: confirm no-go, or ship only a thin wrapper (analyzer batch fix, then package bump) with no pattern of our own?
4. Is the library-heavy corpus bias a problem? Add integration-test-heavy repos (IAsyncLifetime) before deciding?

## See also

- `docs/projectinfo/architectuur/corpus-automapper.md` — the model of a survey
- `docs/projectinfo/project.md` — roadmap and existing-tool table
- `claude/tasks/TAAK-440-scout-testfw.md` — the scout task
- `claude/tasks/TAAK-307-moq-to-nsubstitute.md` — the next planned fix
