# TAAK-307 — Moq → NSubstitute (roadmap step 7)

**Status**: steps 1-3/6 done; step 4 in progress (queue rows 0-8 and 10 done; corpus round next, main session)
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

- [x] 1. Survey: 10-20 open-source repos with Moq 4.x tests (pinned shas, build status); API frequency per call shape; `docs/projectinfo/architectuur/corpus-moq.md`
- [x] 2. Council: Fixed/Manual line for strict mocks, sequences, protected members, VerifyAll
- [x] 3. `MoqFix` skeleton (+ registration, old/new package swap, using swap) with proofs; `claude/tasks/moq-queue.md` ranked by frequency
- [ ] 4. Patterns from the queue (one task per row via taak-start), each with plan/verify docs and proofs
- [ ] 5. Corpus round to 100% (all build after fix, 0 regressions), `claude/tasks/CORPUSRUN-moq.md`
- [ ] 6. Blind round (unseen repos), roadmap row in `docs/projectinfo/project.md`, README/NuGet text

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | new `Fixes/Moq/`, tests, docs |
| **How**   | New fix |
| **Why**   | Large user base wants off Moq; mechanical test-code rewrite proven by user tests |

## Progress log

- 2026-10-22 (agent moq): step 1 done. 13 repos (5 new clones in `D:/playground/corpus-moq/src`, 8 reused), list in
  `claude/tools/corpus-moq-repos.txt`, results in `docs/projectinfo/architectuur/corpus-moq.md` (frequencies, draft queue,
  council questions). Radarr build blocked by a full `C:` disk (retry later); EventFlow not built.

- 2026-10-22 (agent moq-skeleton): steps 2-3 done. Council round 8 answered M1-M5. `MoqFix` (id `moq`, Moq → NSubstitute 6.2.0,
  usings `Moq*` → `NSubstitute`) and `moq.create` (`src/Unshackle.Core/Fixes/Moq/`): a local/private-field mock used only as
  `.Object`, `new Mock<T>().Object` and `Mock.Of<T>()` become `Substitute.For<T>()`, only for interfaces whose unconfigured answers
  provably match (`MockDefaults`); else Manual with a reason code (`StrictBehaviour`, `MockUsedBeyondObject`, `DefaultValueDiffers`,
  `MockPassedAround`, `MockGet`, `MockOfSetup`, `ClassMock`, `ConstructorArguments`, `MockOptions`, `MockDeclaration`). 11 proofs in
  `src/Unshackle.Tests/Proofs/Moq/Create/`, 10 unit tests in `src/Unshackle.Tests/Core/Moq/`, plan/modellen/verify
  `docs/projectinfo/patterns/moq-create-*`, queue `claude/tasks/moq-queue.md`. `ProofTests` got a `Moq` entry (granted by the
  coordinator). Open for the main session: `Cli.cs` registration, README row, INDEX line. Council questions in the final report.

- 2026-10-22 (agent moq-skeleton, continued): council round 9 applied. Row 2 `ArgMatchers`/`MockCall` (helper, no own pattern).
  Run-time harness `MoqRuntimeProofTests` + `Proofs/Moq/_Runtime/` (before on Moq, after on NSubstitute via the real fix, same outcome,
  `// Expect:` cases that fail under both). `moq.create` now pins Moq's null defaults (`ReturnsNull`, max 10, Info line), Info
  "mock never used"; shared per-mock analysis in `MockUsage`/`MockUseRules`. Row 3 `moq.setup-returns` and row 4 `moq.verify`
  with proofs, run-time proofs and plan/modellen/verify. Open: Q3 `TargetFrameworkUnsupported` needs the project TFM (Fixer/project
  step, not a pattern); README rows for moq.setup-returns and moq.verify; `Cli.cs` should pass the NSubstitute reference.

- 2026-10-22 (agent moq-skeleton, round 10): C1 reason codes (`MoqReason` on `ManualResult.Code`, rendered text unchanged); Q3
  `TargetFrameworkUnsupported` via `IProjectFileMigration.ProjectBlockers` plus Manual in every pattern (engine gap: the project row does not
  block `IFix.Package`); row 10 `moq.verify-all` (M4), row 6 `moq.throws`, row 5 properties (in setup-returns/verify/create). Each with
  proofs, run-time proofs and plan/modellen/verify docs.

- 2026-10-22 (agent moq-skeleton): row 7 `moq.returns-lambda` (`CallLambda`: parameters → `ci.ArgAt<T>(i)`), row 8 `moq.callback`
  (AndDoes / When-Do). The harness found three NSubstitute differences: a configuration call runs earlier matching configurations
  (now `Configure()` when a member has several setups, rows 3/6/7/8), AndDoes runs before a Returns function, and AndDoes/Do runs for
  every matching configuration (both Manual).

- 2026-10-22 (agent L2): row 9 `moq.class-mocks` done. Probe (`<corpus>/l2/probe`, Moq 4.18.4 vs NSubstitute 6.2.0): `CallBase = true` class
  mock equals `ForPartsOf<C>(args)` for ctor, virtual/non-virtual/protected members, properties, ToString/Equals, ctor virtual calls; differs
  for no-CallBase (null vs ""), a Setup on a partial (NSubstitute runs the real member once), abstract members. `ClassMocksPattern` derives
  from `CreatePattern` (own id); new reasons `ClassMockNoCallBase`, `ClassMockAbstract`, `ClassMockSetup`. 10 proofs + 2 run-time proofs.

- 2026-10-22 (agent L2): rows 11-13 done. Row 11 `moq.setup-sequence` (all-Returns steps become `Returns(a, b)` with the exhaustion warning). Row 12
  `moq.logger-verify`: `ILogger.Log` verifies become `Received(n).Log(.. Arg.AnyType ..)`; fixed the formatter type argument (`It.IsAny<Func<It.IsAnyType,..>>`
  was left as Moq, nullable annotation stripped). Row 13 `moq.unsupported`: reason codes `ProtectedMember`, `MockAs`, `MockInvocations`, `MockReset`,
  `MockRaise`, `AutoMocker` (class mocks too); `Reset`/`Raise` stay Manual (not proven). Next: step 5 corpus round (L6).

## See also

- `claude/docs/INDEX.md` — documentation index
- `docs/projectinfo/project.md` — roadmap
- `claude/tasks/automapper-queue.md` — the route this follows
- `claude/tasks/moq-queue.md` — the Moq pattern queue
- Library licence overview: https://dariusz-wozniak.github.io/fossed/
