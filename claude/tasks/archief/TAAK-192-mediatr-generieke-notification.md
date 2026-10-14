# TAAK-192 — Migrate a generic notification automatically (nadirbad)

**Status**: ✅ completed (2026-10-12)
**Created**: 2026-10-12
**Goal**: `DomainEventNotification<T> : INotification` (nadirbad) migrates without Manual to Mediator 3.0.2 with the same runtime behaviour.

## Context for the next agent

- Corpus: `claude/tasks/CORPUSRUN-mediatr.md` (Repeat after TAAK-180..184): nadirbad keeps 1 root Manual, `mediatr.unsupported` on `src/Application/Common/Models/DomainEventNotification.cs`.
- nadirbad (`feb3a15`): `public class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : DomainEvent`,
  handler `INotificationHandler<DomainEventNotification<AppointmentBookedEvent>>`, and `DomainEventService` creates the notification with
  `Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)`.
- StyleCop + `TreatWarningsAsErrors` in nadirbad: generated code must be StyleCop-clean (ctor initializer on its own line, SA1128).
- Other agents work on the same fix at the same time (TAAK-190..196); only extend `MediatRFix.Patterns` by one line.

## Analysis

### Current situation

`UnsupportedPattern` gives Manual on every non-abstract generic `INotification`; through `AllOrNothing` the whole solution stays MediatR.

### Problem

Probe (Mediator 3.0.2): a generic concrete notification makes the generator write `T` in `Mediator.g.cs` → CS0246, the build fails.

### New situation

The probe proves: make `G<T>` **abstract** (the generator skips abstract types, TAAK-132) and add per closed type `X` a
concrete `GOfX : G<X>`. A handler of `INotificationHandler<G<X>>` receives `GOfX` (polymorphic dispatch of Mediator 3),
also when the object was created via `Activator` and is published as `INotification` (probe: 3/3 called).

### Improvement

nadirbad can migrate fully; 1 more corpus project at 100%.

## Approach

### What

New `ISolutionPattern` `mediatr.generic-notification` (`GenericNotificationPattern`), before `UnsupportedPattern` in `MediatRFix.Patterns`.

### Where

- `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs`, `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` (one line)
- proofs `src/Unshackle.Tests/Proofs/MediatR/GenericNotification/` and `_Combined/generic-notification-nadirbad.*`

### How

1. Declaration `G<T>`: add `abstract` (remove `sealed`), then per closed type `X` a `public class GOfX : G<X>` with the same ctors (`: base(...)`),
   and only if `typeof(G<>).MakeGenericType(x)` occurs somewhere a `static class GTypes { Type Close(Type) }` with an `if` chain.
2. Closed types = every `G<X>` in the solution (handlers, `new`, variables) ∪ (with `MakeGenericType`) every concrete type in the solution that satisfies the constraint of `T`.
3. Call sites: `new G<X>(a)` and `new(a)` with target type `G<X>` → `new GOfX(a)`; `typeof(G<>).MakeGenericType(x)` → `GTypes.Close(x)`.

### Why this approach

A non-generic adapter (one `DomainEventNotification` with a `DomainEvent` property) changes every handler (`notification.DomainEvent` becomes the base type) and the dispatch (all handlers get everything).
Derived classes keep all existing code (handlers, `notification.DomainEvent`, variables of type `G<X>`) valid unchanged.

## Choices

| Choice | Reason |
| ------ | ------ |
| `G<T>` abstract, not renamed | Handlers and variables keep using `G<X>`; Mediator dispatches polymorphically (probe). |
| Name `{G}Of{X}`, `{G}Types.Close` | Predictable; clash with an existing type → Manual. |
| Ctors of `G` unchanged (also `public`) | Smallest change; CA1012 is not in the default analyzers. |
| Types with the constraint also without a handler | MediatR then silently publishes nothing; with a `GOfX` Mediator does the same (MSG0005 is no longer an error, TAAK-136). Without that type `Close` would throw. |
| `Close` of an unknown type throws `ArgumentException` | Only possible for types outside the solution; cannot be created without runtime code generation. |
| Manual: arity ≠ 1, partial, nested, struct, existing subclasses, type parameter as argument of `new`, `typeof(G<>)` without `MakeGenericType`, `MakeGenericType` without a type constraint, closed type invisible from `G`'s project | No unambiguous exact translation; does not occur in the corpus. |

## Steps

- [x] 1. Probe Mediator 3.0.2 (generic → CS0246; abstract + subclasses → works, 3/3) in `<probe-dir>/probe-192`
- [x] 2. Pattern + registration
- [x] 3. Proofs (separate + `_Combined` nadirbad) and unit tests
- [x] 4. Docs plan/models/verify, README row, INDEX, proofs-doc
- [x] 5. Build + tests; nadirbad corpus again with the fix
- [x] 6. Wrap up, archive, queue

## Risks & edge cases

| Risk | Mitigation |
| ---- | ---------- |
| `GetType() == typeof(G<X>)` in user code becomes false | No occurrence in the corpus; documented difference. |
| Project boundary: `X` in a project that does not see `G`'s project | Manual with the name of the type. |

## Changed files

- `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericNotificationPattern.cs` (new, `ISolutionPattern`)
- `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` (one line: before `UnsupportedPattern`)
- `src/Unshackle.Tests/Core/GenericNotificationPatternTests.cs` (4 tests, two projects)
- `src/Unshackle.Tests/Proofs/MediatR/GenericNotification/` (`make-generic-type`, `new-closed`, `last-in-file`, `generic-code`, `no-constraint`)
- `src/Unshackle.Tests/Proofs/MediatR/_Combined/generic-notification-nadirbad.*`
- `docs/projectinfo/patterns/mediatr-generic-notification-{plan,modellen,verify}.md`, README row, `claude/docs/INDEX.md`, `docs/projectinfo/architectuur/proofs.md`

## Result

Corpus nadirbad (`feb3a15`, worktree `ac683dd` + this pattern): 18 Fixed, 0 Manual, MediatR gone, build 0 errors (1× MSG0005 warning for
`DomainEventNotificationOfAppointmentCompletedEvent`, no handler — like MediatR), tests 30/30 → 30/30. The first run failed on SA1513
(no blank line after the last type without a final line ending) → solved, proof `last-in-file`. Contracts unchanged (existing `ISolutionPattern`, `FixedResult.Warning`).

## See also

- `claude/docs/INDEX.md`
- `docs/projectinfo/README.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
- `claude/tasks/mediatr-queue.md`
