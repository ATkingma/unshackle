# Models: automapper.unsupported

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/UnsupportedOptions.cs` and the hooks in `CreateMapPattern.cs`. No rendered figures (no `kingmadoc`/`d2`); the models are tables.

## Classes

| Element | Role |
| --- | --- |
| `CreateMapPattern` | Shared profile rewrite; `TryParseOptions` skips droppable options, `Rewrite` checks depth options and builds the Manual reason |
| `UnsupportedOptions` | `IsDepthOption` (`MaxDepth(constant)`, `PreserveReferences()`), `IsDoNotValidate`, `DepthOption` (the depth option in a statement), `Reason` (API → advice) |
| `ForMemberIgnorePattern`, `ForMemberMapFromPattern` | Inherit the rewrite, so they get the same drops and reasons |
| `PatternResult` | `Fixed` (option dropped) or `Manual` (specific reason, suffixed `(automapper.unsupported)`) |

## Sequence

| Step | Method | What happens |
| --- | --- | --- |
| 1 | `CreateMapPattern.Rewrite` | per constructor statement calls `TryParseMap` |
| 2 | `TryParseOptions` | walks the chain outside-in; skips `DoNotValidate`/depth options, parses `ForMember` |
| 3 | `Rewrite` (parse failed) | `UnsupportedOptions.Reason(statement)` → specific Manual, or the generic text when it names nothing |
| 4 | `Problem` | computes members and `copied` |
| 5 | `Rewrite` | `DepthOption(statement)` with `copied` or a `ReverseMap` → Manual; else the option is gone |

## Decision tree

| Question | Yes | No |
| --- | --- | --- |
| `ForSourceMember(s => s.X, o => o.DoNotValidate())`? | dropped | next |
| `MaxDepth(constant)` / `PreserveReferences()`? | nested copy or `ReverseMap` → Manual; else dropped | next |
| API in the advice table (chain innermost first)? | Manual with its advice | next |
| `ForMember` with an unread option or form? | Manual naming it | next |
| Assignment to an AutoMapper profile property? | Manual naming the setting | generic Manual |

## See also

- [automapper-unsupported-plan.md](automapper-unsupported-plan.md)
- [automapper-unsupported-verify.md](automapper-unsupported-verify.md)
