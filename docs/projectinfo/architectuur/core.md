# Core: Fixer → Fix → Pattern

```
Fixer                       de ene ingang: FixAsync(solution, request)
 └─ IFix                    één per bibliotheek (FluentAssertions, MediatR, AutoMapper)
     └─ IPattern            één soort code vinden + herschrijven
          ├─ Find(...)      → plekken in de code
          └─ Rewrite(...)   → Fixed(nieuwe node) of Manual(reden)
```
```
src/Unshackle.Core/
├─ Fixer.cs
├─ Abstractions/   IFix, IPattern, IFixer, ISolutionStore, IReportWriter
├─ Models/         Finding, PatternResult, FixRequest, FixReport
└─ Fixes/
   ├─ FluentAssertions/   FluentAssertionsFix.cs + Patterns/
   ├─ MediatR/            MediatRFix.cs + Patterns/
   └─ AutoMapper/         AutoMapperFix.cs + Patterns/
```
Elke fix heeft dezelfde indeling. Ken je er één, dan ken je ze allemaal.

**Afwijkende patronen** (bijvoorbeeld `HaveCount`, waar het type bepaalt wat de juiste
vervanging is) regel je binnen dat pattern. Wordt het te groot, dan splits je het in meerdere
patterns, zoals `HaveCountOnListPattern` en `HaveCountOnEnumerablePattern`. De Fixer merkt
daar niks van.

**Een pattern crasht nooit.** Lukt een herschrijving niet, dan geeft het `Manual(reden)`
terug en komt dat in het rapport.

**Dry-run kost niks extra.** Roslyn past een `Solution` nooit aan. De Fixer berekent een
nieuwe `Solution` en het rapport. Bij `--dry-run` tonen we alleen de diff, anders schrijft
`ISolutionStore` de wijzigingen weg.

## Zoals gebouwd (TAAK-001)

- `IPattern`: `Id`, `Description`, `Find(SyntaxNode root, SemanticModel model)` → nodes,
  `Rewrite(SyntaxNode node, SemanticModel model)` → `PatternResult`. Het pattern kiest zelf welke node het vervangt
  (expressie, buitenste aanroep van een keten, of een heel statement).
- `PatternResult.Fixed(node(s), params requiredNamespaces)` of `PatternResult.Manual(reason)`.
- `IFix`: `Id`, `Patterns`, `RemovedNamespaces`, `AddedNamespaces`.
- `Fixer`: per document de patterns na elkaar (vers `SemanticModel`), vervangen met `TrackNodes`/`ReplaceNode`
  (buitenste eerst, overlap → Manual), daarna usings via `UsingDirectives`. Exceptions per pattern → Manual.
- Details en redenen: `claude/tasks/archief/TAAK-001-fundament.md`.

## SOLID in het kort

| Principe | Hoe |
|---|---|
| S | Een pattern beschrijft alleen wat er herschreven wordt. Laden, schrijven en rapporteren gebeurt elders |
| O | Een nieuwe bibliotheek of nieuw patroon is een nieuwe klasse. De Fixer verandert niet |
| L | Elk pattern geeft `Fixed` of `Manual` terug en gooit nooit een exception, dus de Fixer behandelt ze allemaal hetzelfde |
| I | Kleine interfaces: zoeken/herschrijven, solution-opslag en rapport-schrijven zijn apart |
| D | Core hangt af van interfaces. MSBuild, het bestandssysteem en CSV zitten in DataAccess |

## Zie ook

- `docs/projectinfo/README.md` — overzicht en C4-diagrammen
- `claude/docs/design-patterns.md` — GoF-patronen in deze opzet
- `src/Unshackle.Core/Fixer.cs` — implementatie
