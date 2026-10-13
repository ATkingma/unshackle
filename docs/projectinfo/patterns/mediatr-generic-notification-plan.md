---
kingmadoc: 1
feature: "mediatr-generic-notification"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6]
files_expected:
    - src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs
    - src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs
    - src/Unshackle.Tests/Proofs/MediatR/GenericNotification/
    - src/Unshackle.Tests/Proofs/MediatR/_Combined/generic-notification-nadirbad.before.cs
---

# Feature: mediatr.generic-notification: generieke notification → abstracte basis + concrete subklasse per gesloten type

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-12 (met de hand) |

> Opgesteld uit TAAK-192 (corpus nadirbad).

## One-sentence summary

`class G<T> : INotification` wordt `abstract`, krijgt per gesloten type `X` uit de solution een `class GOfX : G<X>` met dezelfde constructors, en `new G<X>(...)` / `typeof(G<>).MakeGenericType(x)` worden `new GOfX(...)` / `GTypes.Close(x)`.

## Scope (in / out)

**In**: niet-abstracte, top-level generieke klasse (of record class) met één typeparameter die MediatR `INotification` implementeert en geen request is.
**Out (Manual)**: partial of file-local, bestaande subklassen, `new G<TParam>` in generieke code, `typeof(G<>)` anders dan in `MakeGenericType(x)`, `MakeGenericType` zonder class/interface-constraint, gesloten type niet zichtbaar vanuit het project van `G`, naambotsing, alleen private constructors. Ariteit > 1 blijft bij `mediatr.unsupported`.

## Requirements

- **REQ-1**: WHEN `typeof(G<>).MakeGenericType(x)` in de solution staat en `T : C` THE SYSTEM SHALL voor elk concreet niet-generiek type met `C` in de solution een subklasse schrijven en `GTypes.Close(x)` met een `if`-keten (proof `make-generic-type`).
- **REQ-2**: WHEN `new G<X>(...)` of `G<X> v = new(...)` staat THE SYSTEM SHALL `new GOfX(...)` schrijven; parameters met standaardwaarde gaan mee (proof `new-closed`, ook `sealed` eraf).
- **REQ-3**: WHEN `G` het laatste type in een bestand zonder slot-regeleinde is THE SYSTEM SHALL de nieuwe typen met een lege regel scheiden (SA1513, proof `last-in-file`).
- **REQ-4**: WHEN de solution het nadirbad-geval bevat (handler `INotificationHandler<G<X>>`, `Activator.CreateInstance(typeof(G<>).MakeGenericType(...))`) THE SYSTEM SHALL het met de hele fix zonder Manual omzetten en compileert het tegen Mediator 3.0.2 mét generator (proof `_Combined/generic-notification-nadirbad`).
- **REQ-5**: IF een gesloten type in een ander project staat dan `G` kan zien, of `G` al een subklasse heeft THEN Manual (unittests `GenericNotificationPatternTests`).
- **REQ-6**: IF `new G<TParam>(...)` in generieke code staat, of `MakeGenericType` zonder constraint THEN Manual (proofs `generic-code`, `no-constraint`).

## Assumptions

- Mediator 3.0.2 (probe `<probe-dir>/probe-192`): een generieke concrete notification geeft CS0246 in `Mediator.g.cs`; abstracte typen slaat de generator over (TAAK-132); een handler van `G<X>` krijgt `GOfX` via polymorfe dispatch, ook als `INotification` gepubliceerd na `Activator.CreateInstance` (3/3 aangeroepen).
- MSG0005 (subklasse zonder handler) is al geen error (TAAK-136); MediatR publiceert zonder handler ook stil niets.
- Gedragsverschil: `GetType()` is nu `GOfX`; de finding draagt een `Warning`.

## Modellen

[mediatr-generic-notification-modellen.md](mediatr-generic-notification-modellen.md).

## Zie ook

- [mediatr-generic-notification-verify.md](mediatr-generic-notification-verify.md)
- `claude/tasks/archief/TAAK-192-mediatr-generieke-notification.md`
