# TAAK-390 — Scout ImageSharp → SkiaSharp/Magick.NET

**Status**: done (awaiting council)
**Created**: 2026-10-22
**Goal**: A go/no-go recommendation for a new `imagesharp` fix, backed by primary sources and a source-level corpus survey, in `docs/projectinfo/architectuur/candidate-imagesharp.md`.

## Context for the next agent

- Unshackle migrates .NET code off libraries with license, sponsor or EOL problems; roadmap steps 7-10 (Moq, Polly, EPPlus, MassTransit) are planned. This scout looks for the next candidate (user request 2026-10-22: use spare capacity on other libraries).
- Scouts are light: web sources and shallow clones under `<corpus>/corpus-candidates/imagesharp/`, grep counts only, no builds.
- Owned files and reserved numbers (391–399): see `claude/tasks/CLAIMS.md`. The go/no-go itself is a council decision (`claude/docs/council.md`).

## Steps

- [x] 1. Facts from primary sources (license/EOL, dates, links) and API distance; Fixed vs Manual sketch; behaviour-proof idea
- [x] 2. Survey: 8-15 repos with pinned shas, grep counts per API shape, demand and effort (S/M/L) in `candidate-imagesharp.md`
- [x] 3. Recommendation + council questions; if go, a draft follow-up task (number 391–399)

## Progress log

- 2026-10-22: created; agent scout-imagesharp started.
- 2026-10-22: facts, 11 repos counted, document written; recommendation conditional no-go (park), effort L.

## Changes

| Field     | Content |
| --------- | ------- |
| **Where** | `docs/projectinfo/architectuur/candidate-imagesharp.md` |
| **How**   | Research |
| **Why**   | Pick the next library with evidence instead of a guess |

## See also

- `claude/tasks/CLAIMS.md` — file claims and reserved numbers
- `claude/tasks/POOL-2026-10-22.md` — pool rules
- `docs/projectinfo/project.md` — roadmap
