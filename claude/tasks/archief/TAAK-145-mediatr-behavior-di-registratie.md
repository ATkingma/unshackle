# TAAK-145 — MediatR: behavior-registratie in DI buiten AddMediatR

**Status**: ✅ voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Queue**: `claude/tasks/mediatr-queue.md` rij 8 (`mediatr.behavior-di-registration`)

## Probleem

`services.AddTransient(typeof(IPipelineBehavior<,>), typeof(X<,>))` (ook `AddScoped`/`AddSingleton`) buiten `AddMediatR`
(jason, ardalis). De vraag: leest Mediator 3 behaviors uit de container, of alleen uit `cfg.PipelineBehaviors`?

## Meting

Testproject `<corpus>/t145/` (MediatR 12.5.0, xUnit): `AddMediatR` met alleen assembly-registratie
plus `services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`; een test stuurt `Ping` en eist dat de
behavior `before`/`after` logt. Kopie `t145-run/` door `unshackle fix --fix mediatr` gehaald (zonder én met dit patroon):

| Variant (na migratie naar Mediator 3.0.2)                | Resultaat                                                |
| -------------------------------------------------------- | -------------------------------------------------------- |
| `AddTransient`, `ServiceLifetime.Transient`              | groen: behavior draait                                   |
| `AddScoped` (uit een scope), `ServiceLifetime.Transient` | green                                                    |
| `AddSingleton`, `ServiceLifetime.Transient`              | green                                                    |
| `AddTransient`, `ServiceLifetime.Singleton` (2× Send)    | behavior draait, maar **één instantie** voor beide calls |

Conclusie: de gegenereerde Mediator haalt elke `IPipelineBehavior<,>` uit de container. Er is niets te herschrijven;
`MediatRFix.SwappedTypes` bevat `MediatR.IPipelineBehavior\`2`al, dus de using-swap maakt er`Mediator.IPipelineBehavior` van.

## Keuzes

- **Pattern dat de node ongewijzigd `Fixed` teruggeeft**: de registratie verschijnt zo in het rapport (de gebruiker ziet
  dat ze bekeken en goed bevonden is) en heeft proofs. Geen herschrijving: die is niet nodig (meting).
- **Alleen de `typeof`-vorm met twee argumenten** van `Microsoft.Extensions.DependencyInjection`: dat is wat de queue noemt.
  De generieke vorm `AddTransient<IPipelineBehavior<A,B>, X>()` en factory-overloads werken via dezelfde swap, maar
  verschijnen niet als finding.
- **Geen waarschuwing voor `ServiceLifetime.Singleton`**: dat verschil (Mediator cachet de behavior-instantie) ontstaat alleen
  als de gebruiker in `AddMediatR` zelf `Lifetime = Singleton` zette; `AddMediatRPattern` maakt standaard `Transient`. Dat
  zou analyse over statements/bestanden heen vragen; vastgelegd als risico in het plan.

## Stappen

- [x] Meting in t145 (tabel hierboven)
- [x] `src/Unshackle.Core/Fixes/MediatR/Patterns/BehaviorDiRegistrationPattern.cs` + regel in `MediatRFix.Patterns`
- [x] Proofs `src/Unshackle.Tests/Proofs/MediatR/BehaviorDiRegistration/` (`transient`, `lifetimes`)
- [x] Unittests `src/Unshackle.Tests/Core/BehaviorDiRegistrationPatternTests.cs`
- [x] Plan, modellen, verify, README-rij
- [x] End-to-end: t145-run met dit patroon gemigreerd, gebouwd, test groen

## Zie ook

- `docs/projectinfo/patterns/mediatr-behavior-di-registration-plan.md`
- `docs/projectinfo/patterns/mediatr-behavior-di-registration-verify.md`
- `claude/tasks/mediatr-queue.md`
