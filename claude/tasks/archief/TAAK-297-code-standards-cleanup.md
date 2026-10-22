# TAAK-297 — Code standards cleanup

**Status**: done (2026-10-21) — the split of `CreateMapPattern.cs`/`Fixer.cs` (step 5 proposal) is a follow-up task  
**Created**: 2026-10-20  
**Goal**: all non-vendored code in `src/` follows `claude/docs/code-standaarden.md`; behaviour and rendered output stay exactly the same.

## Context for the next agent

- Audit done on 2026-10-20 (main `91d1216`, release 0.2.0). Already clean: no `sealed`, no Moq, no primary constructors, no `ConfigureAwait`, no assembly scanning.
- The `CancellationToken cancellationToken = default` strings in `MediatorImplementationPattern.cs` are generated user code: leave them.
- `Unshackle.Core/Fixes/MediatR/Vendored/` is copied package source: out of scope.
- Pure refactor: no test may change its expectations. If one has to, stop and take it to the council (`claude/docs/council.md`).
- Push procedure: `claude/docs/overdracht.md` "Push procedure" (`verify-head.sh ... && git push`).

## Steps

- [x] 1. §15: add `// Arrange`, `// Act`, `// Assert` to `Tests/Core/MediatRFixPackageTests.cs`, `Tests/Core/ShouldBeEqualsOrderTests.cs`, `Tests/Helpers/TestSolutionTests.cs`; grep for other tests that lack them (also in files that have some).
- [x] 2. §13: move `MapperCleanup.Removal` and `MapperCleanup.Entry` (`Core/Fixes/AutoMapper/Patterns/MapperCleanup.cs:490,503`) to their own files; grep for other nested types in non-vendored code and do the same.
- [x] 3. §1: `RequestProcessorCallerPattern.CallAnalysis` (private record, line 166) → class with `required init`, own file. `ProjectFile.Span` (`readonly record struct`) stays unless the council says otherwise (private value type, not a model).
- [x] 4. Manual pass over the rules that `.editorconfig` cannot enforce: §5 `var` only with a visible type, §7 `ThrowIfNull` only at public entry points, §9 in `IEnumerable<T>`/out `IReadOnlyList<T>`, §16 comments explain the why.
- [x] 5. Split candidates: `CreateMapPattern.cs` (2040 lines) and `Fixer.cs` (966 lines). Make a proposal (which helper classes, along which seams); executing it is a follow-up task, not this one.
- [x] 6. Build, tests, sample tests green in a worktree check → commit → push → CI green.

## Result step 1 (2026-10-21)

The three named files already had `// Act`/`// Assert`; what was missing everywhere was `// Arrange`. Nine tests in six files
(`MediatRFixPackageTests`, `ShouldBeEqualsOrderTests`, `SwappedTypesTests` ×2, `TestSolutionTests`, `CliTests`, `LineDiffTests`) now
put their input in an Arrange block; no expectation changed. All other test files have Arrange/Act/Assert in every test.

## Result steps 2–3 (2026-10-21)

Twelve nested types moved to their own files as top-level `internal` types: `MapCallKind`, `MapOwnerEntry`, `MapNode`,
`MapperCleanupRemoval`, `MapperCleanupEntry`, `MapperRegistrationScope`, `MapperRegistrationFacts`, `TypePairComparer`,
`GenericDecoratorKind`, `NotificationClosedTypes`, `GenericRequestUses`, and `ProcessorCallAnalysis` (was the private record
`CallAnalysis`, now a class with `required init`). `ProjectFile.Span` stays. No test changed; 1648 tests green.

## Result step 4 (2026-10-21)

- §7: 58 `ThrowIfNull` calls removed from patterns and helpers (34 AutoMapper, 24 elsewhere). They stay in the Fixer, commands,
  `Cli`, `SpectreConsoleView`, the stores, report writers, `ProcessRunner`, and the public members of the fix classes
  (`FluentAssertionsFix` constructor, `MediatRFix.PlanPartialAsync`): the fixes are built at the composition root. If the council
  sees fixes as helpers, move those too.
- §9: `CreateMapPattern.ConfigurationLambdas` and `MediatRSyntax.ReturnedExpressions` return `IReadOnlyList<T>`;
  `MediatRSyntax.CommaList<T>` takes `IReadOnlyList<T>` (needs `Count`). Skipped on purpose: lists that callers fill or store in
  typed tuples/dictionaries (`CapturedLocal.Used`, `MapOwners.ClassesUnderTest`/`MockedClasses`, `Projections.Problem` out
  parameter, `MapperRegistrationFacts.Containers`, the `namespaces` sets) and ~20 private helpers that use `Count`/indexing.
- §5: no change; IDE0007 already enforces the visible-type rule and rejects explicit types where Roslyn sees the type.
- §16: no comment removed; the inline comments give reasons or TAAK references.

## Proposal step 5: splitting the two large files

Follow-up task, not executed here. Seams follow the existing method groups; each new class is `internal static` in the same folder,
the pattern/fixer keeps its public surface, so tests and output do not change.

**`CreateMapPattern.cs` (2040 lines) → five files**

| New class | Lines now | Contents |
| --- | --- | --- |
| `CreateMapPattern` (stays) | 1–530 | `Find`, `Rewrite`, `RewriteAsync`, `RewriteMethodsAsync`, `RewriteProjectionAsync`, the main `Rewrite` |
| `CreateMapProblems` | 587–1590 | every `*Problem` method, `ConstructorChoice`, `Reverse`, `MemberPath`, `NullSafeChain`, `MayBeNull`, `KeepsDefault`, `Grouped`: why a map stays Manual |
| `ProfileSyntax` | 1590–1950 | syntax queries on profiles: `IsProfile`, `IsProfileWithMaps`, `ConfigurationLambdas`, `LambdaStatements`, `MapStatements`, `CreateMapName`, `TryParseMap`, `TryParseOptions`, `ReverseMapCall`, `HasReverseMap`, `UsesForMember` (not merged into `ConfigurationSyntax`, which serves `ConfigurationTestPattern`) |
| `MapAccessibility` | 1553–1570, 1915–1934 | `IsReference`, `IsAvailableOutsideProfile`, `Effective`, `Weakest` |
| `MapMethodWriter` | 536–586, 1934–2040 | `ExtensionsName`, `MethodName`, `Extensions`, `Emptied`, `Trimmed`, `AtAnchor`: writing the hand-written map methods |

The `protected` helpers (`Anchor`, `UsesForMember`, `IsProfileWithMaps`) are used by subclasses; they become `internal` on the new
class, and the subclasses call them there.

**`Fixer.cs` (966 lines) → four files**

| New class | Lines now | Contents |
| --- | --- | --- |
| `Fixer` (stays) | 1–87, 196–425 | constructors, `FixAsync`, `RunAsync`, the pass loop, `PassCopiesAsync`, `NewCode` |
| `PartialRun` | 88–196 | `RunPartialAsync`, `KeepsKeptCodeAsync`, `InOriginalAsync`, `IsKeptPackageRow` |
| `FileFixer` | 425–605, 806–958 | `FixFileAsync`, `FixWithTargetedImportsAsync`, `AddReferences`, `ApplyPatternAsync`, `BlockedCall`, `RewriteAsync`, the trivia/directive helpers: everything for one file against the fixed baseline |
| `SolutionChecks` | 695–799 | `SwapGlobalUsingsAsync`, `UnreachableFilesAsync`, `NotUtf8FilesAsync` |
| `FindingFactory` | 605–621, 799–806, 958–966 | the `CreateFinding`/`NotApplied`/`InternalError` overloads |

`FileFixer` is also the seam TAAK-298 needs: one file in, one `FilePass` out, so parallel processing only has to merge results.
Do the `Fixer` split before TAAK-298 step 5.

## Changes

| Field     | Content                                                                 |
| --------- | ----------------------------------------------------------------------- |
| **Where** | `src/Unshackle.Tests` (3+ test files), `Unshackle.Core/Fixes/AutoMapper/Patterns/MapperCleanup.cs`, `Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorCallerPattern.cs` |
| **How**   | Refactor without behaviour change: comments in tests, types into their own files, record → class |
| **Why**   | Technical debt: the code standards (2026-10-10) are not followed everywhere |

## See also

- `claude/docs/code-standaarden.md` — the rules
- `claude/tasks/archief/TAAK-298-performance.md` — performance follow-up from the same audit
- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/README.md` — architecture overview
