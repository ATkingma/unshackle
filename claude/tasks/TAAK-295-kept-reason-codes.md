# TAAK-295 — Reason codes for every kept finding (after 0.2.0)

**Status**: step 1/4 — not started
**Created**: 2026-10-19
**Goal**: council round 4 Q2: kept maps, kept calls, kept `AddAutoMapper` and configuration tests use a typed reason code (like `KeptReason` on the `GetService<IMapper>()` lookup) instead of the `Stays on AutoMapper:` string prefix.

## Context for the next agent

- Do this after release 0.2.0; the output must not change (tests pin the rendered text).
- `AutoMapperFix.AllowsPartial` accepts both the code and the old prefix today; after this task, only the code.

## Steps

- [ ] 1. Inventory all prefix uses; plan
- [ ] 2. Implementation + tests (rendered text unchanged)
- [ ] 3. Corpus `after` on the six AutoMapper repos: identical reports
- [ ] 4. Verify, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/docs/council.md`, `claude/tasks/archief/TAAK-293-council-round-3.md`
