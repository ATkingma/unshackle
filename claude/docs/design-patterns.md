# Design patterns

Use the names and forms from the Gang of Four book (_Design Patterns_, Gamma, Helm, Johnson, Vlissides) with
[refactoring.guru/design-patterns](https://refactoring.guru/design-patterns) as reference. Name the pattern in
a `## See also` or a comment when it is not obvious. Only apply one when there is a concrete need.

| Place in Unshackle                                 | GoF pattern                                                               |
| -------------------------------------------------- | ------------------------------------------------------------------------- |
| `IPattern`, `IFix`, `IReportWriter`                | [Strategy](https://refactoring.guru/design-patterns/strategy)             |
| `Fixer` as the single entry point for Interface    | [Facade](https://refactoring.guru/design-patterns/facade)                 |
| `CSharpSyntaxRewriter` / `CSharpSyntaxWalker`      | [Visitor](https://refactoring.guru/design-patterns/visitor)               |
| Choosing the report writer by `--report` extension | [Factory Method](https://refactoring.guru/design-patterns/factory-method) |
| `PatternResult.Fixed(...)` / `.Manual(...)`        | Static factory method (not GoF, but a common variant)                     |

## See also

- `claude/docs/code-standaarden.md` — other code style
- `docs/projectinfo/architectuur/core.md` — Fixer → Fix → Pattern
- `claude/docs/INDEX.md` — documentation index
