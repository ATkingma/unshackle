---
kingmadoc: 1
feature: "polly-unsupported"
status: approved
requirements: [REQ-1, REQ-2]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/UnsupportedPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/Unsupported/
---

# Feature: polly.unsupported: Fallback, Bulkhead, Cache, RateLimit, PolicyRegistry, Context, ExecuteAndCapture, Contrib Backoff: Manual

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 12 (`claude/tasks/polly-queue.md`), council round 8 general rule.

## One-sentence summary

polly.unsupported: v7 constructs without a proven v8 mapping are reported Manual with a reason code each, so a project keeps its v7 package until they are handled by hand.

## Scope (in / out)

**In scope**: `Fallback(Async)`, `Bulkhead(Async)`, `Cache(Async)`, `RateLimit(Async)`, `new PolicyRegistry()`, `new Context(...)`, `ExecuteAndCapture(Async)` and calls into Polly.Contrib.WaitAndRetry.

**Out of scope**: any rewrite; type mentions such as `IReadOnlyPolicyRegistry` parameters are not reported by this pattern.

## Requirements

- **REQ-1**: WHEN one of the listed calls or object creations on a Polly type is found THE SYSTEM SHALL report Manual with the matching code `FallbackNotMapped`, `BulkheadNotMapped`, `CacheNotMapped`, `RateLimitNotMapped`, `PolicyRegistryNotMapped`, `ContextNotMapped`, `ExecuteAndCaptureNotMapped` or `ContribBackoffNotMapped` (proofs `fallback`, `bulkhead`, `rate-limit`, `registry`, `context`, `execute-and-capture`).
- **REQ-2**: THE SYSTEM SHALL NOT report plain retry calls (unit test `Unsupported_Find_PlainRetryIsNotReported`).

## Assumptions

- No v8 construct was proven equal for these; Fallback and RateLimit have v8 strategies, but their option shapes and callbacks differ and were not measured.

## Models

[polly-unsupported-modellen.md](polly-unsupported-modellen.md).

## Open questions

- None.
