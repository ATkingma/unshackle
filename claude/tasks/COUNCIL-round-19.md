# Council round 19 — transitive pins, EPPlus range locals, EF bulk update values

Date: 2026-10-27. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. All decisions joint
(round 1 split on T5, R3, B10, B11; round 2 accepted the proposals below).
Questions: council questions 1-2 in `claude/tasks/archief/TAAK-343-transitive-pin-followups.md`, council questions 1-3 in
`claude/tasks/archief/TAAK-337-epplus-range-locals.md`, council questions 9-12 in `claude/tasks/TAAK-342-ef-bulk.md`.
Follow-ups: TAAK-349 (T4, T5), TAAK-346 step 2a (R3), TAAK-342 steps 7a-7c (B10, B11, B12). TAAK-350 is not used.

| # | Decision | Joint rule |
| --- | --- | --- |
| T4 | Lower required version (TAAK-343 Q1) | Stays Manual. Removing the pin would silently downgrade the package to the reference's lower bound (no NU1605, but a behaviour change). The reason text distinguishes the two cases: "higher: NU1605 risk" vs "lower: removing the pin downgrades to X". |
| T5 | Several users in "kept for" (Q2) | List all user projects in solution order, capped at three names plus "and N more"; the full list goes in the detailed report. |
| R1 | Other range uses (TAAK-337 Q1) | Manual stays; the reason names the use (value, style, `Merge()`, `InsertData`). Extend per use only on corpus evidence, reusing the existing `ws.Cells[..]` mappings. |
| R2 | Range parameter passed on (Q2) | Manual accepted; no range chains until the corpus shows them. The reason names the callee. |
| R3 | Non-`Cells[..]` range sources (Q3) | `UnmappedAddress` accepted, but one row per node (as E31): when `epplus.unsupported` already reports the node, the address reason is folded into that row and the summary counts it once. |
| B9 | Decimal arithmetic (TAAK-342 Q9) | Manual stays. Widen only with a real-provider proof (SQL Server or PostgreSQL via Testcontainers); "same SQL as Z" is not enough because rounding to column precision is provider-specific. Backlog, not a step now. |
| B10 | String operands (Q10) | Allow `==` / `!=` between a string member and a non-null literal or `const`, with a SQLite proof that includes a NULL column row. Captured string locals/fields (may be null), other operators and concatenation stay Manual. |
| B11 | Captured members beyond B4 (Q11) | Keep captured fields, instance properties of captured values, enum members and constants (all client-evaluated by Z and EF); each gets a proof. Static members (`DateTime.Now`) stay Manual per B4. |
| B12 | Project row severity (Q12) | Accepted: a non-blocking project-level Manual row `EfCoreBelow7`; the plan documents that it is the B3 "Warning" row. No new report kind. |

## T4-T5 — transitive pins

- G: a lower bound is not "safe": dropping the pin changes the resolved version without any build signal.
- P: the row must tell the user which case applies so they know whether to edit the reference or keep the pin.
- A: one rule ("a differing version → Manual") stays; only the reason text differs. A capped list keeps rows readable.

## R1-R3 — EPPlus range locals

- G: each range use has its own ClosedXML semantics; widen per use with a proof.
- P: the corpus decides which uses are worth it; the reason must name the use so hand work is quick.
- A: a second row for a node already in `epplus.unsupported` double-counts; reuse the E31 one-row-per-node rule.

## B9-B12 — EF bulk update values

- G: string equality with a NULL column can differ between C# semantics and SQL; only a proof with a NULL row is enough, and only for
  non-null constants. Decimal rounding is provider-specific.
- P: `o.Status == "x"` is common; Manual for it is needless hand work. Captured request properties are the main real-world shape.
- A: the allow-list stays one explicit list in `UpdatePattern` with one proof per entry; no new report severity for one row.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-18.md`
- `claude/tasks/archief/TAAK-349-transitive-pin-reasons.md`
- `claude/tasks/archief/TAAK-346-epplus-flow-rows.md`
- `claude/tasks/TAAK-342-ef-bulk.md`
- `claude/tasks/archief/TAAK-343-transitive-pin-followups.md`
- `claude/tasks/archief/TAAK-337-epplus-range-locals.md`
