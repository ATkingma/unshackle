# TAAK-180 — MediatR: `RequestHandlerDelegate` in test code silently blocks the whole migration

**Status**: ✅ completed — 2026-10-12 (step 4 corpus proof on amantin not run, see Context)
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, run 2026-10-12)
**Goal**: amantinband clean-architecture migrates fully (build + 85/85 tests), and every remaining MediatR usage without a pattern gets its own Manual with a location.

## Context for the next agent

- amantin (`582281115489ac69d048e47c7363c7832e5b425a`, MediatR 12.2.0): `tests/CleanArchitecture.Application.UnitTests/Common/Behaviors/{Validation,Authorization}BehaviorTests.cs` mock `Substitute.For<RequestHandlerDelegate<ErrorOr<T>>>()`, set `_next.Invoke().Returns(x)` and call `behavior.Handle(cmd, _next, default)`.
- No pattern finds this. The only real Manual is `project-file` "MediatR is still used in this project" on the test project; because of that all 27 other findings become cascade Manuals (all-or-nothing), and the user does not see *where* the usage is.
- `PipelineBehaviorPattern` already rewrites the behavior to `MessageHandlerDelegate<TReq,TRes>`; the caller in the test must come along.

- Done (2026-10-12): `RequestHandlerDelegatePattern` (registered before `PipelineBehaviorPattern`), safety net `Fixer.UnmatchedPatternId` + `ProjectPackages.RemainingUsesAsync`, proofs `Proofs/MediatR/RequestHandlerDelegate/` + `_Combined`, `CombinedProofTests.CombinedMediatR_*`, NSubstitute in `TestReferences.MediatR/Mediator`, unit tests in `FixerProjectFileTests`, pattern docs and README row.
- Note: MediatR 12.5's delegate has an optional `CancellationToken`; `Invoke(token)` stays the second argument. Lambdas as `next` are not covered.

## Analysis

Mediator: `MessageHandlerDelegate<TMessage,TResponse>(TMessage message, CancellationToken ct)` → `ValueTask<TResponse>`. Test code: type `RequestHandlerDelegate<R>` → `MessageHandlerDelegate<TCmd,R>` (TCmd from the `Handle` argument), `next.Invoke()` → `next.Invoke(Arg.Any<TCmd>(), Arg.Any<CancellationToken>())`, `.Returns(x)` → `.Returns(new ValueTask<R>(x))`; `await behavior.Handle(...)` keeps working. Without a derivable TCmd: Manual with a location.

## Steps

1. [x] Safety net in `ProjectPackages`/`MediatRFix`: every remaining MediatR symbol reference without a finding becomes a Manual on that line (not only "still used" on the csproj).
2. [x] Proof `Proofs/MediatR/RequestHandlerDelegate/` (NSubstitute mock + `Invoke().Returns`).
3. [x] Pattern `mediatr.request-handler-delegate` + KingmaDoc docs.
4. [x] Proof: imitated with a `_Combined` proof. Corpus run 2026-10-12 on 5f9b366 (`CORPUSRUN-mediatr.md`, section Repeat after TAAK-180..184): `ValidationBehaviorTests` covered, but `AuthorizationBehaviorTests` shares one `_mockNextBehavior` field across three request types → now a Manual with a location (the safety net works), amantin not yet migrated (0 Fixed, 85/85, 0 regressions). Follow-up: TAAK-191.

## Changed files

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs` (new), `MediatRFix.cs`
- `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Core/ProjectPackages.cs`
- `src/Unshackle.Tests/CombinedProofTests.cs`, `Helpers/TestReferences.cs`, `Core/FixerProjectFileTests.cs`
- `src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/`, `Proofs/MediatR/_Combined/`
- `docs/projectinfo/patterns/mediatr-request-handler-delegate-{plan,modellen,verify}.md`, `docs/projectinfo/README.md`, `claude/docs/INDEX.md`

## See also

- `src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs`
- `src/Unshackle.Core/ProjectPackages.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
