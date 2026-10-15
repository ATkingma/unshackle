# Corpus run — blind validation round (TAAK-268)

Tool: published from main `23c0f28` into `<corpus>/corpus-blind/_results/tool-23c0f28/`. The tool is not changed during this round.
Harness: `claude/tools/corpus-am.sh` with `REPOS=claude/tools/corpus-blind-repos.txt`, list `claude/tools/corpus-blind-list.txt` (fix per line).
Corpus root: `/mnt/dikkeschijf/unshackle-corpus/corpus-blind/`.

## Selection (step 2)

Picked from GitHub code search (`gh search code 'Include="<Library>" Version="<major>.'`, csproj) on 2026-10-15, before any run. None is on the exclusion list. Shas pinned in `corpus-blind-repos.txt`.

| Name | Repo | Fix | Size | Why |
| --- | --- | --- | --- | --- |
| skunkdeco | bfriesen/RandomSkunk.DependencyInjection.Decorator | fluentassertions | small (38 kB) | Library with one test project; FA 6; multi-target incl. net461/netstandard |
| ogooreck | oskardudycz/Ogooreck | fluentassertions | medium | API-testing library (128 stars); FA 6 across multi-targeted test projects |
| omnikeeper | max-bytes/omnikeeper | fluentassertions | large (9 test projects) | Real application backend; net6/net7 |
| realworld | minhlong149/realworld-aspnetcore | mediatr | small | RealWorld (Conduit) API with MediatR 12 handlers and a test project |
| fullcart | AmrElshaer/FullCart | mediatr | medium (35 files use MediatR) | Clean-architecture shop; MediatR 12 |
| miru | MiruFx/Miru | mediatr | large (8 test projects) | Web framework built on MediatR; net5–net9 |
| culinary | culinary-code/backend | automapper | small | API with AutoMapper 13 profiles and a test project |
| ctwapi | Coflnet/CtwApi | automapper | small | AutoMapper 13 in an API with tests |
| msb | Robnoir/MSB | automapper | large (106 files) | Application using AutoMapper 13 (and MediatR); one test project |

## Results

_(base, scan and after per repo: filled in during steps 4–5)_

## See also

- `claude/tasks/TAAK-268-blind-validation-round.md`
- `claude/tasks/CORPUSRUN-automapper.md`
