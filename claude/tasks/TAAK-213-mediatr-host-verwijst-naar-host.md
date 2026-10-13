# TAAK-213 — MediatR: generator-host verwijst naar een andere host (kgrzybek CS0436/CS0121)

**Status**: open
**Aangemaakt**: 2026-10-13 (uit TAAK-212, corpus-ronde 5)
**Doel**: kgrzybek `91c8ef2` bouwt na `fix --fix mediatr`.

## Context voor de volgende agent

- `_results/r5/kgr/logs/build1.log`: 410× CS0436 (`RequestHandlerWrapper`, `NotificationHandlerWrapper`, `ContainerMetadata`, `Mediator`, ...)
  en 2× CS0121 (`AddMediator` dubbelzinnig), alleen in `Registrations.Infrastructure`.
- `Registrations.Infrastructure` verwijst naar `UserAccess.Infrastructure`; beide krijgen `Mediator.SourceGenerator` (Autofac `MediatorModule`).
  De publieke gegenereerde `AddMediator`-extensie van UserAccess botst (CS0121); `UserAccess.Infrastructure` geeft Registrations
  `InternalsVisibleTo` (internal-handler-access), waardoor ook de internal gegenereerde typen botsen (CS0436).
- De generator (3.0.2) controleert geen toegankelijkheid: zonder IVT wordt het CS0122 voor de internal handlers van UserAccess.Infrastructure.
- In ronde 4 verborgen: UserAccess.Infrastructure brak toen al op MSG0001, dus Registrations werd niet gecompileerd.

## Stappen

- [ ] 1. Repro: twee host-projecten waarvan de ene de andere refereert (`_Combined` met `@project`).
- [ ] 2. Opties onderzoeken: `MediatorOptions.Assemblies` beperken, `GenerateTypesAsInternal`/namespace per host, of Manual.
- [ ] 3. Bewijs op kgrzybek met het harnas (`_results/r5/harness.sh`).

## Zie ook

- `claude/tasks/archief/TAAK-212-mediatr-kgrzybek-build-na-fix.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Herhaling ronde 5"
- `claude/tasks/mediatr-queue.md`
