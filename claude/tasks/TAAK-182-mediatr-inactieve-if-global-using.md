# TAAK-182 — MediatR: inactieve `#if`-code zonder MediatR houdt `global using MediatR` vast

**Status**: stap 1/3 — nog te starten
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`; actie 10 uit de run van 2026-10-11)
**Doel**: jasontaylor CleanArchitecture v8.0.0 migreert volledig.

## Context voor de volgende agent

- jason (`f2f0445721f5e3efe98ea22266148c698b3ef288`, tag v8.0.0, MediatR 12.1.1): `src/Web/GlobalUsings.cs:3 global using MediatR;`. `Web` heeft `#if (UseApiOnly)`-blokken (dotnet-new-template) in `Program.cs`, `DependencyInjection.cs`, `Endpoints/Users.cs`; geen ervan noemt een MediatR-identifier.
- `UsingDirectives.cs` geeft daardoor Manual "still uses MediatR (or has inactive #if code)" en `project-file` "still used" → alles-of-niets, 0 van 29 herschreven.

## Analyse

Inactieve takken bestaan alleen als `DisabledTextTrivia`. Tokeniseer die tekst (`SyntaxFactory.ParseTokens`) en tel alleen als gebruik als een identifier in `MediatRFix.SwappedTypes` of een MediatR-specifiek type (`IRequestHandler`, `IPipelineBehavior`, `AddMediatR`, ...) voorkomt. Anders mag de `global using` geswapt/verwijderd worden.

## Stappen

1. [ ] Unittest: `#if X` met en zonder `ISender` in de inactieve tak.
2. [ ] Implementatie in `UsingDirectives.cs`.
3. [ ] Bewijs: harnas op jason → Fixed, build groen, 39/41 (2 rood al in baseline).

## Zie ook

- `src/Unshackle.Core/UsingDirectives.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
