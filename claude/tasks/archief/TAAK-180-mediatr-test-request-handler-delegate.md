# TAAK-180 — MediatR: `RequestHandlerDelegate` in testcode blokkeert stil de hele migratie

**Status**: ✅ voltooid — 2026-10-12 (stap 4 corpusbewijs op amantin niet gedraaid, zie Context)
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, run 2026-10-12)
**Doel**: amantinband clean-architecture migreert volledig (build + 85/85 tests), en elk resterend MediatR-gebruik zonder patroon krijgt een eigen Manual met locatie.

## Context voor de volgende agent

- amantin (`582281115489ac69d048e47c7363c7832e5b425a`, MediatR 12.2.0): `tests/CleanArchitecture.Application.UnitTests/Common/Behaviors/{Validation,Authorization}BehaviorTests.cs` mocken `Substitute.For<RequestHandlerDelegate<ErrorOr<T>>>()`, zetten `_next.Invoke().Returns(x)` en roepen `behavior.Handle(cmd, _next, default)` aan.
- Geen patroon vindt dit. Het enige echte Manual is `project-file` "MediatR is still used in this project" op het testproject; daardoor worden alle 27 andere findings cascade-Manual (alles-of-niets), en ziet de gebruiker niet *waar* het gebruik zit.
- `PipelineBehaviorPattern` herschrijft de behavior al naar `MessageHandlerDelegate<TReq,TRes>`; de aanroeper in de test moet mee.

- Gedaan (2026-10-12): `RequestHandlerDelegatePattern` (vóór `PipelineBehaviorPattern` geregistreerd), vangnet `Fixer.UnmatchedPatternId` + `ProjectPackages.RemainingUsesAsync`, proofs `Proofs/MediatR/RequestHandlerDelegate/` + `_Combined`, `CombinedProofTests.CombinedMediatR_*`, NSubstitute in `TestReferences.MediatR/Mediator`, unittests in `FixerProjectFileTests`, patroon-docs en README-rij.
- Let op: MediatR 12.5's delegate heeft een optionele `CancellationToken`; `Invoke(token)` blijft het tweede argument. Lambda's als `next` zijn niet gedekt.

## Analyse

Mediator: `MessageHandlerDelegate<TMessage,TResponse>(TMessage message, CancellationToken ct)` → `ValueTask<TResponse>`. Testcode: type `RequestHandlerDelegate<R>` → `MessageHandlerDelegate<TCmd,R>` (TCmd uit het `Handle`-argument), `next.Invoke()` → `next.Invoke(Arg.Any<TCmd>(), Arg.Any<CancellationToken>())`, `.Returns(x)` → `.Returns(new ValueTask<R>(x))`; `await behavior.Handle(...)` blijft werken. Zonder herleidbare TCmd: Manual met locatie.

## Stappen

1. [x] Vangnet in `ProjectPackages`/`MediatRFix`: elke resterende MediatR-symboolreferentie zonder finding wordt een Manual op die regel (niet alleen "still used" op het csproj).
2. [x] Proof `Proofs/MediatR/RequestHandlerDelegate/` (NSubstitute-mock + `Invoke().Returns`).
3. [x] Patroon `mediatr.request-handler-delegate` + KingmaDoc-docs.
4. [x] Bewijs: nagebootst met `_Combined`-proof; harnas op amantin zelf niet gedraaid (corpusproject niet aanwezig).

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestHandlerDelegatePattern.cs` (nieuw), `MediatRFix.cs`
- `src/Unshackle.Core/Fixer.cs`, `src/Unshackle.Core/ProjectPackages.cs`
- `src/Unshackle.Tests/CombinedProofTests.cs`, `Helpers/TestReferences.cs`, `Core/FixerProjectFileTests.cs`
- `src/Unshackle.Tests/Proofs/MediatR/RequestHandlerDelegate/`, `Proofs/MediatR/_Combined/`
- `docs/projectinfo/patterns/mediatr-request-handler-delegate-{plan,modellen,verify}.md`, `docs/projectinfo/README.md`, `claude/docs/INDEX.md`

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/PipelineBehaviorPattern.cs`
- `src/Unshackle.Core/ProjectPackages.cs`
- `claude/tasks/CORPUSRUN-mediatr.md`
