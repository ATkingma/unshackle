# Code-standaarden

Vastgelegd met de gebruiker op 2026-10-10. Basis: SOLID (zie `docs/projectinfo/architectuur/core.md` sectie "SOLID in het kort")
en de Microsoft .NET-conventies. Wat afdwingbaar is staat in `.editorconfig` (warnings = errors).

## 1. Modellen: class met `required init`

```csharp
public class Finding
{
    public required string FilePath { get; init; }
    public required int Line { get; init; }
    public required string PatternId { get; init; }
}
```

Geen records voor modellen. Uitzondering: `PatternResult` (zie 2).

## 2. Resultaat van een pattern: abstract record met fabrieksmethodes

```csharp
public abstract record PatternResult
{
    public static PatternResult Fixed(SyntaxNode newNode) => new FixedResult(newNode);
    public static PatternResult Manual(string reason) => new ManualResult(reason);
}
public record FixedResult(SyntaxNode NewNode) : PatternResult;   // eigen bestand
public record ManualResult(string Reason) : PatternResult;       // eigen bestand
```

Afhandelen met `switch`-expressie; onbekend subtype → `throw new UnreachableException()`.

## 3. Afhankelijkheden: klassieke constructor + readonly velden

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

Geen primary constructors (IDE0290 staat uit).

## 4. Naamgeving (Microsoft)

| Wat                     | Vorm          | Voorbeeld       |
| ----------------------- | ------------- | --------------- |
| Private veld            | `_camelCase`  | `_store`        |
| Private static readonly | `s_camelCase` | `s_extensions`  |
| Const                   | `PascalCase`  | `DefaultReport` |
| Interface               | `IPascalCase` | `IReportWriter` |
| Async methode           | `…Async`      | `FixAsync`      |

## 5. `var` alleen als het type zichtbaar is

```csharp
var findings = new List<Finding>();
FixReport report = await fixer.FixAsync(solution, request, cancellationToken);
```

## 6. Async

`Task`/`Task<T>`, `Async`-suffix, `CancellationToken cancellationToken` **altijd verplicht** (geen `= default`)
en altijd doorgeven. Geen `ConfigureAwait(false)` (CLI, geen sync-context).

## 7. Argumentcontrole alleen op publieke ingangen

`ArgumentNullException.ThrowIfNull(...)` in de `Fixer`, commands en `ISolutionStore`/`IReportWriter`-implementaties.
Intern (patterns, helpers) vertrouwen we op nullable reference types.

## 8. Foutafhandeling

Een pattern geeft `Manual(reden)` als het niet kan herschrijven. De `Fixer` vangt onverwachte exceptions per pattern af,
zodat één rare node nooit een hele run kost:

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

## 9. Collecties: in `IEnumerable<T>`, uit `IReadOnlyList<T>`

## 10. Pattern-metadata als properties; ID-formaat `<fix>.<kebab-case>`

```csharp
public class ShouldBePattern : IPattern
{
    public string Id => "fa.should-be";
    public string Description => "x.Should().Be(y) → x.ShouldBe(y)";
}
```

Prefixen: `fa` (FluentAssertions), `mediatr`, `automapper`.

## 11. Patterns expliciet registreren in de Fix

```csharp
public IReadOnlyList<IPattern> Patterns { get; } = [new ShouldBePattern(), new HaveCountPattern()];
```

Geen assembly-scan, geen reflectie.

## 12. Zichtbaarheid en `sealed`

Geen vaste regel voor `public`/`internal`. **Nooit `sealed`** (CA1852 staat uit).

## 13. Bestanden en namespaces

File-scoped namespace die de map volgt (`Unshackle.Core.Fixes.FluentAssertions.Patterns`). Eén type per bestand,
ook voor kleine records.

## 14. Expression-bodied en LINQ

Expression-bodied als het op één regel past. LINQ voor transformaties.

## 15. Tests

Elke test heeft `// Arrange`, `// Act`, `// Assert`. Testnamen: nog te kiezen.

## 16. Commentaar

XML-docs (`/// <summary>`) op alle publieke contracten in `src/Unshackle.Core/Abstractions/` (afgedwongen met CS1591).
Elders alleen commentaar dat het _waarom_ uitlegt.

## 17. Design patterns

GoF-boek + refactoring.guru als naslag, alleen bij concrete behoefte. Zie `claude/docs/design-patterns.md`.

## Zie ook

- `docs/projectinfo/architectuur/core.md` — Fixer → Fix → Pattern, SOLID
- `.editorconfig` — afgedwongen deel van deze regels
- `claude/docs/design-patterns.md` — welke GoF-patronen waar
- `claude/docs/INDEX.md` — documentatie-index
