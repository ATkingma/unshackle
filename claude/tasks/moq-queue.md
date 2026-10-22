# Pattern queue (Moq 4.x → NSubstitute)

**Decision (council round 8, 2026-10-22)**: NSubstitute only (M5). Fixed only when the behaviour is proven the same (general rule);
everything else `Manual` with a typed reason code. Same way of working as `claude/tasks/automapper-queue.md`; a row gets a TAAK
number (range 320–339) when claimed. Frequencies: `docs/projectinfo/architectuur/corpus-moq.md` (13 repos).

Order: projects first, hits second. Rows that rewrite a use of a mock (2–9) run **before** `moq.create` in `MoqFix.Patterns`:
`moq.create` only converts a mock whose every remaining use is `.Object`, so each landed row unlocks more of row 1.

| #   | Pattern ID | Moq | Expected | Corpus (hits / projects) | Status |
| --- | ---------- | --- | -------- | ------------------------ | ------ |
| 0   | _infrastructure_ | `MoqFix`, package + using swap, proof runner entry, Moq 4.18.4 types | — | — | done (TAAK-307 step 3) |
| 1   | `moq.create` | `new Mock<T>()` (+ literal `Loose`/`Default`), `Mock<T>` locals/private fields, `.Object`, `Mock.Of<T>()` | `Substitute.For<T>()`; Manual `DefaultValueDiffers` where unconfigured answers differ | 2389 / 12; `.Object` 3981 / 13; `Mock.Of` 664 / 9 | done (TAAK-307 step 3) |
| 2   | `moq.arg-matchers` | `It.IsAny<T>()`, `It.Is<T>(pred)`, `It.IsNotNull`, `It.IsIn`, `It.IsRegex` | `Arg.Any<T>()`, `Arg.Is<T>(pred)`, `Arg.Is<T>(x => x != null)`, ... (only inside a rewritten setup/verify) | 5396 / 13; 485 / 10; 32 / 2 | done as helper `ArgMatchers` (no own pattern: matchers only live in Setup/Verify lambdas; run-time proven through rows 3-4) |
| 3   | `moq.setup-returns` | `m.Setup(x => x.M(..)).Returns(v)` / `ReturnsAsync(v)`, `Mock.Of<T>(pred)` | `m.M(..).Returns(v)` / `Returns(Task.FromResult(v))` | 4055 / 13; 3326 / 13; 1139 / 10 | done (TAAK-307; `Mock.Of(pred)` still Manual) |
| 4   | `moq.verify` | `m.Verify(x => .., Times.Once/Never/Exactly(n)/AtLeastOnce)` | `m.Received(1).M(..)` / `DidNotReceive()` / `Received(n)` / `Received()`; ranges Manual | 1406 / 12; Times.Once 819 / 11; Never 496 / 12 | done (TAAK-307; ranges and messages Manual) |
| 5   | `moq.properties` | `SetupGet`, `SetupSet`, `SetupProperty`, `SetupAllProperties`, `VerifyGet`/`VerifySet` | `m.P.Returns(v)`, `m.Received().P = v`; property semantics difference checked | 443 / 8; 202 / 3; 70 / 4 | done (TAAK-307; in setup-returns/verify/create; `SetupSet` Manual) |
| 6   | `moq.throws` | `Throws(ex)` / `ThrowsAsync(ex)` | `.Throws(ex)` / `.ThrowsAsync(ex)` (`NSubstitute.ExceptionExtensions`) | 257 / 8; 89 / 9 | done (TAAK-307; void → When/Do) |
| 7   | `moq.returns-lambda` | `Returns((a, b) => ..)` | `Returns(ci => ..)` with `ci.ArgAt<T>(i)` | 302 / 10 | done (TAAK-307; method groups Manual) |
| 8   | `moq.callback` | `Callback(..)` | `AndDoes(..)` / `When(..).Do(..)` | 193 / 11 | done (TAAK-307; with a Returns function, Throws or overlapping setups Manual) |
| 9   | `moq.class-mocks` | `new Mock<T>(ctor args)`, class and delegate mocks, `CallBase = true` | `Substitute.ForPartsOf<T>(args)` where provably the same, else Manual | 23 / 6; CallBase 10 / 4 | done (TAAK-307; only `CallBase = true` + `.Object`, no setups; rest Manual) |
| 10  | `moq.verify-all` | `Verifiable()` + `Verify()`, `VerifyAll()` | one `Received()` per covered setup when every setup of that mock is local to the test method (council M4), else Manual `VerifyScopeNotLocal`; `VerifyNoOtherCalls` Manual | 85+37 / 6; 28 / 4; 101 / 5 | done (TAAK-307; `VerifyNoOtherCalls` Manual; void setups removed) |
| 11  | `moq.setup-sequence` | `SetupSequence(..).Returns(a).Returns(b)` | `Returns(a, b)` with a warning (NSubstitute repeats the last value) only when every step is `Returns`/`ReturnsAsync` (council M2), else Manual | 10 / 3 | done (TAAK-307; Throws/Pass/function steps, void and overlapping setups Manual) |
| 12  | `moq.logger-verify` | `Mock<ILogger<T>>` `Verify(l => l.Log(.., It.IsAnyType ..))` | own design; Manual until then | 109 / 9; It.IsAnyType 72 / 4 | done (TAAK-307; `Arg.AnyType`, state predicate only `ToString()`, else Manual) |
| 13  | `moq.unsupported` | `MockBehavior.Strict` (M1 `StrictBehaviour`), `Protected()` (M3, fake-handler recipe), `As<T>()`, `Invocations`, `Reset`, `Raise`, `It.Ref`, `AutoMocker`/AutoMoq (M5) | Manual with reason code; `Reset` → `ClearReceivedCalls` and `Raise` → `Raise.EventWith` only if proven | Strict 232 / 8; Protected 53 / 5; AutoMocker 1745 / 5; As 21 / 3 | open |
| 14  | _corpus run_ | build + tests before/after on the 13-repo corpus (`claude/tools/corpus-am.sh FIX=moq`) | 0 regressions; Manual only where justified | — | open (TAAK-307 step 5) |

**Recommended next**: the corpus round (row 14, main session), then rows 9, 11-13. Every row adds
one rule to `MockUseRules` and a pattern before `CreatePattern`; rows touching `MockUseRules` go one after another, not in parallel.
Every row needs run-time proofs in `src/Unshackle.Tests/Proofs/Moq/_Runtime/` (council round 9, Q2).

## See also

- `claude/tasks/TAAK-307-moq-to-nsubstitute.md` — the task
- `docs/projectinfo/architectuur/corpus-moq.md` — frequencies and the draft queue
- `claude/docs/council.md` — round 8 (general rule, M1–M5)
- `docs/projectinfo/patterns/moq-create-plan.md` — row 1
