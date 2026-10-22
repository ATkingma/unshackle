# TAAK-307 — Moq → NSubstitute (roadmap step 7)

**Status**: step 1/6 — not started
**Created**: 2026-10-22
**Goal**: A `moq` fix that rewrites Moq 4.x test code to NSubstitute, at 100% on a Moq corpus (all builds, 0 regressions) and a blind round.

## Context for the next agent

- Why: Moq is used in a large share of .NET test suites; trust broke over SponsorLink (4.20, 2023). Last version before that: 4.18.4. Formally still BSD, so the README must state the motive honestly (no "went commercial" claim).
- Test code only, like FA: the user's own tests are the proof after a fix. Our own repo already uses NSubstitute.
- Known mapping (verify in the survey): `new Mock<T>()` -> `Substitute.For<T>()`, `mock.Object` -> the substitute, `Setup(x => x.M(It.IsAny<int>())).Returns(v)` -> `sub.M(Arg.Any<int>()).Returns(v)`, `ReturnsAsync`, `Throws`, `Callback`, `Verify(..., Times.Once)` -> `Received(1)`, `It.Is<T>(p)` -> `Arg.Is<T>(p)`. Hard: `MockBehavior.Strict`, `SetupSequence`, `Protected()`, `As<T>()`, `VerifyAll`/`VerifyNoOtherCalls`, `SetupProperty`, `CallBase` -> likely Manual.
- Follow the AutoMapper route that worked: survey (`docs/projectinfo/architectuur/corpus-automapper.md` as the model), a pattern queue (`claude/tasks/automapper-queue.md` as the model), a KingmaDoc plan + verify per pattern in `docs/projectinfo/patterns/` and a README row, proofs in `src/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`, corpus rounds with the harness `claude/tools/corpus-am.sh` (`FIX=<id>`), then a blind round.
- Unit-test types come from real assemblies (last free version of the old library, pinned in `Directory.Packages.props`; check its license allows that, like AutoMapper 14 with `NoWarn`).
- Design choices (target library, what is Fixed vs Manual) go to the council (`claude/docs/council.md`). A pattern never throws; unsafe rewrites are `Manual(reason)`.
- User selection 2026-10-22: all four new libraries (Moq, Polly, EPPlus, MassTransit); order Moq -> Polly -> EPPlus -> MassTransit, each to 100% before the next.

## Steps

- [ ] 1. Survey: 10-20 open-source repos with Moq 4.x tests (pinned shas, build status); API frequency per call shape; `docs/projectinfo/architectuur/corpus-moq.md`
- [ ] 2. Council: Fixed/Manual line for strict mocks, sequences, protected members, VerifyAll
- [ ] 3. `MoqFix` skeleton (+ registration, old/new package swap, using swap) with proofs; `claude/tasks/moq-queue.md` ranked by frequency
- [ ] 4. Patterns from the queue (one task per row via taak-start), each with plan/verify docs and proofs
- [ ] 5. Corpus round to 100% (all build after fix, 0 regressions), `claude/tasks/CORPUSRUN-moq.md`
- [ ] 6. Blind round (unseen repos), roadmap row in `docs/projectinfo/project.md`, README/NuGet text

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | new `Fixes/Moq/`, tests, docs |
| **How**   | New fix |
| **Why**   | Large user base wants off Moq; mechanical test-code rewrite proven by user tests |

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
