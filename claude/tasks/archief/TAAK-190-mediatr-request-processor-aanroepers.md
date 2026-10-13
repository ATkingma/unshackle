# TAAK-190 — MediatR: aanroepers van een herschreven request-processor (testcode) breken de build

**Status**: ✅ voltooid — 2026-10-12
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, Herhaling na TAAK-180..184)
**Doel**: jason (v8.0.0) migreert volledig en bouwt groen (39/41 zoals de baseline); een aanroeper die niet herschreven kan worden wordt een Manual, nooit een stille buildbreuk.

## Context voor de volgende agent

- jason (tag v8.0.0, `f2f0445`, MediatR 12.1.1): na `fix --fix mediatr --force` 27 Fixed, 0 Manual, MediatR weg, maar `build1` faalt met 4× CS0305.
- `src/Application/Common/Behaviours/LoggingBehaviour.cs`: `LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>` wordt door `RequestProcessorPattern` `LoggingBehaviour<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse>` (extra typeparameter, `Process` → `protected override ValueTask Handle`).
- `tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs:29,39`: `new LoggingBehaviour<CreateTodoItemCommand>(...)` en `await requestLogger.Process(cmd, new CancellationToken())` worden niet meegenomen.
- Dit breekt de alles-of-niets-belofte: 0 Manual, toch een kapotte build.
- Logs: `<corpus>/corpus-mediatr/_results/r2/jason/` (`build1-errors.txt`, `logs/build1.log`).

## Analyse

- Constructie `X<TReq>` van een herschreven processor → `X<TReq, TRes>`; `TRes` uit `TReq : IRequest<TRes>` (`Unit`-varianten: `IRequest` zonder resultaat → hoe vult de processor `TResponse` in? eerst controleren).
- Aanroep `.Process(req, ct)` → publieke `Handle(req, next, ct)` van `MessagePreProcessor` (pipeline-signatuur) met een `next` die `default` teruggeeft, of Manual als dat gedrag verandert (post-processor heeft een response nodig).
- Niet herleidbaar (geen `IRequest<T>`, open generiek, reflection) → Manual met locatie; nooit stil.

## Keuzes

- Eigen patroon `mediatr.request-processor-caller` (`RequestProcessorCallerPattern`), vóór `mediatr.request-processor` in `MediatRFix.Patterns`: de aanroepers binden dan nog aan de MediatR-klasse. Contracten onveranderd; `ResponseOf` verhuisd naar `MediatRSyntax` (gedeeld).
- `HasOtherUses` (alleen eigen project, `used-elsewhere`) is uit `RequestProcessorPattern` weg: het was de kortsluiting die jason liet glippen (testproject niet doorzocht). Aanroepers worden nu solution-breed gevonden en herschreven of Manual.
- `new P<Cmd>()` en elke andere typeverwijzing: `P<Cmd, TRes>` met `TRes` uit `IRequest<T>` (`IRequest` geeft `Unit`); anders Manual.
- `await p.Process(req, ct);` wordt `await p.Handle(req, static (_, _) => default, ct);` (pre) resp. `Handle(req, (_, _) => new ValueTask<TRes>(res), ct)` (post): de publieke pipeline-`Handle` van `MessagePre/PostProcessor`; de protected override is niet aanroepbaar. Gedrag gelijk: pre draait de eigen Handle en dan `next`, post `next` en dan de eigen Handle.
- Twee findings per aanroep (naam en argumentlijst) om geneste vervangingen (Fixer: overlap) te vermijden.
- Manual: resultaat gebruikt, niet-awaited, mock van `Process`, aanroep via de MediatR-interface, open generiek argument, onbekend response-type.

## Stappen

1. [x] Proof `_Combined/request-processor-callers` (new, var, doeltype-new, Unit, post-processor).
2. [x] Vangnet: Manual-proofs `RequestProcessorCaller/result-used`, `through-interface`, `unknown-response`.
3. [x] Rewrite in `RequestProcessorCallerPattern` + KingmaDoc-docs (plan, modellen, verify, README, INDEX).
4. [ ] Bewijs: harnas op jason niet gedraaid (corpus staat niet in deze omgeving); nagebootst met de `_Combined`-proof.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `claude/tasks/archief/TAAK-182-mediatr-inactieve-if-global-using.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
