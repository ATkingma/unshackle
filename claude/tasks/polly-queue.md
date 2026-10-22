# Pattern queue (Polly v7 API → Polly v8 via Microsoft.Extensions.(Http.)Resilience)

**Decision (2026-10-22, council round 8)**: target `Microsoft.Extensions.(Http.)Resilience` (P1); the v7 delay lambda is kept as
`DelayGenerator` (P2); Fixed only when literal, visible in one place and pinned by a behaviour proof, otherwise Manual with a reason
code; the package goes per project only at 0 Manual rows. Same way of working as `claude/tasks/automapper-queue.md`; a row gets a
TAAK number (340–349) when claimed. Order by projects first, hits second in `docs/projectinfo/architectuur/corpus-polly.md`
(20 repos). Every Fixed row needs a run-time proof (before on Polly 7.2.4, after on Polly.Core, equal traces), as in
`src/Unshackle.Tests/Core/Polly/PollyBehaviourProofTests.cs`.

| #   | Pattern ID | Polly v7 | Expected | Corpus (hits / projects) | Status |
| --- | --- | --- | --- | --- | --- |
| 0   | _infrastructure_ | `PollyFix`, package swap, proof references, behaviour-proof runner | — | — | done (TAAK-308 st 4) |
| 1   | `polly.wait-and-retry-async` | `Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay)` (local or inline) + awaited `ExecuteAsync(() => ...)` | `new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions { ShouldHandle, MaxRetryAttempts, DelayGenerator }).Build()` | 27 / 16; ExecuteAsync 51 / 12 | done (TAAK-308 st 4) |
| 2   | `polly.exception-filter` | `.Or<E>()`, `Handle<E>(predicate)`, `HandleResult<T>()`, `Policy<T>.Handle<E>()`, `.OrResult(...)` | `PredicateBuilder(<T>)` chain; `ResiliencePipelineBuilder<T>`; `HandleInner` Manual | 30+5+3+21 / 12 | done (TAAK-308; folded into `polly.wait-and-retry-async`, REQ-9/10: same node, one pattern) |
| 3   | `polly.on-retry` | `onRetry` (exception, timespan[, attempt][, context]) | `OnRetry = args => { ... }` 1:1 mapping, `args.AttemptNumber + 1`; a body using `context` Manual (P3) | 36 / — | done (TAAK-308; folded into `polly.wait-and-retry-async`, REQ-13/14) |
| 4   | `polly.sync-retry` | `WaitAndRetry`/`Retry` + `Execute` (sync) | same pipeline, `Execute(_ => ...)` | 11 / 2; Execute 12 / 3 | open |
| 5   | `polly.policy-member` | `IAsyncPolicy(<T>)`/`AsyncRetryPolicy` fields, properties, return types, parameters | `ResiliencePipeline(<T>)`, all uses in the solution rewritten or all Manual | 39 / 9; 23 / 6 | partly done (TAAK-308): private fields initialised at their declaration in a non-partial type, folded into both retry patterns; slice 2: fields assigned once, unconditionally, in a constructor; properties, return types and parameters stay Manual (need a solution-wide all-or-none analysis) |
| 6   | `polly.add-policy-handler` | `AddPolicyHandler(...)`, `AddTransientHttpErrorPolicy`, `HttpPolicyExtensions.HandleTransientHttpError()` | `AddResilienceHandler(name, b => ...)` with `HttpClientResiliencePredicates`; adds `Microsoft.Extensions.Http.Resilience` | 32 / 8; 18 / 7 | partly done (TAAK-308): inline policies and `AddTransientHttpErrorPolicy`, plain `RetryStrategyOptions` + written-out v7 filter; policy from a method/field next |
| 7   | `polly.retry-variants` | `RetryAsync(n)`, `RetryForever(Async)`, `WaitAndRetryForever(Async)`, `WaitAndRetryAsync(IEnumerable<TimeSpan>)` | `MaxRetryAttempts = int.MaxValue` / indexed `DelayGenerator` | 11 / 7; 4 / 3 | done (TAAK-308) |
| 8   | `polly.circuit-breaker` | `CircuitBreaker(Async)` | `AddCircuitBreaker` only where the v7 count-based semantics are proven equal; else Manual | 5 / 3 | open |
| 9   | `polly.timeout` | `Policy.Timeout(Async)`, `TimeoutRejectedException` | `AddTimeout`; optimistic/pessimistic difference Manual | 7 / 5 | open |
| 10  | `polly.wrap` | `Policy.Wrap(Async)` / `.WrapAsync` | builder adds outermost first; proof per arity 2 and 3 (P4), else Manual `WrapOrderNotProvable` | 17 / 9 | open |
| 11  | `polly.no-op` | `Policy.NoOp(Async)` | `ResiliencePipeline.Empty` | 2 / 2 | open |
| 12  | `polly.unsupported` | `Fallback`, `Bulkhead`, `Cache`, `RateLimit`, `PolicyRegistry`, `Context`, `ExecuteAndCapture`, Contrib `Backoff` | Manual with reason code | 1+1+2+3+29+14+4+1 | open |
| 13  | _corpus run_ | build + tests before/after on the 20-repo corpus | 0 regressions; Manual only where justified | — | open (TAAK-308 st 6) |

**Recommended order**: 2 → 3 → 5 → 6 → 7 → 4 → 10 → 9 → 8 → 11 → 12, then 13. Rows 2 and 3 widen row 1 (most `WaitAndRetryAsync`
calls carry `Or<E>` or `onRetry`); row 5 unlocks the policies held in fields (the common shape in services); row 6 is the
second-largest group. Rows 2, 3 and 7 change `PollyRetry.cs`: work them one after another, not in parallel.

## See also

- `claude/tasks/TAAK-308-polly-to-resilience.md` — the task
- `docs/projectinfo/architectuur/corpus-polly.md` — frequencies
- `claude/tasks/automapper-queue.md` — the model for this queue
- `claude/docs/council.md` — round 8, P1–P7
