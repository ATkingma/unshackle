---
kingmadoc: 1
feature: "polly-no-op"
status: approved
requirements: [REQ-1, REQ-2, REQ-3]
files_expected:
    - src/Unshackle.Core/Fixes/Polly/Patterns/NoOpPattern.cs
    - src/Unshackle.Core/Fixes/Polly/PollyRetry.cs
    - src/Unshackle.Tests/Proofs/Polly/NoOp/
---

# Feature: polly.no-op: Policy.NoOp(Async) + Execute(Async) → ResiliencePipeline.Empty

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-22 by KingmaDoc |

> Drafted from TAAK-308, queue row 11 (`claude/tasks/polly-queue.md`), council round 8 general rule.

## One-sentence summary

polly.no-op: `Policy.NoOp(Async)()` (also generic), held in a local, a private field or inline, becomes `ResiliencePipeline.Empty` (`ResiliencePipeline<T>.Empty`).

## Scope (in / out)

**In scope**: `NoOp()`, `NoOpAsync()`, `NoOp<T>()`, `NoOpAsync<T>()`; the Execute rules of the retry patterns.

**Out of scope**: policies returned, passed or stored as properties (Manual `PolicyNotLocal`), `Execute` overloads with Context/token.

## Requirements

- **REQ-1**: WHEN a NoOp policy is held in a local or inline and only run through `Execute(Async)` THE SYSTEM SHALL emit `ResiliencePipeline.Empty` / `ResiliencePipeline<T>.Empty` (proofs `async`, `typed`, `sync-inline`).
- **REQ-2**: THE SYSTEM SHALL give equal run-time traces before and after (`PollyBehaviourProofTests`).
- **REQ-3**: WHEN the policy is returned or stored outside a local or private field THE SYSTEM SHALL report Manual `PolicyNotLocal` (proof `returned`).

## Assumptions

- `ResiliencePipeline.Empty` runs the callback without any strategy, as v7 NoOp does.

## Models

[polly-no-op-modellen.md](polly-no-op-modellen.md).

## Open questions

- None.
