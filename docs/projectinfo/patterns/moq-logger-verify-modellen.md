# Models: moq.logger-verify

Facts from `Patterns/LoggerVerifyPattern.cs`, `Patterns/VerifyPattern.cs` and `ArgMatchers.cs`.

**Figure 1.** Classes.

```mermaid
classDiagram
    class VerifyPattern { Id = moq.verify; Logs = false }
    class LoggerVerifyPattern { Id = moq.logger-verify; Logs = true }
    class ArgMatchers { Rewrite(); Matcher() }
    VerifyPattern <|-- LoggerVerifyPattern
    VerifyPattern --> VerifySyntax
    VerifySyntax --> ArgMatchers
```

**Figure 2.** Order: before `moq.verify` (which skips `ILogger.Log` verifies) and `moq.create`.

```mermaid
sequenceDiagram
    Fixer->>LoggerVerifyPattern: Rewrite(logger.Verify(l => l.Log(..), Times.Never());)
    LoggerVerifyPattern->>MockUsage: Analyze(logger, pendingUses: true)
    LoggerVerifyPattern->>VerifySyntax: Plan()
    VerifySyntax->>ArgMatchers: Rewrite(arguments)
    LoggerVerifyPattern-->>Fixer: Fixed(logger.Object.DidNotReceive().Log(..);)
```

**Figure 3.** Decision tree.

| Question                                          | If no: Manual         | Proof        |
| ------------------------------------------------- | --------------------- | ------------ |
| Whole mock converts?                              | its reason            | -            |
| `Times` has an exact form?                        | `TimesRange`          | (moq.verify) |
| State predicate only calls `ToString()` (or none)? | `MatcherUnsupported`  | `state-cast` |
| Leaf                                              | `Received(n).Log(..)` | `once`, `never`, run-time `logger-pass` |

## See also

- [moq-logger-verify-plan.md](moq-logger-verify-plan.md)
- [moq-logger-verify-verify.md](moq-logger-verify-verify.md)
