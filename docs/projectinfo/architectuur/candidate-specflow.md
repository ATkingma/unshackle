# Candidate fix: SpecFlow → Reqnroll

Scout report for TAAK-370 (claim #8), measured 2026-10-22. Goal: decide whether `specflow` is a worthwhile next fix.
Light survey only: web sources, shallow clones, regex counts over `*.cs`, **no builds**. Numbers are indicative.

## Recommendation

**Go (conditional on council), effort M, demand high.** SpecFlow is dead (no stable release since 2022-05, GitHub projects
deleted, no fixes) and Reqnroll is a maintained, BSD-3 drop-in fork with a documented, almost purely mechanical migration.
Unlike the license-driven fixes this is an EOL/abandonment fix; the council must accept that scope (see questions).

## Facts (primary sources)

| Fact | Source |
| ---- | ------ |
| Tricentis announced SpecFlow end-of-life in December 2024; EOL date 2024-12-31; from 2025-01-01 the SpecFlow GitHub projects are deleted and the support section of specflow.org is disabled | https://reqnroll.net/news/2025/01/specflow-end-of-life-has-been-announced/ |
| Existing NuGet packages keep working; possible later steps (unlisting packages, shutting docs) are unknown | same page |
| Reqnroll forked from SpecFlow in January 2024; positioned as the replacement | https://docs.reqnroll.net/latest/help/faq.html |
| Last stable SpecFlow: 3.9.74 (published 2022-05-03). Last package of any kind: 4.0.31-beta (2023-02-15) | NuGet registration API, checked 2026-10-22 |
| Reqnroll 3.3.4 published 2026-03-23, license expression `BSD-3-Clause`; `Reqnroll.SpecFlowCompatibility` 3.3.3 (2026-01-27) | NuGet registration API, checked 2026-10-22 |
| Official migration guide (two paths: compatibility package, or namespace change) | https://docs.reqnroll.net/latest/guides/migrating-from-specflow.html |

There is **no license trap**, only EOL: no fixes, no .NET 10 support from SpecFlow, VS extension and LivingDoc unmaintained.
README text must say so honestly.

## API distance

Reqnroll is based on SpecFlow v4, so it is a rename plus a few v3→v4 breaks (migration guide):

| Area | SpecFlow 3.9 | Reqnroll | Fixed / Manual |
| ---- | ------------ | -------- | -------------- |
| Packages | `SpecFlow`, `SpecFlow.NUnit/xUnit/MsTest`, `SpecFlow.Tools.MsBuild.Generation` | `Reqnroll.NUnit/xUnit/MsTest` (generator included) | Fixed (csproj + central props); generator package dropped |
| Namespace | `TechTalk.SpecFlow[.Assist\|.Infrastructure\|...]` | `Reqnroll[...]` (whole-word replace) | Fixed |
| Compat shortcut | | `Reqnroll.SpecFlowCompatibility` keeps `TechTalk.SpecFlow`; zero code change | Possible "level 1" fix, see questions |
| Attributes, hooks, `ScenarioContext`, `FeatureContext`, `Table` | | same names (`DataTable` alias is new) | Fixed (using only) |
| `BoDi.IObjectContainer` | `BoDi` | `Reqnroll.BoDi` | Fixed (using) |
| `ISpecFlowOutputHelper` and other `*SpecFlow*` type names | | `IReqnrollOutputHelper` etc. | Fixed (finite rename list) |
| Config | `specflow.json`, App.config `<specFlow>` | `reqnroll.json`: rename file, `stepAssemblies`→`bindingAssemblies`, `bindingCulture/name`→`language/binding`; App.config only via compat package | json Fixed (non-Roslyn edit); App.config Manual |
| Generated `*.feature.cs` | MSBuild generator | regenerated at build | checked-in ones: hint |
| Step text | regex | regex or Cucumber expression; text with `/` or `$` may change meaning | Manual flag |
| v3→v4 breaks | | string step-calls (`Given("...")` in a `Steps` class) removed; `async void` bindings forbidden; `ScenarioAndFeatureTags` deprecated for `CombinedTags`; AggregateException no longer swallowed | `Steps` + string calls Manual; `async void` Manual or Fixed to `async Task`; tags Fixed |
| MsTest scenario outlines | one method per example | one `[DataRow]` method (breaks VSTest filters) | Fixed by `generator/allowRowTests=false` in `reqnroll.json` |
| Plugins | `SpecFlow.Autofac`, `SpecFlow.Plus.LivingDocPlugin`, `SpecFlow.Allure`, `SpecRun`, `SpecFlow.Assist.Dynamic`, `SpecFlow.Actions.*` | DI-container and Actions ports exist; LivingDoc not included (per guide); SpecRun/Allure/Assist.Dynamic: no 1:1 found | Manual, one report line per package |

Not verified here: Reqnroll generator support for xUnit v3 and .NET 10 (check at step 1 of the task).

## Proof: how the user's own tests prove it

The user's scenarios are the proof: after the fix the **same `.feature` files and bindings must build (Reqnroll
generates the code-behind during build) and run green**. For us:

- `Proofs/SpecFlow/<Pattern>/*.before.cs` against SpecFlow 3.9.74, `*.after.cs` against Reqnroll 3.x, as for other fixes.
- Corpus harness: baseline build, apply fix, build and `dotnet test`, compare pass/fail counts. Many corpus repos are
  Selenium/Playwright UI suites that cannot run in CI; for those only "builds + same number of generated tests" is checkable.
  The corpus report must state that limit.
- Caveat: SpecFlow 3.9.74 predates .NET 10 and its generator is MSBuild-task based; some baselines may need an older SDK.

## Survey

13 repos from GitHub code search (`SpecFlow.NUnit|xUnit|MsTest` in `*.csproj`, 2026-10-22), cloned shallowly (HEAD at clone
time) into `<corpus>\corpus-candidates\specflow\src\<name>\`. Script and raw output:
`<corpus>\corpus-candidates\specflow\tools\measure.py`, `out.txt`. `*.feature.cs`, `*.g.cs`, `*.Designer.cs`, `bin`,
`obj`, comments excluded. Caveat: code search is biased toward small demos; company repos are private. "TT usings" =
`using TechTalk.SpecFlow[.sub]`; ".fcs" = checked-in `*.feature.cs`.

| Repo (sha, last commit) | SpecFlow pkgs | cs files | .feature | TT usings | `[Binding]` | step attrs | hooks | other notable |
| ----------------------- | ------------- | -------: | -------: | --------: | ----------: | ---------: | ----: | ------------- |
| Warewolf-ESB/Warewolf (`89e4a8f23b77`, 2025-09) | 3.9.22 MsTest | 4994 | 132 | 144 | 138 | 3990 | 105 | 8 App.config, 132 .fcs, 57 string step-calls, 1 `Steps` base, 18 `async void`, 84 Assist calls |
| AutomateThePlanet/AutomateThePlanet-Learning-Series (`52787545e090`, 2024-07) | 3.5.14 MsTest + Assist.Dynamic | 1609 | 5 | 36 | 28 | 75 | 37 | App.config, 9 Assist, 1 `[StepArgumentTransformation]` |
| SkillsFundingAgency/das-learning (`ba3d92376be9`, 2026-10) | 3.9.74 NUnit + LivingDoc | 369 | 30 | 1 | 18 | 176 | 5 | 49 `ScenarioContext`, 53 `Table` |
| toniarnold/aspnettest (`d2e1e0caf315`, 2023-02) | 3.9.74 NUnit + LivingDoc | 405 | 5 | 3 | 2 | 30 | 3 | 25 string-call hits (upper bound), 6 Assist |
| godrose/Solid (`2a018b905bd7`, 2022-11) | 3.1.97 / 3.9.74 xUnit + LivingDoc | 192 | 18 | 43 | 21 | 89 | 6 | 9 `specflow.json`, 18 `IObjectContainer`, 37 `ScenarioContext` |
| ERNI-Academy/net6-automation-testware (`f3b2250a6564`, 2024-05) | 3.9.74 MsTest + SpecFlow.Allure | 192 | 11 | 6 | 25 | 100 | 48 | 4 `specflow.json`, 16 string-call hits, 4 `async void` |
| nationalarchives/discovery-bdd-test (`7c8dd1e746a9`, 2022-11) | 3.0 to 3.3 NUnit | 72 | 47 | 60 | 60 | 591 | 0 | 12 `specflow.json`, 47 .fcs |
| matthewrenze/clean-architecture-demo (`620d3d9f1377`, 2025-02) | 3.9.74 NUnit + LivingDoc | 89 | 6 | 12 | 6 | 14 | 0 | 25 Assist calls |
| executeautomation/Playwrightdotnet (`e098bf7da0d0`, 2023-11) | 3.9.74 NUnit + Assist.Dynamic + LivingDoc | 16 | 2 | 1 | 2 | 4 | 0 | |
| hippo-digital/playwright-specflow-pom-dotnet (`ba2de1ff2057`, 2024-01) | 3.9.22 NUnit + LivingDoc | 5 | 1 | 0 (implicit usings) | 3 | 3 | 1 | |
| LambdaTest/SpecFlow-Selenium-Sample (`b630598f496a`, 2026-10) | packages.config | 3 | 1 | 3 | 2 | 7 | 2 | App.config, 15 `ScenarioContext` |
| manu1rock/RestSharpDemo (`cd4e67038ed2`, 2022-02) | packages.config | 15 | 1 | 1 | 1 | 4 | 0 | |
| eMerchantPay/dotnet_qa_automation_sample (`d61a1086fe03`, 2021-04) | packages.config | 5 | 1 | 1 | 1 | 0 | 0 | |

Findings:

- **Dominant shape: `using TechTalk.SpecFlow;` plus attributes.** Package swap plus using rename covers every repo except where the
  rare shapes below occur. Step and hook bodies need no change.
- Rare but real: `Steps` base with string step-calls (Warewolf), `async void` (Warewolf 18, testware 4), App.config (4 of 13),
  `specflow.json` (3 of 13).
- **Plugins are the real friction**: 6 of 13 use `SpecFlow.Plus.LivingDocPlugin` (no Reqnroll port), others `SpecFlow.Allure` and
  `SpecFlow.Assist.Dynamic`. These become Manual report lines.
- 3 repos use `packages.config`/old-style projects, so the package swap needs that path or is Manual.
- Checked-in `*.feature.cs` (Warewolf, discovery, ATP) must be regenerated; stale ones reference `TechTalk.SpecFlow`.
- Unlike FA or Moq, the whole test project is what is migrated; no production code is touched.

## Demand

NuGet total downloads (search API, 2026-10-22):

| Package | Downloads |
| ------- | --------: |
| SpecFlow | 119.8 M |
| SpecFlow.Tools.MsBuild.Generation | 91.9 M |
| SpecFlow.NUnit | 38.3 M |
| SpecFlow.xUnit | 25.1 M |
| SpecFlow.Plus.LivingDocPlugin | 24.4 M |
| SpecFlow.MsTest | 12.1 M |
| Reqnroll | 28.9 M |
| Reqnroll.NUnit / xUnit / MSTest | 10.6 M / 7.2 M / 3.3 M |
| Reqnroll.SpecFlowCompatibility | 1.9 M |

Reqnroll already has about a quarter of SpecFlow's lifetime total, so migration happens organically; the long tail
(enterprise suites like Warewolf) remains. Demand is below Moq/AutoMapper but sits with QA-automation teams that rarely have
Roslyn-literate maintainers. The manual route (package swap plus find-and-replace) is already easy, which limits what a tool
adds. The value is mostly the **report** (which plugins and breaks block you) and the safe config migration.

## Effort estimate: M

- S part: `SpecFlowFix` with three patterns (package swap, using/namespace rename, type renames) plus proofs, reusing the
  package-swap and using-swap infrastructure of earlier fixes.
- M driver: `specflow.json` to `reqnroll.json` (non-C# edit, key renames, `allowRowTests`), App.config detection (Manual),
  plugin report. If the Fixer has no concept of non-.cs files, that is new core work.
- Schedule risk: getting baseline builds of SpecFlow 3.x on a current SDK.

## Top risks

1. Scope: EOL, not license; the council must accept "dead dependency" as a trigger.
2. Plugins without a Reqnroll equivalent (LivingDoc: 6 of 13 repos) leave many repos only partially migrated.
3. Behaviour proof for UI suites is weak (cannot run in CI): only build plus discovered-test count.
4. Cucumber-expression detection can silently change step matching for text with `/` or `$`.
5. SpecFlow 3.9.74 baseline may not build on the .NET 10 SDK; Reqnroll xUnit v3 / .NET 10 support unverified.
6. Config and `.feature` files are not Roslyn: needs a non-C# edit path.

## Council questions

1. Does an EOL/abandonment fix (no license change) belong in Unshackle's scope, or only after the four planned fixes?
2. Level 1 (swap packages plus `Reqnroll.SpecFlowCompatibility`, no code change) first, with the full namespace rename as level 2, or straight to level 2?
3. Is rewriting `specflow.json`, `.csproj` and `packages.config` in scope for the Fixer, or report-only?
4. Plugins without a port (LivingDoc, Allure, SpecRun): a Manual line, or block the fix for that project?
5. Proof bar for UI suites that cannot run in CI: is "builds, same number of generated tests" enough?

## Draft follow-up task (taak-start format)

```
# TAAK-371 — SpecFlow → Reqnroll (candidate fix)
**Status**: step 0/6 — not started
**Goal**: A `specflow` fix that migrates SpecFlow 3.x projects to Reqnroll (packages, namespaces, config), 100% on a SpecFlow corpus.
Steps: 1 survey with builds (10-15 repos, baselines) -> corpus-specflow.md; 2 council (questions above);
3 SpecFlowFix skeleton: package swap + using rename + type renames, proofs; 4 reqnroll.json migration + plugin/App.config/Steps Manual report;
5 corpus round (build + test counts equal before/after); 6 blind round, roadmap row, README text.
```

## See also

- `docs/projectinfo/architectuur/corpus-automapper.md` — model of a survey
- `claude/tasks/TAAK-370-scout-specflow.md` — this scout task
- `claude/tasks/TAAK-307-moq-to-nsubstitute.md` — the route a new fix follows
- `docs/projectinfo/project.md` — roadmap
