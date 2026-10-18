# Verification: automapper-reverse-map

|               |                                                                                              |
| ------------- | -------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-reverse-map-plan.md`](automapper-reverse-map-plan.md) |
| **Status**    | Verified                                                                                     |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                           |

> Verified by an AI agent against the code on branch of TAAK-249 (before merge).

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/CreateMapPattern.cs` (`Rewrite`, `TryParseMap`, `ReverseMapCall`, `TryParseOptions`, `HasReverseMap`, `Reverse`, `MemberPath`, `Unwrap`, `MayFlatten`, `Problem`), `NestedMaps.cs` (`AddProfiles`), `MapCallPattern.cs` (`GeneratedAsync`). Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/CreateMap/reverse-map*`, `CreateMap/member-list-reverse-map`, `ForMemberIgnore/reverse-map*`, `ForMemberMapFrom/reverse-map*`, `_Combined/reverse-map-call`. Unit tests: `src/Unshackle.Tests/Core/CreateMapPatternTests.cs`, `MapCallPatternTests.cs`.

| REQ    | Verdict | Evidence                                                                                                       |
| ------ | ------- | -------------------------------------------------------------------------------------------------------------- |
| REQ-1  | Met     | `CreateMap/reverse-map` (jhipster and bitwarden forms, with `MemberList.Destination`), `CreateMap/member-list-reverse-map` |
| REQ-2  | Met     | `CreateMap/reverse-map` (`CreationDate` keeps its initializer), `CreateMap/member-list-reverse-map` (`PasswordHash`), `_Combined/reverse-map-call` (`CreatedBy`) |
| REQ-3  | Met     | `_Combined/reverse-map-call`, `ForMemberIgnore/reverse-map-options` (`Note` ignored forward, mapped in reverse), unit test case "Rank" |
| REQ-4  | Met     | `ForMemberIgnore/reverse-map-options` (`Read`), `ForMemberMapFrom/reverse-map` (`Note = source.Label`), unit test `Rewrite_ReverseOptionAndReversedMapFrom_OptionWins` |
| REQ-5  | Met     | `ForMemberMapFrom/reverse-map` (`Name = source.Title`)                                                         |
| REQ-6  | Met     | `CreateMap/reverse-map-nested` (`Job = source.Job?.ToJob()`, list copy through `ToJob`)                        |
| REQ-7  | Met     | `_Combined/reverse-map-call` (`dto.ToOrder()`, `dto.MapInto(order)`), unit test `FixAsync_ReverseMapInOtherFile_CallsReverseMethod` |
| REQ-8  | Met     | `ForMemberMapFrom/reverse-map-unflattening`, unit test cases "maps the whole A", "may be unflattened into A.Customer" |
| REQ-9  | Met     | `CreateMap/reverse-map-get-method`, `ForMemberMapFrom/reverse-map-may-be-null` (plain chain, TAAK-263), unit test cases "casts", "both read Id", "cannot be written", method `code()` |
| REQ-10 | Met     | `CreateMap/reverse-map-options` (jhipster `ForPath`, since TAAK-264 it stops at the collection copy), `CreateMap/reverse-map-duplicate`; Condition/ForPath on the reverse map: see `automapper-member-options` REQ-9 |
| REQ-11 | Met | `CreateMap/reverse-prefix-not-flattened` (Fixed), `CreateMap/reverse-unflatten` (Manual); corpus msb (TAAK-273) |

## Deviations

- No pattern class of its own: the behaviour is part of the shared profile rewrite (see plan, Design); the proofs live in the folders of the three profile patterns.
- Two existing Manual proofs that used `.ReverseMap()` as an example of "another option" (`CreateMap/options`, `MapCall/manual-profile`) now use `.MaxDepth(1)`; `ForMemberIgnore/reverse-map` and `CreateMap/member-list-reverse-map` became Fixed (output checked against the measured behaviour).
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there. `docs/projectinfo/architectuur/proofs.md` still has to be regenerated with `claude/tools/proofs-doc.py`.

## Validation results

| Check   | Command                                                     | Result   |
| ------- | ----------------------------------------------------------- | -------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors) |
| Tests   | `dotnet test`                                               | 1199/1199 passed |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed |
| Lint    | _none_                                                      | _not run_: no lint command configured |

## See also

- [automapper-reverse-map-plan.md](automapper-reverse-map-plan.md)
- [automapper-reverse-map-modellen.md](automapper-reverse-map-modellen.md)
- `claude/tasks/archief/TAAK-249-automapper-reverse-map.md`
