# TAAK-011 — Pattern fa.not-throw

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `act.Should().NotThrow()` becomes `Should.NotThrow(act)`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 8 in `claude/tasks/patroon-queue.md`. Sample: `MappingTests.cs:14`. Example: TAAK-008 (`fa.throw`).

## Analysis

FA 7.2.2: `NotThrow(because, args)` on `ActionAssertions`/`FunctionAssertions<T>` (`.Which` = value), `NotThrow<T>`, `NotThrowAfter`. Shouldly 4.3.0: `Should.NotThrow(Action[, customMessage])` and generic `NotThrow<T>(Func<T>[, customMessage])`.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Subject: any parameterless `Action`/`Func<T>`, including value types | Shouldly's `NotThrow<T>(Func<T>)` is generic (unlike `Throw`) |
| 2 | `NotThrow<T>()` → Manual | FA only forbids type T; Shouldly forbids any exception |
| 3 | `NotThrowAfter` → Manual | Waits and polls; no equivalent |
| 4 | Result used (`.Which`, `.And`, variable) → Manual | Shouldly returns the value, but the shape differs; possibly later |
| 5 | `Invoking`, becauseArgs → Manual | Same as `fa.throw` |
| 6 | `NotThrowAsync` out of scope; `Func<Task>.Should().NotThrow()` does not compile in FA | Async belongs with the async patterns; no guard needed |
| 7 | `ThrowPattern` left untouched; small duplication of the subject check | Assignment; a shared helper can come later |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan (8 REQ)
- [x] 4. `NotThrowPattern` + registration
- [x] 5. Proofs (10 cases in `NotThrow/`)
- [x] 6. Build/test green (108 + 15 sample), verify, archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/NotThrowPattern.cs` (new), `FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/NotThrow/`
- `docs/projectinfo/patterns/fa-not-throw-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/archief/TAAK-008-fa-throw.md`
