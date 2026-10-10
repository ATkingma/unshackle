# Design patterns

Gebruik de namen en vormen uit het Gang of Four-boek (_Design Patterns_, Gamma, Helm, Johnson, Vlissides) met
[refactoring.guru/design-patterns](https://refactoring.guru/design-patterns) als naslag. Noem het patroon bij naam in
een `## Zie ook` of commentaar als het niet voor de hand ligt. Alleen toepassen als er een concrete behoefte is.

| Plek in Unshackle                               | GoF-patroon                                                               |
| ----------------------------------------------- | ------------------------------------------------------------------------- |
| `IPattern`, `IFix`, `IReportWriter`             | [Strategy](https://refactoring.guru/design-patterns/strategy)             |
| `Fixer` als enige ingang voor Interface         | [Facade](https://refactoring.guru/design-patterns/facade)                 |
| `CSharpSyntaxRewriter` / `CSharpSyntaxWalker`   | [Visitor](https://refactoring.guru/design-patterns/visitor)               |
| Report writer kiezen op extensie van `--report` | [Factory Method](https://refactoring.guru/design-patterns/factory-method) |
| `PatternResult.Fixed(...)` / `.Manual(...)`     | Static factory method (geen GoF, wel een gangbare variant)                |

## Zie ook

- `claude/docs/code-standaarden.md` — overige codestijl
- `docs/projectinfo/architectuur/core.md` — Fixer → Fix → Pattern
- `claude/docs/INDEX.md` — documentatie-index
