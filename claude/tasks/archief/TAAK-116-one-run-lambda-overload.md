# TAAK-116 — One `fix` run is enough for a rewritten lambda argument

**Status**: done (2026-10-11)
**Created**: 2026-10-11 (follow-up to TAAK-110)

## Context

After TAAK-110, CSharpFunctionalExtensions needed two runs. `fa.should-be` rewrites `ctx.Should().Be(context)` in the
lambda argument of `maybe.Bind(...)` to `ctx.ShouldBe(context)`. Shouldly was not in the loaded model, so that did not bind,
the overload resolution of `Bind` failed, `maybe2` became an error type and `fa.be-true-false` did not see `maybe2.HasValue.Should().BeTrue()`:
21 Manuals `usings` ("run the fix again").

## Choice

- (a) chosen: Shouldly as a metadata reference in the working copy. Small, and solves it at the source.
- (b) recognition on the original compilation: patterns deliberately run one after another on the intermediate result (nested findings of
  different patterns, AndChain, AssertionScope); doing everything on the original would break that nesting. The per-file working copy
  of TAAK-130 does not help here: the problem is within a single file.
- (c) a fixpoint without (a) does not help: `ShouldBe` stays unknown.

## Result

- `IFix.AddedReferences` (default empty). `FluentAssertionsFix(IReadOnlyList<MetadataReference> shouldly)`; the parameterless
  constructor stays (no references, old behaviour).
- `Fixer.AddReferences`: per C# project the missing references (by file name) plus document `Unshackle.AddedUsings.g.cs`
  with `global using Shouldly;` in the `baseline` on which the patterns run. The result builds on `original`, so the projects
  get nothing extra. `UsingDirectives.GlobalUsings` ignores that document (otherwise a file would get no `using Shouldly;`).
- Composition root (`Cli`): `References/Shouldly.dll` next to the tool, the **netstandard2.0** build from the package
  (`GeneratePathProperty`, `ExcludeAssets="all"`). The net9.0 build gave CS1705 in a net8.0 project and then resolved nothing.
- Proof: `_Combined/unbound-should.*` → `_Combined/lambda-overload-one-run.*`, now fully Fixed (`ShouldBeTrue`, no FA using).
  `CombinedProofTests` uses `new FluentAssertionsFix(TestReferences.ShouldlyAssembly)` like the CLI.
- Corpus: fresh copy of CSFE from `scratchpad/corpus/tgz/vkhorikov_CSharpFunctionalExtensions.tgz` (`scratchpad/t116/run4`):
  one fix run 4502 Fixed / 49 Manual, **0 `usings`** (was 4481/70 with 21 `usings`), build green.

## See also

- `claude/tasks/archief/TAAK-110-unrecognized-should-keeps-using.md`
- `docs/projectinfo/architectuur/core.md`
