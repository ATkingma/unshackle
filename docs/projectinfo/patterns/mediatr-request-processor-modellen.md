# Modellen: mediatr.request-processor

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs` en `MediatRSyntax.cs`.

## Figuur 1: klassen

```mermaid
classDiagram
    IPattern <|.. RequestProcessorPattern
    RequestProcessorPattern ..> MediatRSyntax : DeclaredInterface / WithBaseClass / WithConstraint / CommaList
    MediatRFix o-- RequestProcessorPattern
```

## Figuur 2: sequence

```mermaid
sequenceDiagram
    Fixer->>RequestProcessorPattern: Find(root, model)
    RequestProcessorPattern-->>Fixer: type declarations
    Fixer->>RequestProcessorPattern: Rewrite(type, model)
    RequestProcessorPattern-->>Fixer: Fixed(type) or Manual(reason)
    Fixer->>Fixer: AllOrNothing: any Manual → original solution
```

## Figuur 3: beslisboom

```text
Process declared here, not explicit? -> no: Manual
Task.CompletedTask or async? -> no: Manual
open pre-processor: TResponse free and no other use? -> no: Manual
already a base class? -> yes: Manual
else: Fixed
```
