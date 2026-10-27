# TAAK-335 — Polly: redundant transient clause

**Status**: done
**Created**: 2026-10-27
**Goal**: `builder.AddTransientHttpErrorPolicy(p => p.OrTransientHttpStatusCode().WaitAndRetryAsync(...))` wrote a second, identical
`HandleResult(5xx || 408)` clause (corpus B2 side issue, `claude/tasks/CORPUSRUN-polly.md`). Write the transient predicate once.

## Context for the next agent

- Code: `src/Unshackle.Core/Fixes/Polly/PollyPredicate.cs` (`TransientClauses`, `Concat`).
- Pattern docs: `docs/projectinfo/patterns/polly-add-policy-handler-plan.md` REQ-3b, `-verify.md`.

## Steps

- [x] 1. `PollyPredicate`: the transient filter is two clauses (`Handle<HttpRequestException>()`, `HandleResult(5xx || 408)`); when the
      chain starts at a transient filter, a generated clause that is already present is dropped. User clauses are never deduplicated.
- [x] 2. Proof `src/Unshackle.Tests/Proofs/Polly/AddPolicyHandler/transient-or-status` (before/after, trace in `PollyBehaviourProofTests`).
- [x] 3. Unit tests in `AddPolicyHandlerPatternTests` (`OrTransientHttpStatusCode`, `OrTransientHttpError`, `HandleTransientHttpError()...`,
      user clause kept).
- [x] 4. KingmaDoc plan/verify REQ-3b; CORPUSRUN-polly note.
- [x] 5. `dotnet build`, `dotnet test`, `verify-head.sh`.

## Council questions (new)

- P-Q1 (conservative choice taken): only clauses Unshackle generates itself are deduplicated. An identical user-written clause
  (e.g. `.OrResult(r => (int)r.StatusCode >= 500 ...)` after the transient filter) is kept verbatim, even though it is redundant. Also dedupe
  user clauses that are textually equal to a generated one?

## See also

- `claude/tasks/CORPUSRUN-polly.md` (B2)
- `docs/projectinfo/patterns/polly-add-policy-handler-plan.md`
