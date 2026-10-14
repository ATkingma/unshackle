# TAAK-182 — MediatR: inactive `#if` code without MediatR keeps `global using MediatR` alive

**Status**: ✅ Done — 2026-10-12 (step 3 partly: harness not run on jason, corpus project not available)
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`; action 10 from the run of 2026-10-11)
**Goal**: jasontaylor CleanArchitecture v8.0.0 migrates fully.

## Context for the next agent

- jason (`f2f0445721f5e3efe98ea22266148c698b3ef288`, tag v8.0.0, MediatR 12.1.1): `src/Web/GlobalUsings.cs:3 global using MediatR;`. `Web` has `#if (UseApiOnly)` blocks (dotnet-new template) in `Program.cs`, `DependencyInjection.cs`, `Endpoints/Users.cs`; none of them mentions a MediatR identifier.
- As a result `UsingDirectives.cs` gives Manual "still uses MediatR (or has inactive #if code)" and `project-file` "still used" → all-or-nothing, 0 of 29 rewritten.

## Changed files

- `src/Unshackle.Core/UsingDirectives.cs`, `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Core/ProjectPackages.cs`
- `src/Unshackle.Tests/Core/FixerTests.cs`, `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/edge-inactive-if.{before,after}.cs`

## Analysis

Inactive branches only exist as `DisabledTextTrivia`. Tokenize that text (`SyntaxFactory.ParseTokens`) and only count it as a use if an identifier from `MediatRFix.SwappedTypes` or a MediatR-specific type (`IRequestHandler`, `IPipelineBehavior`, `AddMediatR`, ...) occurs. Otherwise the `global using` may be swapped/removed.

## Steps

1. [x] Unit test: `#if X` with and without `ISender` in the inactive branch (`FixerTests`: with `StringBuilder` existing, without new) + proof `MediatR/RequestHandler/edge-inactive-if`.
2. [x] Implementation in `UsingDirectives.cs`: `InactiveCodeMayUse` replaces `HasInactiveCode`; tokenizes `DisabledTextTrivia` and looks for names of public types, extension methods and the first namespace segments of the removed namespaces (from the compilation, so generic for all fixes). `KeepsRemovedUsing` got a `Compilation` parameter.
3. [x] Proof: corpus run 2026-10-12 on 5f9b366 (`CORPUSRUN-mediatr.md`, section Repetition after TAAK-180..184): jason 27 Fixed, 0 Manual, MediatR gone — but the build after the fix fails (CS0305 in `RequestLoggerTests`: the test constructs `LoggingBehaviour<TRequest>`, which now has two type parameters). Follow-up: TAAK-190.

## See also

- `src/Unshackle.Core/UsingDirectives.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
