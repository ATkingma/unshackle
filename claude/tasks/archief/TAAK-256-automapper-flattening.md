# TAAK-256 — automapper.flattening

**Status**: ✅ completed
**Created**: 2026-10-14
**Goal**: row 9 of `claude/tasks/automapper-queue.md`: a destination member without a source member of its exact name gets the value AutoMapper 14 reads for it (another case, `GetX()`, flattening `Customer.Name` → `CustomerName`), only where the match is unambiguous and equal to AutoMapper's.

## Context for the next agent

- Before this task `CreateMapPattern.Problem` returned "has no member with the same name" for every such member (proof `CreateMap/flattening` was Manual).
- Like TAAK-245..251 this is a capability of the shared profile rewrite, not a pattern class.
- Corpus (TAAK-244): 13 members in 3 projects (kavita `FullAnnotationDto.LibraryName`/`SeriesName`, nop admin models, cats); the proof `CreateMap/flattening-nullable` follows the kavita shape.

## Evidence (AutoMapper 14.0.0, file-based `dotnet run` in a scratch folder, `PropertyMaps` read through `Internal().FindTypeMapFor`)

| Case | Result |
| --- | --- |
| `CustomerName` with `Customer.Name` | `Customer.Name`; null `Customer` → `null` (overwrites the initializer) |
| `CustomerAddressCity` | `Customer.Address.City`, null on the way → `null` |
| `CustomerAge` (`int`), null `Customer` | `0` |
| `CustomerAge` into `string`, null `Customer` | `"0"` (default, then converted) |
| `CustomerAge` into `int?`, null `Customer` | `null` |
| `CustomerWhen` (`int?` leaf) into `int` | `0` for a null leaf |
| `Total` property and `GetTotal()` | `Total`; a field `Total` also beats `GetTotal()` |
| only `GetSum()` / property `GetCode` | `Sum` ← `GetSum()`, `Code` ← `GetCode` (prefix on any member) |
| method `Total()` | `Total` ← `Total()` |
| `CustomerName` with `CustomerName` and `Customer.Name` | the direct member |
| `CustomerAddressCity` with `Customer.Address` and `CustomerAddress` | `Customer.Address.City` (shortest prefix first) |
| `CustomerName2X` with `Customer`, `CustomerName2` | `CustomerName2.X` (backtracking) |
| `NAME` with `Name` and field `name` | `Name` (one of them) |
| `customerName` / `total` | matched in any case |
| `CustomerFull` / `BuyerName` | `Customer.GetFull()` / `GetBuyer().Name` (methods on the way) |
| `PX` with `P? P` (struct) | not mapped (configuration invalid) |
| `PointX` with struct `Point` | `Point.X` |
| `NameTrim` | `Name.Trim()` |

## Choices

- Two members matching one name, or two complete paths → Manual (AutoMapper picks one; the rewrite does not guess).
- Value leaf behind a possibly null object into a `Nullable<U>` of another type → Manual (not measured).
- Reverse maps keep the old Manual for non-exact names; forward flattening with `ReverseMap()` stays Manual through the TAAK-249 unflattening check.
- Constructor parameters stay Manual (TAAK-251 message).

## Steps

- [x] Measure AutoMapper 14.0.0
- [x] `Flattening.cs`, `CreateMapPattern.FlattenedProblem`, `MemberValues.NullableWarnings`
- [x] Proofs `CreateMap/flattening*` (5 after, 3 manual), `_Combined/flattening-map-call`, unit tests
- [x] Plan, models, verify in `docs/projectinfo/patterns/automapper-flattening-*`
- [x] `dotnet build`, `dotnet test` (1319), SampleShop (38) green

## See also

- `docs/projectinfo/patterns/automapper-flattening-plan.md`
- `docs/projectinfo/patterns/automapper-flattening-verify.md`
- `claude/tasks/archief/TAAK-249-automapper-reverse-map.md`
