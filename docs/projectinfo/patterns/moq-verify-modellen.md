# Models: moq.verify

Facts from `src/Unshackle.Core/Fixes/Moq/Patterns/VerifyPattern.cs` and `VerifySyntax.cs`.

**Figure 1.** Classes.

```mermaid
classDiagram
    class VerifyPattern { Id = moq.verify }
    class VerifySyntax { IsVerify(); Plan(); Received(times) }
    class MockCall { Rewrite(lambda, receiver) }
    class MockUsage { Analyze(symbol, pendingUses) }
    class MockUseRules { Blocker(use) }
    VerifyPattern --> MockUsage
    VerifyPattern --> VerifySyntax
    VerifySyntax --> MockCall
    MockUsage --> MockUseRules
    MockUseRules --> VerifySyntax
```

**Figure 2.** Order in `MoqFix.Patterns`: `moq.setup-returns`, `moq.verify`, then `moq.create`, which sees only `.Object` uses left.

```mermaid
sequenceDiagram
    Fixer->>VerifyPattern: Rewrite(statement)
    VerifyPattern->>MockUsage: Analyze(m, pendingUses: true)
    VerifyPattern-->>Fixer: m.Object.Received(1).M(Arg..)
    Fixer->>CreatePattern: Rewrite on the new document
    CreatePattern-->>Fixer: m.Received(1).M(Arg..)
```

**Figure 3.** Decision tree.

| Question | If no: Manual | Proof |
| --- | --- | --- |
| Receiver is a local or private field of this file? | `MockPassedAround` | - |
| Whole mock converts (`MockUsage`)? | its reason | `verify-all-blocks` |
| No failure message? | `VerifyMessage` | `message` |
| `Times` is Once, Never, Exactly(n), AtLeastOnce or absent? | `TimesRange` | `at-least` |
| Lambda and arguments expressible (`MockCall`, `ArgMatchers`)? | `CallShape`, `Matcher*`, ... | `ArgMatchersTests` |

| Times | NSubstitute | Proofs |
| --- | --- | --- |
| Once | `Received(1)` | `once`, `once-method-group` |
| Never | `DidNotReceive()` | `never`, `property` |
| Exactly(n) | `Received(n)` | `exactly` |
| AtLeastOnce / none | `Received()` | `no-times` |

## See also

- [moq-verify-plan.md](moq-verify-plan.md)
- [moq-verify-verify.md](moq-verify-verify.md)
