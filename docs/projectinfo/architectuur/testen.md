# Testing

## Proofs: testing every pattern without new test code

```
src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/
   simple.before.cs      simple.after.cs
   dictionary.before.cs  dictionary.manual.txt    ← expected: manual work
```

One test (`ProofTests`) discovers all proofs itself and runs the Fixer on them. Testing a new pattern or
unusual case means adding two files. The `Proofs/` folder is not
compiled: it is test data.

**Safety check in every proof:** the compilation of `before` must be error-free against the old
library, and that of `after` against the new one. This catches a forgotten reference (otherwise
the `SemanticModel` silently returns error types) and a rewrite that does not compile.

## Combined proofs: patterns together

```
src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/<case>.before.cs + <case>.after.cs
```

`ProofTests` runs one pattern per proof; that leaves cooperation unproven (e.g. `fa.all-satisfy` copies its
lambda body into a `foreach` and the later patterns rewrite that body; `.And` splitting next to other patterns;
usings). `CombinedProofTests` runs the **real** `FluentAssertionsFix` (all patterns, real order) and compares
the final text exactly with `.after.cs`. Same compile check: before against FluentAssertions, after against Shouldly (if
the after still contains `using FluentAssertions;` due to a Manual leftover, then Shouldly + FluentAssertions). `ProofTests` skips
the `_Combined` folder. A separate test pins down that `AllSatisfyPattern` comes first. New case = two files.

## Pending proofs: known bugs

`src/Unshackle.Tests/Proofs/_Pending/<Fix>/<Pattern>/<case>.*` contains proofs of known pattern bugs, with the desired
`.after.cs`. `ProofTests` skips the folder; every pending proof belongs to a task. Fix done → move the folder back.
Origin of the edge cases: `architectuur/edge-cases.md`; frequencies and fix runs: `architectuur/corpus.md`.

## Types without building

Unit tests use an in-memory `Solution` (`AdhocWorkspace`). The types come from:

1. **Real assemblies** (default). The test project references the last free versions
   (FluentAssertions 7.2.2, MediatR 12.5.0, AutoMapper 14.0.0, plus Shouldly and Mediator.Abstractions).
   .NET itself comes from `Basic.Reference.Assemblies.Net100`. A helper `TestReferences` provides the right set of
   `MetadataReference`s per fix.
2. **Stubs**: only for exceptions you cannot reproduce with the real package.
3. **Real sample projects** in `samples/` (currently: `samples/SampleShop`, a web shop with FluentAssertions 7.2.2,
   MediatR 12.5.0 and AutoMapper 14.0.0; every FA pattern has a comment with the expected rewrite). Samples
   are separate from our build: their own `Directory.Build.props`, `Directory.Packages.props` and `.editorconfig` (root).
   `Unshackle.Tests/Integration/` loads them with MSBuild, fixes, builds and tests them. Slow, so few of them, and runnable separately via a trait/filter.

## Mocking (NSubstitute)

Only mock your **own** interfaces, never Roslyn types. For Roslyn, use a real
in-memory solution.

| What you test   | How                                                             |
| --------------- | --------------------------------------------------------------- |
| A pattern       | Proofs with real assemblies                                     |
| The Fixer logic | Mocked `IFix` and `IPattern`, small in-memory solution          |
| Commands        | Mocked `IFixer`, `IReportWriter` and `IConsoleView`             |
| Report writers  | `MockFileSystem` (System.IO.Abstractions), no real files        |
| End-to-end      | `samples/` in the integration tests                             |

We deliberately do not use Moq (SponsorLink controversy in 2023, and it is itself a migration candidate).

## See also

- `docs/projectinfo/architectuur/proofs.md` — overview of all proofs (generate: `python3 claude/tools/proofs-doc.py`)
- `docs/projectinfo/README.md` — overview and C4 diagrams
- `src/Unshackle.Tests/ProofTests.cs` — proof runner
- `src/Unshackle.Tests/CombinedProofTests.cs` — runner for cooperation of patterns
