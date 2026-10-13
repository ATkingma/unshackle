# TAAK-192 — Generieke notification automatisch migreren (nadirbad)

**Status**: ✅ voltooid (2026-10-12)
**Aangemaakt**: 2026-10-12
**Doel**: `DomainEventNotification<T> : INotification` (nadirbad) migreert zonder Manual naar Mediator 3.0.2 met hetzelfde runtimegedrag.

## Context voor de volgende agent

- Corpus: `claude/tasks/CORPUSRUN-mediatr.md` (Herhaling na TAAK-180..184): nadirbad houdt 1 root-Manual, `mediatr.unsupported` op `src/Application/Common/Models/DomainEventNotification.cs`.
- nadirbad (`feb3a15`): `public class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : DomainEvent`,
  handler `INotificationHandler<DomainEventNotification<AppointmentBookedEvent>>`, en `DomainEventService` maakt de notification met
  `Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)`.
- StyleCop + `TreatWarningsAsErrors` in nadirbad: gegenereerde code moet StyleCop-netjes zijn (ctor-initializer op eigen regel, SA1128).
- Andere agents werken tegelijk in dezelfde fix (TAAK-190..196); `MediatRFix.Patterns` alleen met een regel uitbreiden.

## Analyse

### Huidige situatie

`UnsupportedPattern` geeft Manual op elke niet-abstracte generieke `INotification`; door `AllOrNothing` blijft de hele solution MediatR.

### Probleem

Probe (Mediator 3.0.2): een generieke concrete notification laat de generator `T` in `Mediator.g.cs` schrijven → CS0246, build faalt.

### Nieuwe situatie

Probe bewijst: maak `G<T>` **abstract** (generator slaat abstracte typen over, TAAK-132) en voeg per gesloten type `X` een
concrete `GOfX : G<X>` toe. Een handler van `INotificationHandler<G<X>>` krijgt `GOfX` binnen (polymorfe dispatch van Mediator 3),
ook als het object via `Activator` gemaakt is en als `INotification` gepubliceerd wordt (probe: 3/3 aangeroepen).

### Verbetering

nadirbad kan volledig migreren; 1 corpus-project meer op 100%.

## Aanpak

### Wat

Nieuw `ISolutionPattern` `mediatr.generic-notification` (`GenericNotificationPattern`), vóór `UnsupportedPattern` in `MediatRFix.Patterns`.

### Waar

- `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs`, `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` (één regel)
- proofs `src/Unshackle.Tests/Proofs/MediatR/GenericNotification/` en `_Combined/generic-notification-nadirbad.*`

### Hoe

1. Declaratie `G<T>`: `abstract` erbij (`sealed` eraf), daarna per gesloten type `X` een `public class GOfX : G<X>` met dezelfde ctors (`: base(...)`),
   en alleen als ergens `typeof(G<>).MakeGenericType(x)` staat een `static class GTypes { Type Close(Type) }` met een `if`-keten.
2. Gesloten typen = elke `G<X>` in de solution (handlers, `new`, variabelen) ∪ (bij `MakeGenericType`) elk concreet type in de solution dat aan de constraint van `T` voldoet.
3. Aanroepplekken: `new G<X>(a)` en `new(a)` met doeltype `G<X>` → `new GOfX(a)`; `typeof(G<>).MakeGenericType(x)` → `GTypes.Close(x)`.

### Waarom deze aanpak

Een niet-generieke adapter (één `DomainEventNotification` met `DomainEvent`-property) verandert elke handler (`notification.DomainEvent` wordt het basistype) en de dispatch (alle handlers krijgen alles).
Afgeleide klassen houden alle bestaande code (handlers, `notification.DomainEvent`, variabelen van type `G<X>`) ongewijzigd geldig.

## Keuzes

| Keuze | Reden |
| ----- | ----- |
| `G<T>` abstract, niet hernoemd | Handlers en variabelen blijven `G<X>` gebruiken; Mediator dispatcht polymorf (probe). |
| Naam `{G}Of{X}`, `{G}Types.Close` | Voorspelbaar; botsing met bestaand type → Manual. |
| Ctors van `G` ongewijzigd (ook `public`) | Kleinste wijziging; CA1012 staat niet in de standaard-analyzers. |
| Typen met de constraint ook zonder handler | MediatR publiceert dan stil niets; met een `GOfX` doet Mediator hetzelfde (MSG0005 is al geen error, TAAK-136). Zonder dat type zou `Close` gooien. |
| `Close` van een onbekend type gooit `ArgumentException` | Alleen mogelijk voor typen buiten de solution; niet te maken zonder runtime-codegeneratie. |
| Manual: ariteit ≠ 1, partial, genest, struct, bestaande subklassen, type-parameter als argument bij `new`, `typeof(G<>)` zonder `MakeGenericType`, `MakeGenericType` zonder type-constraint, gesloten type onzichtbaar vanuit het project van `G` | Geen eenduidige exacte vertaling; komt in het corpus niet voor. |

## Stappen

- [x] 1. Probe Mediator 3.0.2 (generiek → CS0246; abstract + subklassen → werkt, 3/3) in `<probe-dir>/probe-192`
- [x] 2. Pattern + registratie
- [x] 3. Proofs (los + `_Combined` nadirbad) en unittests
- [x] 4. Docs plan/modellen/verify, README-rij, INDEX, proofs-doc
- [x] 5. Build + tests; nadirbad-corpus opnieuw met de fix
- [x] 6. Afronden, archief, wachtrij

## Risico's & randgevallen

| Risico | Mitigatie |
| ------ | --------- |
| `GetType() == typeof(G<X>)` in gebruikerscode wordt false | Geen voorkomen in corpus; gedocumenteerd verschil. |
| Projectgrens: `X` in een project dat `G`'s project niet ziet | Manual met naam van het type. |

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs` (nieuw, `ISolutionPattern`)
- `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` (één regel: vóór `UnsupportedPattern`)
- `src/Unshackle.Tests/Core/GenericNotificationPatternTests.cs` (4 tests, twee projecten)
- `src/Unshackle.Tests/Proofs/MediatR/GenericNotification/` (`make-generic-type`, `new-closed`, `last-in-file`, `generic-code`, `no-constraint`)
- `src/Unshackle.Tests/Proofs/MediatR/_Combined/generic-notification-nadirbad.*`
- `docs/projectinfo/patterns/mediatr-generic-notification-{plan,modellen,verify}.md`, README-rij, `claude/docs/INDEX.md`, `docs/projectinfo/architectuur/proofs.md`

## Resultaat

Corpus nadirbad (`feb3a15`, worktree `ac683dd` + dit patroon): 18 Fixed, 0 Manual, MediatR weg, build 0 errors (1× MSG0005-warning voor
`DomainEventNotificationOfAppointmentCompletedEvent`, geen handler — zoals MediatR), tests 30/30 → 30/30. Eerste run faalde op SA1513
(geen lege regel na het laatste type zonder slot-regeleinde) → opgelost, proof `last-in-file`. Contracten ongewijzigd (bestaande `ISolutionPattern`, `FixedResult.Warning`).

## Zie ook

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/mediatr-queue.md`
