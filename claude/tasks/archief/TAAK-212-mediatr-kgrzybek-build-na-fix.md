# TAAK-212 — kgrzybek does not build after `fix --fix mediatr` (MSG0001, SA1210, CS1503, CS0122)

**Status**: ✅ completed (the four error kinds); kgrzybek build still red on a next layer → TAAK-213
**Created**: 2026-10-13 (from TAAK-210, corpus round 4, `claude/tasks/CORPUSRUN-mediatr.md`)
**Finished**: 2026-10-13
**Goal**: kgrzybek `91c8ef2` migrates fully: 0 Manual (achieved), build ok, tests 246/287 → 246/287.

## Context

After TAAK-210 (`_results/r4/kgr`): 217 Fixed, 0 Manual, the build fails on 88× MSG0001, 26× SA1210, 6× CS1503, 4× CS0122.

## Steps

- [x] 1. A repro per error kind (MSG0001: two mini projects in scratch + the decompiled generator; rest: proof/unit test).
- [x] 2. MSG0001: a false report from Mediator 3.0.2 → `WarningsNotAsErrors`.
- [x] 3. SA1210: added usings in a sorted list at their alphabetical place.
- [x] 4. CS1503 and CS0122.
- [x] 5. Proof: harness on kgrzybek (`_results/r5/kgr`): the four error kinds gone; new layer (CS0436/CS0121) → TAAK-213.

## Findings and choices

| Error | Cause | Choice |
| ----- | ----- | ------ |
| MSG0001 (88×) | Mediator 3.0.2 (`CompilationAnalyzer.ProcessInterface`, decompiled) walks the `AllInterfaces` of a handler. For a void handler from **another project** behind an own marker (`ICommandHandler<T> : IRequestHandler<T>`) it counts `IRequestHandler<T>` and `IRequestHandler<T, Unit>` as two handlers: MSG0001 (severity Warning, an error under TWAE). The "message type" it names is the handler itself. The handler is registered all the same (checked in the generated `Mediator.g.cs`). Decorators play no part; no report in the same project. | `MSG0001` next to MSG0003/MSG0005 in the `WarningsNotAsErrors` of `Mediator.SourceGenerator` (`MediatRFix`, `MediatRFixPackageTests`). No Manual: the behaviour is the same. |
| CS1503 (6×) | `RequestHandlerPattern` wrapped `return _decorated.Handle(...)` in `new ValueTask<T>(...)`; after the swap that is already a `ValueTask<T>`. | A return of `Handle(...)` on a (derived) `IRequestHandler<,>` stays as it is. Proof `RequestHandler/decorator-inner-handle`. |
| CS0122 (4×) | The generator names every message it sees, also an internal domain event without a handler (`MeetingGroupProposalRejectedDomainEvent` in Administration.Domain). | `InternalHandlerAccessPattern` also gives projects with only internal messages `InternalsVisibleTo` to the host. Proof `_Combined/internal-message-access`. |
| SA1210 (26×) | The new `using Mediator;` was added at the end. | `UsingDirectives`: after adding, a list that was sorted (System first or not, at least 2 usings) is sorted again; trivia stays per position. 11 existing proofs adjusted (`using System.Linq;` etc. now in their place). |

## Result

- `dotnet build` ok; `dotnet test` 1062/1062 green (+3 `MediatRFixPackageTests`, +2 proofs).
- Corpus kgrzybek (`_results/r5/kgr`): 218 Fixed, 0 Manual, 0× MSG0001/SA1210/CS1503/CS0122; build still red on 410× CS0436 + 2× CS0121 in
  `Registrations.Infrastructure`, which refers to `UserAccess.Infrastructure` (also a generator host) → TAAK-213.

## See also

- `claude/tasks/archief/TAAK-213-mediatr-host-verwijst-naar-host.md`
- `claude/tasks/archief/TAAK-210-mediatr-generieke-decorator-zonder-using.md`
- `claude/tasks/CORPUSRUN-mediatr.md` — "Repeat round 4" and "Repeat round 5"
- `claude/tasks/mediatr-queue.md`
- `docs/projectinfo/patterns/mediatr-request-handler-plan.md`, `docs/projectinfo/patterns/mediatr-internal-handler-access-plan.md`
