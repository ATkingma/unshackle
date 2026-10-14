# TAAK-009 — Pattern fa.be-equivalent-to-string

**Status**: ✅ done
**Created**: 2026-10-11
**Goal**: `s.Should().BeEquivalentTo("x")` on a string becomes `s.ShouldBe("x", StringCompareShould.IgnoreCase)`, with proofs, green in `dotnet test`.

## Context for the next agent

- Row 9 in `claude/tasks/patroon-queue.md`. Example: TAAK-007. Sample: `OrderTests.cs:51`. `OrderHandlerTests.cs:30` (object) is row 14.

## Analysis

FA 7.2.2 `StringAssertions.BeEquivalentTo(string, because, becauseArgs)`: only case is ignored (ordinal); leading/trailing whitespace, `\r\n` vs `\n`, İ/i and ß/SS differ. No `options =>` overload for strings. Shouldly `ShouldBe(expected, StringCompareShould.IgnoreCase)` behaves identically (test program). Shouldly overload with because: `ShouldBe(expected, customMessage, StringCompareShould)`. `ShouldNotBe` has no IgnoreCase overload.

## Choices

| # | Choice | Reason |
|---|---|---|
| 1 | Find filters on `StringAssertions` | `BeEquivalentTo` on objects/collections is row 14 |
| 2 | `because` goes between expected and `StringCompareShould.IgnoreCase` | Only existing overload order |
| 3 | `NotBeEquivalentTo` Manual | No Shouldly equivalent |
| 4 | Manual reasons: "no Shouldly equivalent", "becauseArgs", "is used", "is not in the form" | `ProofTests` requires one reason per proof |

## Steps

- [x] 1. Claim and create task
- [x] 2. Research and choices
- [x] 3. KingmaDoc plan (7 REQ)
- [x] 4. `BeEquivalentToStringPattern` + registration
- [x] 5. Proofs (13 files in `BeEquivalentToString/`)
- [x] 6. Build/test green (98 + 15 sample), verify, archive, row `klaar`

## Changed files

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToStringPattern.cs` (new)
- `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` (1 line)
- `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToString/`
- `docs/projectinfo/patterns/fa-be-equivalent-to-string-{plan,verify}.md`, `docs/projectinfo/README.md` (row)

## See also

- `claude/tasks/patroon-queue.md`
- `claude/tasks/archief/TAAK-007-fa-contain.md`
