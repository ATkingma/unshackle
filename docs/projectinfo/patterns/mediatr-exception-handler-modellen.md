# Modellen: mediatr.exception-handler

Feiten uit `src/Unshackle.Core/Fixes/MediatR/Patterns/ExceptionHandlerPattern.cs` en `MediatRSyntax.cs`.

## Figuur 1: klassen

```mermaid
classDiagram
    IPattern <|.. ExceptionHandlerPattern
    ExceptionHandlerPattern ..> MediatRSyntax : DeclaredInterface / WithBaseClass / WithConstraint / CommaList
    MediatRFix o-- ExceptionHandlerPattern
```

## Figuur 2: sequence

```mermaid
sequenceDiagram
    Fixer->>ExceptionHandlerPattern: Find(root, model)
    ExceptionHandlerPattern-->>Fixer: type declarations
    Fixer->>ExceptionHandlerPattern: Rewrite(type, model)
    ExceptionHandlerPattern-->>Fixer: Fixed(type) or Manual(reason)
    Fixer->>Fixer: AllOrNothing: any Manual → original solution
```

## Figuur 3: beslisboom

```text
abstract or Handle elsewhere? -> Manual
async / no block body? -> Manual
state only as SetHandled + return? -> no: Manual
all returns Task.CompletedTask? -> no: Manual
already a base class? -> Manual
else: Fixed
```
