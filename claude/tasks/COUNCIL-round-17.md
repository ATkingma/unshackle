# Council round 17 — transitive pins, MediatR generic-request closure, EF bulk

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint
(round 1 split on T1, T2, M15, B1, B4, B6; round 2 accepted the proposals below, amendments folded in).
Questions: council questions 1-3 in `claude/tasks/archief/TAAK-329-transitive-package-loss.md`, the open question in
`claude/tasks/archief/TAAK-338-automapper-closure-regressions.md`, council questions 1-8 in `claude/tasks/TAAK-342-ef-bulk.md`.
Follow-ups: TAAK-343 (T1-T3), TAAK-344 (M15), TAAK-342 steps 4-9 (B1-B8). TAAK-345 is not used.

| # | Decision | Joint rule |
| --- | --- | --- |
| T1 | Pin vs the new package's closure (TAAK-329 Q1) | Keep pinning the pre-migration version. The row text starts with "may conflict (NU1605)" and says "raise the pin" as the remedy; the same hint appears when the post-fix build/verify fails with NU1605. A nuspec reader (Core interface, DataAccess implementation) is built as soon as the corpus shows one NU1605. |
| T2 | Dependent that declares the package itself (Q2) | Keep pinning (one predictable rule); the Fixed row reason names the user project ("kept for `<project>`"). |
| T3 | Package reached through a project reference (Q3) | Accept the same-version pin; document "kept = package libraries in assets only" as a limitation in `core.md`. A differing version → Manual, not a pin. |
| M15 | `mediatr.generic-request` follows callers via `TypeArgumentClosure` (TAAK-338) | Yes, as its own task, guarded: a closed request is Fixed only when a handler registration for it is found statically; a handler that only an assembly-scan call would pick up stays Manual. A corpus count of open generic requests (proofs + MediatR corpus repos) sets priority (>= 3 → do it), not correctness. Until then the Manual reason says "callers not followed". |
| B1 | Go / priority (TAAK-342 Q1) | Go, narrow. README roadmap line only after step 4 (signatures) and step 6 (SQL proof). |
| B2 | EFCore.BulkExtensions `BatchDelete/BatchUpdate` in v1 (Q2) | No; a later slice decided by corpus counts. |
| B3 | EF Core 7+ precondition (Q3) | Per-call Manual `NoNativeExecute` stays, plus one project-level Warning row ("EF Core < 7: upgrade first, N calls blocked"). |
| B4 | Non-trivial values (Q4) | Manual stays the default. Widen only to an allow-list: literals, captured locals/parameters, entity member access, numeric arithmetic of those (no string concatenation), each with a proof, plus a decimal overflow/rounding proof. `DateTime.Now`, `Math.*` only after a step-6 SQL proof, one per proof. Manual rows show the exact expression. |
| B5 | Package policy (Q5) | Remove a Z package per project only at zero Z usages (Bulk*, Future, FromCache, AuditTrail included; dependents counted; Plus and Extensions both); otherwise leave it and report the remaining usages with file:line. |
| B6 | Stubs vs real types (Q6) | Stubs accepted. Step 4 is a one-time check against the nupkg (scratch dir, metadata only); verified signatures and the nupkg version go in the docs, and `EfBulkReferences.cs` names that version. No CI download. |
| B7 | `DeleteAsync(builder, ct)` (Q7) | Manual `BatchOptions`; the row names the options. A token-only overload is mapped if the corpus shows it common. |
| B8 | README note on `EFCore.BulkExtensions.MIT` (Q8) | Yes, one neutral line ("alternative, not migrated automatically") after checking the fork's license and maintenance. |

## T1-T3 — transitive pins

- G: a pin that causes NU1605 breaks restore, which is worse than Manual; one corpus NU1605 is enough to build the nuspec reader.
- P: the pin reproduces what compiled before; a clear row line is cheaper than a reader for a rare case. P wanted to skip T2's pin;
  accepted keeping it when the reason names the user project.
- A: one rule "pin every lost package with a user" is predictable; the reader, when built, is a Core interface with the I/O in DataAccess.

## M15 — MediatR generic-request closure

- G: closing a request can compile and still resolve no handler at run time; only a static registration counts.
- P: AutoMapper closes the same shape, so Manual for MediatR is inconsistent hand work; count first.
- A: one closing rule across generic patterns; the threshold is a priority trigger, the handler guard is required regardless.

## B1-B8 — EF bulk

- G: the stubs were written from memory; wrong stubs mean wrong rewrites with green proofs, so step 4 precedes any claim. Client vs server
  evaluation of values is a silent behaviour change.
- P: trial expiry (ERROR_005) is a run-time failure in production, a strong motive; one summary row beats N identical ones.
- A: one source family per fix (Z only); the same removal rule as Polly; no network I/O in CI. P's amendment "step 4 alone suffices for
  an experimental roadmap line" was not taken (G and A wanted step 6 too; P accepted the base rule).

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-16.md`
- `claude/tasks/archief/TAAK-329-transitive-package-loss.md`
- `claude/tasks/archief/TAAK-338-automapper-closure-regressions.md`
- `claude/tasks/TAAK-342-ef-bulk.md`
- `claude/tasks/archief/TAAK-343-transitive-pin-followups.md`
- `claude/tasks/TAAK-344-mediatr-generic-request-closure.md`
