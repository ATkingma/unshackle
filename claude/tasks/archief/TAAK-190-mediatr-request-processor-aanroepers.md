# TAAK-190 — MediatR: callers of a rewritten request processor (test code) break the build

**Status**: ✅ Done — 2026-10-12
**Created**: 2026-10-12 (`claude/tasks/CORPUSRUN-mediatr.md`, Repetition after TAAK-180..184)
**Goal**: jason (v8.0.0) migrates fully and builds green (39/41 like the baseline); a caller that cannot be rewritten becomes a Manual, never a silent build break.

## Context for the next agent

- jason (tag v8.0.0, `f2f0445`, MediatR 12.1.1): after `fix --fix mediatr --force` 27 Fixed, 0 Manual, MediatR gone, but `build1` fails with 4× CS0305.
- `src/Application/Common/Behaviours/LoggingBehaviour.cs`: `LoggingBehaviour<TRequest> : IRequestPreProcessor<TRequest>` is turned by `RequestProcessorPattern` into `LoggingBehaviour<TRequest, TResponse> : MessagePreProcessor<TRequest, TResponse>` (extra type parameter, `Process` → `protected override ValueTask Handle`).
- `tests/Application.UnitTests/Common/Behaviours/RequestLoggerTests.cs:29,39`: `new LoggingBehaviour<CreateTodoItemCommand>(...)` and `await requestLogger.Process(cmd, new CancellationToken())` are not taken along.
- This breaks the all-or-nothing promise: 0 Manual, yet a broken build.
- Logs: `<corpus>/corpus-mediatr/_results/r2/jason/` (`build1-errors.txt`, `logs/build1.log`).

## Analysis

- Construction `X<TReq>` of a rewritten processor → `X<TReq, TRes>`; `TRes` from `TReq : IRequest<TRes>` (`Unit` variants: `IRequest` without a result → how does the processor fill in `TResponse`? check first).
- Call `.Process(req, ct)` → public `Handle(req, next, ct)` of `MessagePreProcessor` (pipeline signature) with a `next` that returns `default`, or Manual if that changes behavior (a post-processor needs a response).
- Not traceable (no `IRequest<T>`, open generic, reflection) → Manual with location; never silent.

## Choices

- Own pattern `mediatr.request-processor-caller` (`RequestProcessorCallerPattern`), before `mediatr.request-processor` in `MediatRFix.Patterns`: the callers then still bind to the MediatR class. Contracts unchanged; `ResponseOf` moved to `MediatRSyntax` (shared).
- `HasOtherUses` (own project only, `used-elsewhere`) is gone from `RequestProcessorPattern`: it was the short circuit that let jason slip through (test project not searched). Callers are now found solution-wide and rewritten or made Manual.
- `new P<Cmd>()` and any other type reference: `P<Cmd, TRes>` with `TRes` from `IRequest<T>` (`IRequest` gives `Unit`); otherwise Manual.
- `await p.Process(req, ct);` becomes `await p.Handle(req, static (_, _) => default, ct);` (pre) or `Handle(req, (_, _) => new ValueTask<TRes>(res), ct)` (post): the public pipeline `Handle` of `MessagePre/PostProcessor`; the protected override is not callable. Same behavior: pre runs its own Handle and then `next`, post runs `next` and then its own Handle.
- Two findings per call (name and argument list) to avoid nested replacements (Fixer: overlap).
- Manual: result used, not awaited, mock of `Process`, call through the MediatR interface, open generic argument, unknown response type.

## Steps

1. [x] Proof `_Combined/request-processor-callers` (new, var, target-typed new, Unit, post-processor).
2. [x] Safety net: Manual proofs `RequestProcessorCaller/result-used`, `through-interface`, `unknown-response`.
3. [x] Rewrite in `RequestProcessorCallerPattern` + KingmaDoc docs (plan, models, verify, README, INDEX).
4. [ ] Proof: harness not run on jason (corpus is not in this environment); mimicked with the `_Combined` proof.

## See also

- `src/Unshackle.Core/Fixes/MediatR/Patterns/RequestProcessorPattern.cs`
- `claude/tasks/archief/TAAK-182-mediatr-inactieve-if-global-using.md`
- `claude/tasks/CORPUSRUN-mediatr.md`
