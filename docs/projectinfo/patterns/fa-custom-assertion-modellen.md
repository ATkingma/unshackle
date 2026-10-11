# Modellen: fa.custom-assertion

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs`, `ReportRow.FromAll` en `SpectreConsoleView.ShowSummary`.

## Figuur 1: klassen

```mermaid
classDiagram
    class IPattern
    class CustomAssertionPattern {
        +PatternId : string
        +Find(root, model)
        +Rewrite(node, model) PatternResult
        +Group(findings)$ IEnumerable~IGrouping~
    }
    IPattern <|.. CustomAssertionPattern
    ReportRow ..> CustomAssertionPattern : Group
    SpectreConsoleView ..> CustomAssertionPattern : Group
```

## Figuur 2: sequence

```mermaid
sequenceDiagram
    Fixer->>CustomAssertionPattern: Find (invocations met eigen methode op FA-type)
    Fixer->>CustomAssertionPattern: Rewrite → Manual(reden per extensie)
    ScanCommand->>SpectreConsoleView: ShowSummary(findings) → Group → aantal per extensie
    ScanCommand->>ReportWriter: WriteAsync → ReportRow.FromAll → één rij per extensie
```

## Figuur 3: beslisboom

```mermaid
flowchart TD
    A[invocation x.M] --> B{M uit FluentAssertions-namespace?}
    B -- ja --> N[geen match]
    B -- nee --> C{receiver/declarerend type of basistype: FA-type *Assertions?}
    C -- nee --> N
    C -- ja --> M[Manual: Custom assertion Type.M]
```
