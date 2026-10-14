# TAAK-033 — Pattern fa.and-chain

**Status**: ✅ completed
**Created**: 2026-10-11
**Goal**: `x.Should().A().And.B();` becomes `x.ShouldA(); x.ShouldB();`, with every link rewritten by its own pattern.

## Context for the next agent

- Row 16 in `claude/tasks/patroon-queue.md` (round 2). Example of splitting: `BeGreaterLessPattern` (TAAK-010).

## Analysis

FA 7.2.2: almost every assertion returns `AndConstraint<TAssertions>` or `AndWhichConstraint<TAssertions, T>`; `.And` is the same assertions instance on the same subject. Exception: `Throw<T>().And` is the exception itself (no FA methods). Shouldly has no chains: every `ShouldX` is a separate call. Splitting evaluates the subject again per link.

## Choices

| #   | Choice                                                                                                                    | Reason                                                                                                                                                   |
| --- | ------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | `AndChainPattern` is a Composite over the assertion patterns (`FluentAssertionsFix.s_assertions`)                         | No duplicated link logic; new patterns work in chains automatically                                                                                      |
| 2   | Split into `x.Should().M(args);` and compile speculatively (`Compilation.ReplaceSyntaxTree`)                              | The link patterns need a semantic model; this provides the real model for the split code                                                                 |
| 3   | One link not found or Manual → the whole chain Manual                                                                     | Half-rewritten chains are confusing; the reason names the link                                                                                           |
| 4   | Side-effect free = `this`, local, parameter, field, property without arguments (and member access on those, or on a type) | Conservative, as requested; calls and indexers → Manual                                                                                                  |
| 5   | The `.And` receiver must have the same type as `x.Should()`                                                               | A same-named method on another subject would silently check something else after splitting. No compilable FA 7 example found, so without a proof        |
| 6   | The pattern sits directly after `AllSatisfyPattern`, before the assertion patterns                                        | Those would otherwise report `.And` links as Manual; unfold AllSatisfy loops first                                                                       |
| 7   | `BeGreaterLessPattern` keeps its own chain logic                                                                          | Does not touch its proofs; in the full fix `fa.and-chain` takes the chain first                                                                          |

## Steps

- [x] 1. Task, research, choices
- [x] 2. KingmaDoc plan (7 REQ)
- [x] 3. `AndChainPattern` + registration
- [x] 4. Proofs (15 cases in `AndChain/`)
- [x] 5. Build/test green (412 + 38 sample), models, verify, archive, row `done`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs` (new), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/`
- `docs/projectinfo/patterns/fa-and-chain-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (row), `edge-cases.md`

## See also

- `claude/tasks/archief/TAAK-010-fa-be-greater-less.md`
- `docs/projectinfo/patterns/fa-and-chain-plan.md`
