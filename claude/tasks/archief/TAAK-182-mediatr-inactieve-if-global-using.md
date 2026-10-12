# TAAK-182 — MediatR: inactieve `#if`-code zonder MediatR houdt `global using MediatR` vast

**Status**: ✅ voltooid — 2026-10-12 (stap 3 deels: harnas op jason niet gedraaid, corpusproject niet beschikbaar)
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`; actie 10 uit de run van 2026-10-11)
**Doel**: jasontaylor CleanArchitecture v8.0.0 migreert volledig.

## Context voor de volgende agent

- jason (`f2f0445721f5e3efe98ea22266148c698b3ef288`, tag v8.0.0, MediatR 12.1.1): `src/Web/GlobalUsings.cs:3 global using MediatR;`. `Web` heeft `#if (UseApiOnly)`-blokken (dotnet-new-template) in `Program.cs`, `DependencyInjection.cs`, `Endpoints/Users.cs`; geen ervan noemt een MediatR-identifier.
- `UsingDirectives.cs` geeft daardoor Manual "still uses MediatR (or has inactive #if code)" en `project-file` "still used" → alles-of-niets, 0 van 29 herschreven.

## Gewijzigde bestanden

- `src/Unshackle.Core/UsingDirectives.cs`, `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Core/ProjectPackages.cs`
- `src/Unshackle.Tests/Core/FixerTests.cs`, `src/Unshackle.Tests/Proofs/MediatR/RequestHandler/edge-inactive-if.{before,after}.cs`

## Analyse

Inactieve takken bestaan alleen als `DisabledTextTrivia`. Tokeniseer die tekst (`SyntaxFactory.ParseTokens`) en tel alleen als gebruik als een identifier in `MediatRFix.SwappedTypes` of een MediatR-specifiek type (`IRequestHandler`, `IPipelineBehavior`, `AddMediatR`, ...) voorkomt. Anders mag de `global using` geswapt/verwijderd worden.

## Stappen

1. [x] Unittest: `#if X` met en zonder `ISender` in de inactieve tak (`FixerTests`: met `StringBuilder` bestaand, zonder nieuw) + proof `MediatR/RequestHandler/edge-inactive-if`.
2. [x] Implementatie in `UsingDirectives.cs`: `InactiveCodeMayUse` vervangt `HasInactiveCode`; tokeniseert `DisabledTextTrivia` en zoekt namen van publieke typen, extension methods en de eerste namespace-segmenten van de verwijderde namespaces (uit de compilation, dus generiek voor alle fixes). `KeepsRemovedUsing` kreeg een `Compilation`-parameter.
3. [x] Bewijs: Corpusrun 2026-10-12 op 5f9b366 (`CORPUSRUN-mediatr.md`, sectie Herhaling na TAAK-180..184): jason 27 Fixed, 0 Manual, MediatR weg — maar build na fix faalt (CS0305 in `RequestLoggerTests`: test construeert `LoggingBehaviour<TRequest>` die nu twee typeparameters heeft). Vervolg: TAAK-190.

## Zie ook

- `src/Unshackle.Core/UsingDirectives.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
