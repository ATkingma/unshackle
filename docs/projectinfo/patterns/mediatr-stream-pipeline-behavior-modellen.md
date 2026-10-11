# Modellen: mediatr.stream-pipeline-behavior

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/StreamPipelineBehaviorPattern.cs` en `MediatRSyntax.cs`.

## Figuur 1: klassen

```mermaid
classDiagram
    IPattern <|.. StreamPipelineBehaviorPattern
    StreamPipelineBehaviorPattern ..> MediatRSyntax : DeclaredInterface / WithBaseClass / WithConstraint / CommaList
    MediatRFix o-- StreamPipelineBehaviorPattern
```

## Figuur 2: sequence

```mermaid
sequenceDiagram
    Fixer->>StreamPipelineBehaviorPattern: Find(root, model)
    StreamPipelineBehaviorPattern-->>Fixer: type declarations
    Fixer->>StreamPipelineBehaviorPattern: Rewrite(type, model)
    StreamPipelineBehaviorPattern-->>Fixer: Fixed(type) or Manual(reason)
    Fixer->>Fixer: AllOrNothing: any Manual → original solution
```

## Figuur 3: beslisboom

```text
explicit Handle? -> Manual
form Handle(request, StreamHandlerDelegate<T> next, ct)? -> no: Manual
every use of next is next()? -> no: Manual
else: Fixed
```
