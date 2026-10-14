# Verification: mediatr-generic-notification

|               |                                              |
| ------------- | -------------------------------------------- |
| **Plan**      | [`mediatr-generic-notification-plan.md`](mediatr-generic-notification-plan.md) |
| **Status**    | Verified (proofs, unittests, corpus nadirbad) |
| **Generated** | 2026-10-12 (by hand) |

## Requirements

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `Proofs/MediatR/GenericNotification/make-generic-type` (after compiles against Mediator 3.0.2 with the generator) |
| REQ-2 | Met | `GenericNotification/new-closed` |
| REQ-3 | Met | `GenericNotification/last-in-file`; nadirbad failed on SA1513 at first |
| REQ-4 | Met | `_Combined/generic-notification-nadirbad`; corpus nadirbad `feb3a15`: 18 Fixed, 0 Manual, build 0 errors (1× MSG0005-warning), tests 30/30 → 30/30 |
| REQ-5 | Met | `GenericNotificationPatternTests` (4 tests, two projects) |
| REQ-6 | Met | `GenericNotification/generic-code`, `no-constraint` |

## Deviations

- Nadirbad run with a worktree of `ac683dd` + this pattern (ongoing work of other agents broke the build in the main folder).
- `GTypes.Close` throws for a type outside the solution; MediatR would then silently publish nothing.

## See also

- [mediatr-generic-notification-plan.md](mediatr-generic-notification-plan.md)
- `claude/tasks/CORPUSRUN-mediatr.md`
