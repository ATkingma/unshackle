# Verification: fa-should-be

|               |                                                                          |
| ------------- | ------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/fa-should-be-plan.md`](fa-should-be-plan.md) |
| **Status**    | Matches plan                                                             |
| **Generated** | 2026-10-10T23:35+02:00 by KingmaDoc skill 1.1.0                          |

> Verified by an AI agent against the code at uncommitted changes (basis b1ab588).
> Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs`; registration in `FluentAssertionsFix.cs` (`Patterns`-lijst). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/`, run by `ProofTests`.

| REQ    | Verdict                     | Evidence                                                                                                               |
| ------ | --------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| REQ-1  | Met                         | `simple.before.cs` / `simple.after.cs` (int, string)                                                                 |
| REQ-2  | Met                         | `because.before.cs` / `because.after.cs`; `because` comes along in the original argument list                         |
| REQ-3  | Met                         | `lambda.before.cs` / `lambda.after.cs` (`IsResultDiscarded` accepteert void-lambda)                                  |
| REQ-4  | Met                         | `constant-cast.before.cs` / `constant-cast.after.cs`: `(byte)1`, `(Status)0`, `long` without a cast (`ConvertExpected`) |
| REQ-5  | Met                         | `chain.before.cs` / `chain.manual.txt` (reason "is used")                                                            |
| REQ-6  | Replaced                    | by Extension TAAK-097                                                                                            |
| REQ-7  | Met                         | `comparer.before.cs` / `comparer.manual.txt` (`values.Count != 1`)                                                   |
| REQ-8  | Met                         | `nullable-expected.before.cs` / `nullable-expected.manual.txt` (`ConvertExpected` returns null)                      |
| REQ-9  | Met                         | `xml.before.cs` / `xml.manual.txt` (namespace `FluentAssertions.Xml`/`.Types`)                                       |
| REQ-10 | Met in code, no proof | `subject is null` (`FluentAssertionPattern.Rewrite`) and `IErrorTypeSymbol` (`RewriteAssertion`) give Manual; no proof file |
| TAAK-097 | Met | `because-args.after.cs` (`string.Format`); Manual: `edge-named-because`, `because-args-variable`, `because-args-bad-format` |
| TAAK-100 | Met | `assertion-scope-nested`, `assertion-scope-using-block`, `assertion-scope-using-var` (`.manual.txt`) |
| TAAK-070 | Met | `edge-assertion-in-throw-lambda` (`.manual.txt`): an assertion in a lambda that is checked with `Throw<T>` where T ≠ `Exception`/`ShouldAssertException`; shared check `IsInsideExpectedFailure` in all FA patterns, unit test `IsInsideExpectedFailure_DependsOnExpectedExceptionType` |
| TAAK-169 | Met | `implicit-conversion.before.cs` / `implicit-conversion.after.cs`: `ShouldBe((Doc)42)` for an own type with `implicit operator` (int and string); `ConvertExpected` recognises `Conversion.IsUserDefined`. Behaviour measured with a probe, FA and Shouldly equal. |
| TAAK-199 | Met | `own-shouldbe-in-namespace.before.cs` / `.manual.txt`: an own `ShouldBe<T>` in the enclosing namespace; unit tests `AmbiguousAddedNamespaceTests.FixAsync_FileInNamespaceOfOwnShouldBe_*`. Akka: `MixedBindingPair.cs` (namespace `Akka.TestKit`) stays FA + Manual. |
| TAAK-200 | Met (warning) | `EqualsOrderWarning` in `ShouldBePattern` (also `NotBePattern`): Fixed with `Warning` for an own `Equals` or two unknown runtime types; unit tests `ShouldBeEqualsOrderTests` (5 with, 5 without a warning), proof `own-equals.*`. Probe Akka DData: `a.Equals(e)=True`, `e.Equals(a)=False`. |
| TAAK-198 | Met | Fallback per call: `own-shouldbe-in-namespace.manual.txt` stays Manual (the only call is `ShouldBe`); mixed files in `AmbiguousAddedNamespaceTests` (`ShouldBe` stays FA, the rest via `using static`). |

Extension TAAK-277: Met, `ShouldBe/type.*`, `ShouldBe/edge-named-because.*`; probe against FA 7.2.2.

## Deviations

| #   | Area         | Plan says                                              | Code does                                                                                       | Impact |
| --- | ------------ | ------------------------------------------------------ | ----------------------------------------------------------------------------------------------- | ------ |
| 1   | Requirements | REQ-10: unknown type and deviating shape become Manual | The code does this (`ShouldBePattern.cs`, start of `Rewrite`), but no proof covers it  | Low    |
| 2   | Risk         | TAAK-001 names `Guid` against `string` as Manual     | Falls under the same branch as `nullable-expected` (`ConvertExpected` null); only `int?` proven | Low    |
| 3 | Scope | _(edge case)_ | An expression-bodied void method/local function/accessor (`void M() => x.Should().Be(1);`) counts as a discarded result (TAAK-040, `edge-expression-bodied.*`); `a?.B.Should()` likewise (TAAK-041) | Low |

Edge cases outside the pattern itself (in `Fixer`/`UsingDirectives`, see `docs/projectinfo/architectuur/core.md`):
`edge-ifdef` (an inactive `#if` branch stays FA; `using FluentAssertions;` stays next to `using Shouldly;` + Manual `usings`,
TAAK-043) and `edge-global-using` (`global using FluentAssertions;` → `global using Shouldly;`, TAAK-044). The proof runner
compiles `after` against both libraries if there is a Manual finding.

No further deviations: all `files_expected` exist and were touched, no new containers or external systems, scope not exceeded (no `NotBe`, no chain splitting).

## Validation results

| Check | Command        | Result                                                       |
| ----- | -------------- | ------------------------------------------------------------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors                                 |
| Tests | `dotnet test`  | passed: 29 of 29 (whole solution, including all ProofTests)  |
| Lint  | _none_         | _not run_: no lint command configured in `.featuredoc.yml`   |
