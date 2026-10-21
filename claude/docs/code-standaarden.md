# Code standards

Agreed with the user on 2026-10-10. Basis: SOLID (see `docs/projectinfo/architectuur/core.md` section "SOLID in het kort")
and the Microsoft .NET conventions. Whatever can be enforced is in `.editorconfig` (warnings = errors).

## 1. Models: class with `required init`

```csharp
public class Finding
{
    public required string FilePath { get; init; }
    public required int Line { get; init; }
    public required string PatternId { get; init; }
}
```

No records for models. Exception: `PatternResult` (see 2).

## 2. Result of a pattern: abstract record with factory methods

```csharp
public abstract record PatternResult
{
    public static PatternResult Fixed(SyntaxNode newNode) => new FixedResult(newNode);
    public static PatternResult Manual(string reason) => new ManualResult(reason);
}
public record FixedResult(SyntaxNode NewNode) : PatternResult;   // eigen bestand
public record ManualResult(string Reason) : PatternResult;       // eigen bestand
```

Handle with a `switch` expression; unknown subtype → `throw new UnreachableException()`.

## 3. Dependencies: classic constructor + readonly fields

```csharp
public class Fixer : IFixer
{
    private readonly IReadOnlyList<IFix> _fixes;

    public Fixer(IEnumerable<IFix> fixes)
    {
        _fixes = [.. fixes];
    }
}
```

No primary constructors (IDE0290 is off).

## 4. Naming (Microsoft)

| What                    | Form          | Example         |
| ----------------------- | ------------- | --------------- |
| Private field           | `_camelCase`  | `_store`        |
| Private static readonly | `s_camelCase` | `s_extensions`  |
| Const                   | `PascalCase`  | `DefaultReport` |
| Interface               | `IPascalCase` | `IReportWriter` |
| Async method            | `…Async`      | `FixAsync`      |

## 5. `var` only when the type is visible

```csharp
var findings = new List<Finding>();
FixReport report = await fixer.FixAsync(solution, request, cancellationToken);
```

## 6. Async

`Task`/`Task<T>`, `Async` suffix, `CancellationToken cancellationToken` **always required** (no `= default`)
and always passed on. No `ConfigureAwait(false)` (CLI, no sync context).

## 7. Argument checks only at public entry points

`ArgumentNullException.ThrowIfNull(...)` in the `Fixer`, commands, `ISolutionStore`/`IReportWriter` implementations, fix constructors and public `IFix` members (council round 6, Q5: built at the composition root or called by the `Fixer`).
Internally (patterns, helpers) we rely on nullable reference types.

## 8. Error handling

A pattern returns `Manual(reason)` if it cannot rewrite. The `Fixer` catches unexpected exceptions per pattern,
so that one odd node never costs an entire run:

```csharp
try
{
    result = pattern.Rewrite(node, model);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    result = PatternResult.Manual($"Internal error in {pattern.Id}: {ex.Message}");
}
```

## 9. Collections: in `IEnumerable<T>`, out `IReadOnlyList<T>`

## 10. Pattern metadata as properties; ID format `<fix>.<kebab-case>`

```csharp
public class ShouldBePattern : IPattern
{
    public string Id => "fa.should-be";
    public string Description => "x.Should().Be(y) → x.ShouldBe(y)";
}
```

Prefixes: `fa` (FluentAssertions), `mediatr`, `automapper`.

## 11. Register patterns explicitly in the Fix

```csharp
public IReadOnlyList<IPattern> Patterns { get; } = [new ShouldBePattern(), new HaveCountPattern()];
```

No assembly scanning, no reflection.

## 12. Visibility and `sealed`

No fixed rule for `public`/`internal`. **Never `sealed`** (CA1852 is off).

## 13. Files and namespaces

File-scoped namespace that follows the folder (`Unshackle.Core.Fixes.FluentAssertions.Patterns`). One type per file,
also for small records.

## 14. Expression-bodied and LINQ

Expression-bodied if it fits on one line. LINQ for transformations.

## 15. Tests

Every test has `// Arrange`, `// Act`, `// Assert`. Test names: still to be decided.

## 16. Comments

XML docs (`/// <summary>`) on all public contracts in `src/Unshackle.Core/Abstractions/` (enforced with CS1591).
Elsewhere only comments that explain the _why_.

## 17. Design patterns

The GoF book + refactoring.guru as reference, only when there is a concrete need. See `claude/docs/design-patterns.md`.

## See also

- `docs/projectinfo/architectuur/core.md` — Fixer → Fix → Pattern, SOLID
- `.editorconfig` — enforced part of these rules
- `claude/docs/design-patterns.md` — which GoF patterns where
- `claude/docs/INDEX.md` — documentation index
