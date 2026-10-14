# TAAK-136 — MediatR: Mediator generator MSG0005 breaks the build with TreatWarningsAsErrors

**Status**: done (2026-10-11)
**Created**: 2026-10-11
**Found in**: `claude/tasks/CORPUSRUN-mediatr.md` (evolutionary-architecture ch. 1, simulated after TAAK-134)

## Problem

MediatR allows a notification without a handler (publish then does nothing). The Mediator 3 generator reports
`MSG0005 found message without any registered handler` (`OfferPrepareEvent`, `PassRegisteredEvent`: only handled in
integration tests). With `TreatWarningsAsErrors` (evo1, evo2, amantin, nadirbad, jason have it)
the build breaks after an otherwise successful migration.

## What, where, how

- **Where**: project edit of `MediatRFix` (where `Mediator.SourceGenerator` is added).
- **How**: add `MSG0005` to `<NoWarn>` (or `WarningsNotAsErrors`) in the host project, with a reason in the
  report; first check whether MSG0005 also occurs for requests without a handler (then it is a real runtime error: Manual).

## Steps

- [x] Record MSG0005 behaviour for notification vs request
- [x] NoWarn or Manual
- [x] `dotnet build` + `dotnet test` green

## Result

MSG0005 also occurs for a request without a handler (probe Mediator 3.0.2), but MediatR also only fails there at `Send`:
same behaviour, so no Manual. `AddedPackage.WarningsNotAsErrors`: when adding `Mediator.SourceGenerator`,
`XmlProjectFileStore` writes `<WarningsNotAsErrors>$(WarningsNotAsErrors);MSG0005</WarningsNotAsErrors>` in the same
file (last unconditional `PropertyGroup`, otherwise a new one). Chosen over `NoWarn`: the warning stays
visible. evo1: MSG0005 is now a warning.

## See also

- `claude/tasks/CORPUSRUN-mediatr.md`
