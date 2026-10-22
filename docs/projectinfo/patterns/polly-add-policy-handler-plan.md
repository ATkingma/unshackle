---
kingmadoc: 1
feature: "polly-add-policy-handler"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/AddPolicyHandlerPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyPredicate.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/AddPolicyHandler/
---

# Feature: polly.add-policy-handler: AddPolicyHandler / AddTransientHttpErrorPolicy → AddResilienceHandler

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 6 (`claude/tasks/polly-queue.md`); council round 8 general rule, P1–P3; round 10 P9/P10.

## One-sentence summary

polly.add-policy-handler: an HttpClient handler built from an inline v7 retry (`.AddPolicyHandler(<retry>)` or
`.AddTransientHttpErrorPolicy(p => p.<retry>)`) becomes `.AddResilienceHandler("retry", builder => builder.AddRetry(new
RetryStrategyOptions<HttpResponseMessage> { ... }))` with the same filter, count, delay and onRetry.

## Scope (in / out)

**In scope**: `AddPolicyHandler(IAsyncPolicy<HttpResponseMessage>)` with a retry call written in place, and
`AddTransientHttpErrorPolicy(p => p.<retry>)`; every retry form of `polly.wait-and-retry-async` and `polly.retry-variants`
(filters, onRetry); `HttpPolicyExtensions.HandleTransientHttpError()`, `.OrTransientHttpError()`, `.OrTransientHttpStatusCode()`.

**Out of scope (Manual)**: a request-based selector or registry overload (`HandlerOverloadNotSupported`), a policy from a field or registry, or from a method used elsewhere (`HandlerPolicyNotInline`), a client with more than one handler (`HandlerOrderNotProvable`).

## Requirements

- **REQ-1**: THE SYSTEM SHALL use plain `RetryStrategyOptions<HttpResponseMessage>`, not `HttpRetryStrategyOptions`, which honours
  `Retry-After` by default where v7 does not.
- **REQ-2**: THE SYSTEM SHALL write the v7 transient filter out as `.Handle<HttpRequestException>().HandleResult(response =>
  (int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.RequestTimeout)`, not `HttpClientResiliencePredicates`,
  which also retries 429 and timeouts (proof `transient-lambda-429`: 429 is not retried, before and after).
- **REQ-3**: WHEN `AddTransientHttpErrorPolicy(p => ...)` THE SYSTEM SHALL treat `p` as `HandleTransientHttpError()` and keep the
  clauses chained on it (`OrResult`).
- **REQ-4**: THE SYSTEM SHALL give equal run-time traces before (Microsoft.Extensions.Http.Polly 10.0.13, Polly 7.2.4) and after
  (Microsoft.Extensions.Http.Resilience 10.11.0) with a fake primary handler (proofs `inline-transient`, `transient-lambda-429`).
- **REQ-5**: WHEN the client chain has another handler call THE SYSTEM SHALL report Manual `HandlerOrderNotProvable` (proof `two-handlers`).
- **REQ-6**: WHEN the policy is neither written in place nor a handler-only method (REQ-8) THE SYSTEM SHALL report Manual `HandlerPolicyNotInline`;
  the retry patterns leave a handler's policy to this pattern (`Find_HandlerPolicy_OnlyTheHandlerPatternReportsIt`).
- **REQ-7**: THE SYSTEM SHALL add `Microsoft.Extensions.Http.Resilience` only to projects that called `AddPolicyHandler` or
  `AddTransientHttpErrorPolicy`, and remove the `Polly.Extensions.Http` using once nothing binds to it.

- **REQ-8** (slice 2): WHEN the policy is `M()`, a private parameterless method of the same (non-partial) type whose body only returns
  a retry chain and whose every reference is an `AddPolicyHandler(M())` argument THE SYSTEM SHALL inline the options at each call and
  remove the method (`Fixed([])`); helper names must be free at both places (council P10). Otherwise the call and the method are
  Manual `HandlerPolicyNotInline` (proof `policy-method` + trace; `Rewrite_PolicyMethodAlsoUsedElsewhere_HandlerAndMethodManual`).

## Assumptions

- `AddResilienceHandler` resends the same request on retry, like v7's `PolicyHttpMessageHandler`; Polly.Core disposes a discarded
  response, which the caller never sees (pinned by the traces).

## Models

[polly-add-policy-handler-modellen.md](polly-add-policy-handler-modellen.md).

## Open questions

- None.
