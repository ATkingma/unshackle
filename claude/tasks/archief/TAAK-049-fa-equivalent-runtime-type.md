# TAAK-049 — fa.be-equivalent-to-object: Shouldly requires an equal runtime type

**Status**: completed (2026-10-11)
**Created**: 2026-10-11
**Found in**: TAAK-046 (semantic audit)

## Problem

`BeEquivalentToObjectPattern` checks that subject and expectation have the same **static** type. Shouldly's `ShouldBeEquivalentTo`, however, compares the **runtime** type; FA only compares the members of the expectation.

Measured (`scratchpad/semprobe`): `Pt a = new Pt2 { X = 1, Y = 2 }; Pt b = new Pt { X = 1 };`
- `a.Should().BeEquivalentTo(b)` PASS
- `a.ShouldBeEquivalentTo(b)` FAIL

Consequence: a green test turns red when the subject is a derived instance (e.g. a factory/deserializer that returns a subtype). Not hit in the case (`SimpleObject` is not derived).

## What, where, how

- **Where**: `Unsupported(...)` in `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`.
- **How**: only Fixed if the static type is `sealed` or a struct (then the runtime type cannot deviate), otherwise Manual with the reason "The subject's runtime type may derive from {type}: Shouldly requires equal runtime types, FluentAssertions compares only the expectation's members." Also applies to nested members with a class type (Shouldly also compares types recursively; measure first).
- **Trade-off**: this makes many current Fixed cases Manual (ordinary non-sealed classes). Alternative: only warn in the docs. Measure how many of the 3 case Fixeds then become Manual, and decide.

## Steps

- [x] Probe: nested member with a derived runtime instance (FA vs Shouldly)
- [x] Choice: Manual for non-sealed classes, or document
- [x] Update proofs + `docs/projectinfo/patterns/`
- [x] `dotnet build` + `dotnet test` green

## User decision

Non-sealed types do **not** become Manual: the rewrite stays Fixed, with a warning in the report ("ShouldBeEquivalentTo fails when the runtime type differs; FluentAssertions accepted derived types").

## Result

Probe: a nested derived member and both derived with a different extra member: FA PASS, Shouldly FAIL. `FixedResult` got an optional `Warning` (init); `ReportRow` puts it in the Reason column (Markdown/Json/Csv), the console summary counts "with a warning". No warning for sealed/struct (also nested) or when both sides are a `new` expression. Unit tests `Core/BeEquivalentToObjectPatternTests.cs` and `ReportWriterTests.From_FixedWithWarning_PutsWarningInReason`; proofs `sealed.*` and `both-new.*`. SampleShop unchanged.

## See also

- `claude/tasks/archief/TAAK-046-semantische-audit.md`
