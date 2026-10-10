# Beslissing: Fixed of Manual

**Figure 1.** De gedeelde stappen die elk FluentAssertions-pattern in `Rewrite` doorloopt (`FluentAssertionsSyntax`). Elk pattern voegt daar eigen controles tussen; die staan in [patronen.md](patronen.md).

<!-- kingmadoc:diagram img/pattern-beslissing-1.d2 -->
![Beslissing: Fixed of Manual](img/pattern-beslissing-1.png)

| Stap | Hulp in `FluentAssertionsSyntax` | Uitkomst bij falen |
| ---- | -------------------------------- | ------------------ |
| Vorm | `GetSubject` | Manual: niet `x.Should().Methode(...)` |
| Resultaat | `IsResultDiscarded` | Manual: Shouldly geeft niets terug (of het subject) |
| Argumenten | `TryGetArguments` | Manual: Shouldly kan `becauseArgs` niet opmaken |
| Eigen controles | per pattern | Manual met patroon-eigen reden |

## Zie ook

- [patronen.md](patronen.md) — de eigen controles per pattern
- [fix-flow.md](fix-flow.md) — wat de Fixer met het resultaat doet
- [../README.md](../README.md)
