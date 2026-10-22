---
kingmadoc: 1
feature: "polly-circuit-breaker"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/CircuitBreakerPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/CircuitBreaker/
---

# Feature: polly.circuit-breaker: CircuitBreaker(Async) / AdvancedCircuitBreaker(Async): Manual

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 8 (`claude/tasks/polly-queue.md`), council round 8 general rule.

## One-sentence summary

polly.circuit-breaker: every v7 circuit breaker is reported Manual `CircuitBreakerNotEquivalent`, because the probe showed v7 and the nearest v8 mapping break on different call sequences.

## Scope (in / out)

**In scope**: `CircuitBreaker`, `CircuitBreakerAsync`, `AdvancedCircuitBreaker`, `AdvancedCircuitBreakerAsync` calls on a Polly policy builder.

**Out of scope**: any rewrite. Council question: which v8 ratio/window the user wants cannot be derived from v7 arguments.

## Requirements

- **REQ-1**: WHEN a v7 circuit breaker is found THE SYSTEM SHALL report Manual `CircuitBreakerNotEquivalent` and leave the code (proofs `consecutive`, `advanced`).
- **REQ-2**: THE SYSTEM SHALL keep a run-time test that documents the divergence: sequence F S F F F F with threshold 3 gives `F,S,F,F,F,B` on v7 and `F,S,F,F,F,F` on v8 with FailureRatio 1.0 and MinimumThroughput 3 (`CircuitBreaker_V8RatioMapping_BreaksOnDifferentCalls`).
- **REQ-3**: THE SYSTEM SHALL NOT report other Polly calls (unit test `CircuitBreaker_Find_OnlyBreakerCalls`).

## Assumptions

- Also measured in the probe (TAAK-308): v8 rejects BreakDuration below 500 ms and MinimumThroughput below 2, which v7 allows. The advanced breaker (ratio over a window) was not mapped: its window bucketing is time based and no equal-trace proof was made.

## Models

[polly-circuit-breaker-modellen.md](polly-circuit-breaker-modellen.md).

## Open questions

- Council: should the circuit breaker get a hand-chosen v8 mapping (ratio, window) per call site, or stay Manual?
