# TAAK-190 — MediatR: aanroepers van een herschreven request-processor (testcode) breken de build

**Status**: open
**Aangemaakt**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, Herhaling na TAAK-180..184)
**Doel**: jason (v8.0.0) migreert volledig en bouwt groen (39/41 zoals de baseline); een aanroeper die niet herschreven kan worden wordt een Manual, nooit een stille buildbreuk.

## Context voor de volgende agent

- jason (tag v8.0.0, `f2f0445`, MediatR 12.1.1): na `fix --fix mediatr --force` 27 Fixed, 0 Manual, MediatR weg, maar `build1` faalt met 4× CS0305.
- `src/Application/Common/Behaviours/LoggingBehaviour.cs`: `LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>` wordt door `RequestProcessorPattern` `LoggingBehaviour<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse>` (extra typeparameter, `Process` → `protected override ValueTask Handle`).
- `tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs:29,39`: `new LoggingBehaviour<CreateTodoItemCommand>(...)` en `await requestLogger.Process(cmd, new CancellationToken())` worden niet meegenomen.
- Dit breekt de alles-of-niets-belofte: 0 Manual, toch een kapotte build.
- Logs: `D:\playground\corpus-mediatr\_results\r2\jason\` (`build1-errors.txt`, `logs/build1.log`).

## Analyse

- Constructie `X<TReq>` van een herschreven processor → `X<TReq, TRes>`; `TRes` uit `TReq : IRequest<TRes>` (`Unit`-varianten: `IRequest` zonder resultaat → hoe vult de processor `TResponse` in? eerst controleren).
- Aanroep `.Process(req, ct)` → publieke `Handle(req, next, ct)` van `MessagePreProcessor` (pipeline-signatuur) met een `next` die `default` teruggeeft, of Manual als dat gedrag verandert (post-processor heeft een response nodig).
- Niet herleidbaar (geen `IRequest<T>`, open generiek, reflection) → Manual met locatie; nooit stil.

## Stappen

1. [ ] Proof `Proofs/MediatR/RequestProcessor/` met test die `new P<Cmd>(...)` + `.Process(cmd, ct)` doet (before compileert tegen MediatR 12, after tegen Mediator).
2. [ ] Vangnet: verwijzing naar een herschreven processor-type met het oude aantal typeargumenten zonder rewrite → Manual (unittest).
3. [ ] Rewrite in `RequestProcessorPattern` (of eigen patroon) + KingmaDoc-docs bijwerken.
4. [ ] Bewijs: harnas op jason → build groen, 39/41, 0 regressies.

## Zie ook

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `claude/tasks/archief/TAAK-182-mediatr-inactieve-if-global-using.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
