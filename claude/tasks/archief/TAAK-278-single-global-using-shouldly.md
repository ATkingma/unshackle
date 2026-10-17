# TAAK-278 — At most one added global using per project

**Status**: done (2026-10-17)
**Created**: 2026-10-17
**Goal**: the fix writes each added namespace (e.g. `global using Shouldly;`) at most once per project, and not at all when the project already has it.

## Context

Reported during TAAK-275: when several FluentAssertions global usings stay (e.g. `global using FluentAssertions.Primitives;` kept because something is Manual), `UsingDirectives.SwapGlobalUsings` added `global using Shouldly;` after each of them → CS0105 (duplicate using). A known `shortcut:` comment in that method described exactly this.

## Cause and fix

- `Fixer.SwapGlobalUsingsAsync` called `SwapGlobalUsings` per document without knowing what the project already imports globally.
- `SwapGlobalUsings` now takes `ISet<string> imported`: an added namespace already in it is skipped, an added one is recorded. `SwapGlobalUsingsAsync` seeds it per project (per target) from the compilation's existing global usings (`UsingDirectives.GlobalUsings`, now public; MSBuild `<Using>` included, the Fixer's own document excluded).
- Shortcut removed.

## Steps

- [x] Fix in `src/Unshackle.Core/UsingDirectives.cs` + `src/Unshackle.Core/Fixer.cs`
- [x] Unit tests `FixerTests.FixAsync_TwoKeptGlobalUsings_AddsNewOneOnce`, `FixAsync_AddedGlobalUsingAlreadyThere_DoesNotAddItAgain` (no proof: Fixer-level using handling, not a pattern)
- [x] build, tests, sample tests, pattern-docs hook green

## See also

- [TAAK-275](TAAK-275-fa-custom-assertion-classes.md)
