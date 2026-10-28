# TAAK-344 — `mediatr.generic-request` closes type arguments via callers (council round 17 M15)

**Status**: done (2026-10-27): count 2 < 3, only step 2 done
**Created**: 2026-10-27
**Goal**: a request created with open type arguments in generic code becomes Fixed when every caller closes it, like
`automapper.generic-map`, guarded by a statically found handler registration.

## Context for the next agent

- Decision: `claude/tasks/COUNCIL-round-17.md` M15. Today the pattern uses `GenericRequestUses` only:
  `src/Unshackle.Core/Fixes/MediatR/Patterns/GenericRequestPattern.cs`, `GenericRequestUses.cs`.
- Shared closure: grep `TypeArgumentClosure` (users: `automapper.generic-map`, `-generic-destination`, `-generic-profile`); depth 3,
  follows reduced extension calls (council A17).
- Current Manual proof: `src/Unshackle.Tests/Proofs/MediatR/GenericRequest/extension-callers.*`.

## Steps

- [x] 1. Count open generic requests: the existing proofs plus the MediatR corpus repos (`claude/tasks/CORPUSRUN-mediatr.md`).
  Fewer than 3 → do only step 2, then archive with the count.
- [x] 2. Manual reason text: "open type argument in generic code; callers not followed" (pin in a test).
- [ ] 3. Use `TypeArgumentClosure` in `GenericRequestPattern`; an open set stays Manual.
- [ ] 4. Handler guard: each closed request is Fixed only when a handler registration for it (closed handler type, or explicit
  open-generic registration) is found statically in the solution; a handler that only an assembly-scan call
  (`RegisterServicesFromAssembly`, Scrutor scan) would find → Manual with its own reason code.
- [ ] 5. Proofs: `extension-callers` becomes an after proof (with static registration); new manual proofs for an open set and for
  scan-only registration. Update the `mediatr-generic-request` plan/verify in `docs/projectinfo/patterns/`.
- [ ] 6. `dotnet build`, `dotnet test`; archive.

## Result

- Count: 2 open generic requests, both proofs (`GenericRequest/open-creation`, `extension-callers`). MediatR corpus
  (`D:/playground/corpus-mediatr/src`, grep for generic classes implementing `IRequest<`): only eShop `IdentifiedCommand<T, R>`,
  always created closed (`OrdersApi.cs`, tests); kgrzybek `CommandBase<TResult>`/`QueryBase<TResult>` are abstract (not candidates).
  Total 2 < 3.
- Step 2: the reason is now "`G`: open type argument in generic code; callers not followed (...)", pinned by
  `GenericRequestPatternTests.RewriteAsync_OpenCreationInGenericCode_ManualCallersNotFollowed` and both manual proofs.
- Steps 3-6 not done (priority threshold not met); reopen when a corpus shows >= 3.

## Council questions

- None.

## See also

- `claude/tasks/COUNCIL-round-17.md`
- `claude/tasks/archief/TAAK-338-automapper-closure-regressions.md`
