# Council round 23 — SpecFlow step expressions and specflow.json

Date: 2026-10-28. Perspectives: Guardian (G), Pragmatist (P), Architect (A); see `claude/docs/council.md`. Three separate sub-agents
gave positions. Round 1 agreed on pinning regex texts (S13) and split on non-literal texts (P: pin anyway; G, A: Manual) and on Q13
(G, P: keep the name; A: rename together with the csproj items, keep the name as fallback). Round 2 put a joint proposal per
question; all three accepted, with the amendments folded into the rules below (A: a const counts only with a compile-time value; P: count
backslashes on the cooked value; A: the Info row names the VS up-to-date gap). G asked to pin when the detector "cannot decide"; the copied
detector is deterministic, so that case does not arise.
Questions: council questions 12 and 13 in `claude/tasks/TAAK-340-specflow-to-reqnroll.md`.
Follow-ups: TAAK-340 step 6 (S13), step 5 (S14).

Facts checked in the decompiled packages (`Reqnroll.dll` 3.3.4, `Reqnroll.Generator.dll` from `Reqnroll.Tools.MsBuild.Generation`
3.3.4, `TechTalk.SpecFlow.dll` 3.9.74; under `/mnt/dikkeschijf/unshackle-corpus/tmp/`):

- `Reqnroll.ExpressionType { Unspecified, CucumberExpression, RegularExpression }`; `StepDefinitionBaseAttribute.ExpressionType { get; set; }`.
- `BindingFactory`: `RegularExpression` always builds `RegexStepDefinitionBindingBuilder` (detector skipped); `RegexOptions.CultureInvariant`, as SpecFlow.
- Anchoring: SpecFlow always wraps `^` + text + `$`; Reqnroll adds `^` / `$` only when the text does not already start / end with it.
  Same meaning (`^^`, `$$` are idempotent) except a text ending in an escaped dollar (odd backslash run before the final `$`): Reqnroll leaves the end unanchored.
- Runtime `ReqnrollJsonLocator`: `reqnroll.json`, else `specflow.json` (bin dir, then current dir). Generator `ProjectReader`: `reqnroll.json`,
  else `specflow.json`, else `app.config`. The generator props register only `reqnroll.json` as a VS `UpToDateCheckInput`.

| # | Decision | Joint rule |
| --- | --- | --- |
| S13 | Cucumber-read step texts with regex characters (Q12) | Fixed + Info: add `ExpressionType = ExpressionType.RegularExpression` when the attribute is `Given`/`When`/`Then`/`StepDefinition`, the first argument is a string literal or a constant whose compile-time string value the semantic model gives, no `ExpressionType` is set, the copied detector says Cucumber and the text holds a regex character. Info row names the text: "kept as regex via ExpressionType.RegularExpression". Manual stays for a text ending in an escaped `$` (odd backslash run before the final `$`, counted on the cooked value; anchoring differs) and for interpolated or unreadable texts. Proofs include the escaped-dollar Manual case. |
| S14 | `specflow.json` rename (Q13) | Keep the name; drop the rename edit (no file change, G5 trivially met). Info row: "specflow.json kept (Reqnroll reads it); rename by hand together with its csproj items; the generator lists only reqnroll.json for VS up-to-date checks". A rename edit that also moves `specflow.json` csproj items is backlog, only with a proof. |

## S13 — step expressions

- G: SpecFlow texts were always regex, so pinning preserves behaviour; only the escaped-dollar case changes meaning, so it stays Manual.
- P: a trailing `.` must not block a whole solution under AllOrNothing; pin rather than detect edge cases.
- A: reuse the copied detector so the rule fires exactly where Reqnroll would misread; non-literal text is Manual as elsewhere in the engine.

## S14 — specflow.json

- G: renaming without moving `CopyToOutputDirectory` items silently drops the config from bin.
- P: both runtime and generator read the old name; zero work, same behaviour.
- A: S3 allows a known config-file rename, but only with the csproj items in the same edit; keeping the name is the accepted fallback.

## See also

- `claude/docs/council.md` — summary of all rounds
- `claude/tasks/COUNCIL-round-22.md`
- `claude/tasks/TAAK-340-specflow-to-reqnroll.md`
