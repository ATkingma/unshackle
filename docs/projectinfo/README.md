# Unshackle — project info

Main document of the project documentation. The overview and the C4 diagrams are here; details are in separate
documents, and every pattern has its own plan and verification (KingmaDoc) in `patterns/`.

## Documents

| Document | Contents |
| -------- | -------- |
| [project.md](project.md) | Idea, problem, existing tools, CLI, examples, roadmap, risks |
| [architectuur/projecten.md](architectuur/projecten.md) | Projects, dependencies, build settings |
| [architectuur/core.md](architectuur/core.md) | Fixer → Fix → Pattern, as designed and as built, SOLID |
| [architectuur/testen.md](architectuur/testen.md) | Proofs, types without building, mocking, samples |
| [architectuur/rapportage.md](architectuur/rapportage.md) | Report formats |
| [beslissingen.md](beslissingen.md) | Code style and what still has to be decided |
| [features/](features/end-to-end-keten-plan.md) | KingmaDoc plan + verify per feature (not a pattern), e.g. [end-to-end-keten](features/end-to-end-keten-plan.md) ([verify](features/end-to-end-keten-verify.md)), [projectbestanden-migreren](features/projectbestanden-migreren-plan.md) ([verify](features/projectbestanden-migreren-verify.md)), [mediatr-fix](features/mediatr-fix-plan.md) ([verify](features/mediatr-fix-verify.md)), [gelijknamige-typen-swap](features/gelijknamige-typen-swap-plan.md) ([verify](features/gelijknamige-typen-swap-verify.md)), [automapper-partial-migration](features/automapper-partial-migration-plan.md) ([models](features/automapper-partial-migration-modellen.md), [verify](features/automapper-partial-migration-verify.md)), [automapper-mocked-imapper](features/automapper-mocked-imapper-plan.md) ([models](features/automapper-mocked-imapper-modellen.md), [verify](features/automapper-mocked-imapper-verify.md)), [mediatr-partial-migration](features/mediatr-partial-migration-plan.md) ([verify](features/mediatr-partial-migration-verify.md)), [using-removal-edge-cases](features/using-removal-edge-cases-plan.md) ([models](features/using-removal-edge-cases-modellen.md), [verify](features/using-removal-edge-cases-verify.md)) |

## C4 Context

**Figure 1.** Who uses Unshackle and what it depends on.

<!-- kingmadoc:diagram img/README-1.d2 -->

![C4 Context](img/README-1.png)

| Element | Type | Role |
| ------- | ---- | ---- |
| Developer | Person | Runs the tool on their solution |
| Unshackle | System | `src/`: `dotnet tool` with scan, fix, list |
| User's solution | External | `.sln`/`.slnx` that is read and rewritten |
| MSBuild / .NET SDK | External | Loads projects and references |
| git | External | `fix` checks whether the working tree is clean |

## C4 Container

**Figure 2.** The projects in `src/`. The test project is not included.

<!-- kingmadoc:diagram img/README-2.d2 -->

![C4 Container](img/README-2.png)

| Container | Path | Role |
| --------- | ---- | ---- |
| Unshackle.Interface | `src/Unshackle.Interface/` | `dotnet tool` `unshackle`; composition root in `Cli.cs`, commands, console |
| Unshackle.Core | `src/Unshackle.Core/` | Fixer, fixes, patterns |
| Unshackle.DataAccess | `src/Unshackle.DataAccess/` | MSBuild loading, project-file editing and saving, reports (CSV/JSON/Markdown), git status |

## C4 Component: Unshackle.Core

**Figure 3.** Parts of Core. Contracts and models are in [modellen/klassen.md](modellen/klassen.md).

<!-- kingmadoc:diagram img/README-3.d2 -->

![C4 Component: Unshackle.Core](img/README-3.png)

| Component | Path | Role |
| --------- | ---- | ---- |
| Fixer | `src/Unshackle.Core/Fixer.cs` | See [modellen/fix-flow.md](modellen/fix-flow.md) |
| UsingDirectives | `src/Unshackle.Core/UsingDirectives.cs` | Swaps usings (top level, inside namespaces, global usings) |
| FluentAssertionsFix | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs` | Patterns in a fixed order |
| MediatRFix | `src/Unshackle.Core/Fixes/MediatR/MediatRFix.cs` | MediatR 12.5.0 → Mediator 3.0.2; patterns follow (see [features/mediatr-fix-plan.md](features/mediatr-fix-plan.md)) |
| AutoMapperFix | `src/Unshackle.Core/Fixes/AutoMapper/AutoMapperFix.cs` | AutoMapper 14.0.0 → hand-written `ToB()` extension methods |
| FluentAssertionsSyntax | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs` | Shared building blocks |
| Patterns | `src/Unshackle.Core/Fixes/*/Patterns/` | See [modellen/patronen.md](modellen/patronen.md) and the pattern table below |

## C4 Component: Unshackle.Interface and Unshackle.DataAccess

**Figure 4.** The chain from command to file. Exit codes: 0 = ok (also with Manual), 1 = load or write error or dirty working tree, 2 = invalid arguments.

<!-- kingmadoc:diagram img/README-4.d2 -->

![C4 Component: Unshackle.Interface and Unshackle.DataAccess](img/README-4.png)

| Component | Path | Role |
| --------- | ---- | ---- |
| Cli | `src/Unshackle.Interface/Cli.cs` | Composition root, command tree, parse error → 2 |
| ScanCommand / FixCommand / ListCommand | `src/Unshackle.Interface/Commands/` | See [features/end-to-end-keten-plan.md](features/end-to-end-keten-plan.md) |
| SpectreConsoleView, LineDiff | `src/Unshackle.Interface/Output/` | Tables and line diff; errors to stderr |
| MsBuildSolutionStore | `src/Unshackle.DataAccess/Solutions/` | Loading with one `MSBuildWorkspace` (writing goes through `XmlProjectFileStore.SaveAsync`) |
| XmlProjectFileStore | `src/Unshackle.DataAccess/Projects/` | Project-file edits and the one checked save of source and project files |
| Report writers | `src/Unshackle.DataAccess/Reports/` | CSV, JSON, Markdown |
| GitSourceControl | `src/Unshackle.DataAccess/SourceControl/` | `git status --porcelain` |

## Models

| Model | Document |
| ----- | -------- |
| UML class diagram: contracts, models, I/O contracts and implementations | [modellen/klassen.md](modellen/klassen.md) |
| UML sequence: `FixAsync` | [modellen/fix-flow.md](modellen/fix-flow.md) |
| UML activity: Fixed or Manual | [modellen/pattern-beslissing.md](modellen/pattern-beslissing.md) |
| Overview of the FA patterns in groups | [modellen/patronen.md](modellen/patronen.md) |

## Patterns

Every pattern has a KingmaDoc plan (before the build) and verification (after the build) in `patterns/`.
Queue and status: `claude/tasks/patroon-queue.md`.

| Pattern ID | Plan | Models | Verification | Task |
| ---------- | ---- | ------ | ------------ | ---- |
| `fa.should-be` | [plan](patterns/fa-should-be-plan.md) | [models](patterns/fa-should-be-modellen.md) | [verify](patterns/fa-should-be-verify.md) | TAAK-001 |
| `fa.discard-assignment` | [plan](patterns/fa-discard-assignment-plan.md) | [models](patterns/fa-discard-assignment-modellen.md) | [verify](patterns/fa-discard-assignment-verify.md) | TAAK-277 |
| `fa.invoking-local` | [plan](patterns/fa-invoking-local-plan.md) | [models](patterns/fa-invoking-local-modellen.md) | [verify](patterns/fa-invoking-local-verify.md) | TAAK-287 |
| `fa.have-count` | [plan](patterns/fa-have-count-plan.md) | [models](patterns/fa-have-count-modellen.md) | [verify](patterns/fa-have-count-verify.md) | TAAK-002 |
| `fa.be-true-false` | [plan](patterns/fa-be-true-false-plan.md) | [models](patterns/fa-be-true-false-modellen.md) | [verify](patterns/fa-be-true-false-verify.md) | TAAK-003 |
| `fa.be-null` | [plan](patterns/fa-be-null-plan.md) | [models](patterns/fa-be-null-modellen.md) | [verify](patterns/fa-be-null-verify.md) | TAAK-004 |
| `fa.be-empty` | [plan](patterns/fa-be-empty-plan.md) | [models](patterns/fa-be-empty-modellen.md) | [verify](patterns/fa-be-empty-verify.md) | TAAK-006 |
| `fa.contain` | [plan](patterns/fa-contain-plan.md) | [models](patterns/fa-contain-modellen.md) | [verify](patterns/fa-contain-verify.md) | TAAK-007 |
| `fa.contain-key` | [plan](patterns/fa-contain-key-plan.md) | [models](patterns/fa-contain-key-modellen.md) | [verify](patterns/fa-contain-key-verify.md) | TAAK-013, TAAK-168, TAAK-171 |
| `fa.throw` | [plan](patterns/fa-throw-plan.md) | [models](patterns/fa-throw-modellen.md) | [verify](patterns/fa-throw-verify.md) | TAAK-008 |
| `fa.throw-async` | [plan](patterns/fa-throw-async-plan.md) | [models](patterns/fa-throw-async-modellen.md) | [verify](patterns/fa-throw-async-verify.md) | TAAK-012 |
| `fa.not-throw` | [plan](patterns/fa-not-throw-plan.md) | [models](patterns/fa-not-throw-modellen.md) | [verify](patterns/fa-not-throw-verify.md) | TAAK-011 |
| `fa.be-equivalent-to-string` | [plan](patterns/fa-be-equivalent-to-string-plan.md) | [models](patterns/fa-be-equivalent-to-string-modellen.md) | [verify](patterns/fa-be-equivalent-to-string-verify.md) | TAAK-009 |
| `fa.be-equivalent-to-object` | [plan](patterns/fa-be-equivalent-to-object-plan.md) | [models](patterns/fa-be-equivalent-to-object-modellen.md) | [verify](patterns/fa-be-equivalent-to-object-verify.md) | TAAK-016 |
| `fa.be-equivalent-to-object` (collection) | [plan](patterns/fa-be-equivalent-to-collection-plan.md) | [models](patterns/fa-be-equivalent-to-collection-modellen.md) | [verify](patterns/fa-be-equivalent-to-collection-verify.md) | TAAK-172 |
| `fa.equivalent-options` | [plan](patterns/fa-equivalent-options-plan.md) | [models](patterns/fa-equivalent-options-modellen.md) | [verify](patterns/fa-equivalent-options-verify.md) | TAAK-163 |
| `fa.contain-equivalent-of` | [plan](patterns/fa-contain-equivalent-of-plan.md) | [models](patterns/fa-contain-equivalent-of-modellen.md) | [verify](patterns/fa-contain-equivalent-of-verify.md) | TAAK-164 |
| `fa.be-greater-less` | [plan](patterns/fa-be-greater-less-plan.md) | [models](patterns/fa-be-greater-less-modellen.md) | [verify](patterns/fa-be-greater-less-verify.md) | TAAK-010 |
| `fa.only-contain` | [plan](patterns/fa-only-contain-plan.md) | [models](patterns/fa-only-contain-modellen.md) | [verify](patterns/fa-only-contain-verify.md) | TAAK-015 |
| `fa.all-satisfy` | [plan](patterns/fa-all-satisfy-plan.md) | [models](patterns/fa-all-satisfy-modellen.md) | [verify](patterns/fa-all-satisfy-verify.md) | TAAK-014 |
| `fa.assertion-scope` | [plan](patterns/fa-assertion-scope-plan.md) | [models](patterns/fa-assertion-scope-modellen.md) | [verify](patterns/fa-assertion-scope-verify.md) | TAAK-031 |
| `fa.custom-assertion` | [plan](patterns/fa-custom-assertion-plan.md) | [models](patterns/fa-custom-assertion-modellen.md) | [verify](patterns/fa-custom-assertion-verify.md) | TAAK-091, TAAK-275 |
| `fa.catch-assertion-exception` | [plan](patterns/fa-catch-assertion-exception-plan.md) | [models](patterns/fa-catch-assertion-exception-modellen.md) | [verify](patterns/fa-catch-assertion-exception-verify.md) | TAAK-140 |
| `fa.timespan-extensions` | [plan](patterns/fa-timespan-extensions-plan.md) | [models](patterns/fa-timespan-extensions-modellen.md) | [verify](patterns/fa-timespan-extensions-verify.md) | TAAK-224 |
| `fa.not-be` | [plan](patterns/fa-not-be-plan.md) | [models](patterns/fa-not-be-modellen.md) | [verify](patterns/fa-not-be-verify.md) | TAAK-032 |
| `fa.and-chain` | [plan](patterns/fa-and-chain-plan.md) | [models](patterns/fa-and-chain-modellen.md) | [verify](patterns/fa-and-chain-verify.md) | TAAK-033, TAAK-171 |
| `fa.be-of-type` | [plan](patterns/fa-be-of-type-plan.md) | [models](patterns/fa-be-of-type-modellen.md) | [verify](patterns/fa-be-of-type-verify.md) | TAAK-034 |
| `fa.string-start-end` | [plan](patterns/fa-string-start-end-plan.md) | [models](patterns/fa-string-start-end-modellen.md) | [verify](patterns/fa-string-start-end-verify.md) | TAAK-035 |
| `fa.contain-single-which` | [plan](patterns/fa-contain-single-which-plan.md) | [models](patterns/fa-contain-single-which-modellen.md) | [verify](patterns/fa-contain-single-which-verify.md) | TAAK-036 |
| `fa.equal-sequence` | [plan](patterns/fa-equal-sequence-plan.md) | [models](patterns/fa-equal-sequence-modellen.md) | [verify](patterns/fa-equal-sequence-verify.md) | TAAK-038 |
| `fa.not-equal` | [plan](patterns/fa-not-equal-plan.md) | [models](patterns/fa-not-equal-modellen.md) | [verify](patterns/fa-not-equal-verify.md) | TAAK-160 |
| `fa.not-contain-nulls` | [plan](patterns/fa-not-contain-nulls-plan.md) | [models](patterns/fa-not-contain-nulls-modellen.md) | [verify](patterns/fa-not-contain-nulls-verify.md) | TAAK-161 |
| `fa.order` | [plan](patterns/fa-order-plan.md) | [models](patterns/fa-order-modellen.md) | [verify](patterns/fa-order-verify.md) | TAAK-162 |
| `fa.have-length` | [plan](patterns/fa-have-length-plan.md) | [models](patterns/fa-have-length-modellen.md) | [verify](patterns/fa-have-length-verify.md) | TAAK-039 |
| `fa.be-approximately` | [plan](patterns/fa-be-approximately-plan.md) | [models](patterns/fa-be-approximately-modellen.md) | [verify](patterns/fa-be-approximately-verify.md) | TAAK-050 |
| `fa.throw-exactly` | [plan](patterns/fa-throw-exactly-plan.md) | [models](patterns/fa-throw-exactly-modellen.md) | [verify](patterns/fa-throw-exactly-verify.md) | TAAK-051 |
| `fa.invoking` | [plan](patterns/fa-invoking-plan.md) | [models](patterns/fa-invoking-modellen.md) | [verify](patterns/fa-invoking-verify.md) | TAAK-052 |
| `mediatr.request-handler` | [plan](patterns/mediatr-request-handler-plan.md) | [models](patterns/mediatr-request-handler-modellen.md) | [verify](patterns/mediatr-request-handler-verify.md) | TAAK-023 |
| `mediatr.void-request-handler` | [plan](patterns/mediatr-void-request-handler-plan.md) | [models](patterns/mediatr-void-request-handler-modellen.md) | [verify](patterns/mediatr-void-request-handler-verify.md) | TAAK-025 |
| `mediatr.notification-handler` | [plan](patterns/mediatr-notification-handler-plan.md) | [models](patterns/mediatr-notification-handler-modellen.md) | [verify](patterns/mediatr-notification-handler-verify.md) | TAAK-026 |
| `mediatr.pipeline-behavior` | [plan](patterns/mediatr-pipeline-behavior-plan.md) | [models](patterns/mediatr-pipeline-behavior-modellen.md) | [verify](patterns/mediatr-pipeline-behavior-verify.md) | TAAK-027 |
| `mediatr.add-mediatr` | [plan](patterns/mediatr-add-mediatr-plan.md) | [models](patterns/mediatr-add-mediatr-modellen.md) | [verify](patterns/mediatr-add-mediatr-verify.md) | TAAK-028 |
| `mediatr.send-as-task` | [plan](patterns/mediatr-send-as-task-plan.md) | [models](patterns/mediatr-send-as-task-modellen.md) | [verify](patterns/mediatr-send-as-task-verify.md) | TAAK-029 |
| `mediatr.unsupported` | [plan](patterns/mediatr-unsupported-plan.md) | [models](patterns/mediatr-unsupported-modellen.md) | [verify](patterns/mediatr-unsupported-verify.md) | TAAK-037 |
| `mediatr.request-processor` | [plan](patterns/mediatr-request-processor-plan.md) | [models](patterns/mediatr-request-processor-modellen.md) | [verify](patterns/mediatr-request-processor-verify.md) | TAAK-061 |
| `mediatr.exception-handler` | [plan](patterns/mediatr-exception-handler-plan.md) | [models](patterns/mediatr-exception-handler-modellen.md) | [verify](patterns/mediatr-exception-handler-verify.md) | TAAK-061 |
| `mediatr.stream-pipeline-behavior` | [plan](patterns/mediatr-stream-pipeline-behavior-plan.md) | [models](patterns/mediatr-stream-pipeline-behavior-modellen.md) | [verify](patterns/mediatr-stream-pipeline-behavior-verify.md) | TAAK-062 |
| `mediatr.behavior-di-registration` | [plan](patterns/mediatr-behavior-di-registration-plan.md) | [models](patterns/mediatr-behavior-di-registration-modellen.md) | [verify](patterns/mediatr-behavior-di-registration-verify.md) | TAAK-145 |
| `mediatr.request-handler-delegate` | [plan](patterns/mediatr-request-handler-delegate-plan.md) | [models](patterns/mediatr-request-handler-delegate-modellen.md) | [verify](patterns/mediatr-request-handler-delegate-verify.md) | TAAK-180 |
| `mediatr.request-processor-caller` | [plan](patterns/mediatr-request-processor-caller-plan.md) | [models](patterns/mediatr-request-processor-caller-modellen.md) | [verify](patterns/mediatr-request-processor-caller-verify.md) | TAAK-190 |
| `mediatr.generic-notification` | [plan](patterns/mediatr-generic-notification-plan.md) | [models](patterns/mediatr-generic-notification-modellen.md) | [verify](patterns/mediatr-generic-notification-verify.md) | TAAK-192 |
| `mediatr.autofac-mediator-module` | [plan](patterns/mediatr-autofac-mediator-module-plan.md) | [models](patterns/mediatr-autofac-mediator-module-modellen.md) | [verify](patterns/mediatr-autofac-mediator-module-verify.md) | TAAK-196 |
| `mediatr.mediator-implementation` | [plan](patterns/mediatr-mediator-implementation-plan.md) | [models](patterns/mediatr-mediator-implementation-modellen.md) | [verify](patterns/mediatr-mediator-implementation-verify.md) | TAAK-197 |
| `mediatr.generic-decorator` | [plan](patterns/mediatr-generic-decorator-plan.md) | [models](patterns/mediatr-generic-decorator-modellen.md) | [verify](patterns/mediatr-generic-decorator-verify.md) | TAAK-193 |
| `mediatr.generic-request` | [plan](patterns/mediatr-generic-request-plan.md) | [models](patterns/mediatr-generic-request-modellen.md) | [verify](patterns/mediatr-generic-request-verify.md) | TAAK-193 |
| `mediatr.vendored-package` (no pattern class) | [plan](patterns/mediatr-vendored-package-plan.md) | [models](patterns/mediatr-vendored-package-modellen.md) | [verify](patterns/mediatr-vendored-package-verify.md) | TAAK-194 |
| `mediatr.internal-handler-access` | [plan](patterns/mediatr-internal-handler-access-plan.md) | [models](patterns/mediatr-internal-handler-access-modellen.md) | [verify](patterns/mediatr-internal-handler-access-verify.md) | TAAK-195 |
| `mediatr.qualified-swapped-type` | [plan](patterns/mediatr-qualified-swapped-type-plan.md) | [models](patterns/mediatr-qualified-swapped-type-modellen.md) | [verify](patterns/mediatr-qualified-swapped-type-verify.md) | TAAK-288 |
| `fa.be-same-as` | [plan](patterns/fa-be-same-as-plan.md) | [models](patterns/fa-be-same-as-modellen.md) | [verify](patterns/fa-be-same-as-verify.md) | TAAK-093 |
| `fa.not-throw-async` | [plan](patterns/fa-not-throw-async-plan.md) | [models](patterns/fa-not-throw-async-modellen.md) | [verify](patterns/fa-not-throw-async-verify.md) | TAAK-094 |
| `fa.match` | [plan](patterns/fa-match-plan.md) | [models](patterns/fa-match-modellen.md) | [verify](patterns/fa-match-verify.md) | TAAK-099 |
| `fa.direct-mapping` | [plan](patterns/fa-direct-mapping-plan.md) | [models](patterns/fa-direct-mapping-modellen.md) | [verify](patterns/fa-direct-mapping-verify.md) | TAAK-101 |
| `automapper.create-map` | [plan](patterns/automapper-create-map-plan.md) | [models](patterns/automapper-create-map-modellen.md) | [verify](patterns/automapper-create-map-verify.md) | TAAK-146 |
| `automapper.for-member-ignore` | [plan](patterns/automapper-for-member-ignore-plan.md) | [models](patterns/automapper-for-member-ignore-modellen.md) | [verify](patterns/automapper-for-member-ignore-verify.md) | TAAK-155 |
| `automapper.for-member-map-from` | [plan](patterns/automapper-for-member-map-from-plan.md) | [models](patterns/automapper-for-member-map-from-modellen.md) | [verify](patterns/automapper-for-member-map-from-verify.md) | TAAK-156 |
| `automapper.map-call` | [plan](patterns/automapper-map-call-plan.md) | [models](patterns/automapper-map-call-modellen.md) | [verify](patterns/automapper-map-call-verify.md) | TAAK-157 |
| `automapper.add-automapper` | [plan](patterns/automapper-add-automapper-plan.md) | [models](patterns/automapper-add-automapper-modellen.md) | [verify](patterns/automapper-add-automapper-verify.md) | TAAK-159 |
| `automapper.map-into` | [plan](patterns/automapper-map-into-plan.md) | [models](patterns/automapper-map-into-modellen.md) | [verify](patterns/automapper-map-into-verify.md) | TAAK-158 |
| `automapper.nested-maps` | [plan](patterns/automapper-nested-maps-plan.md) | [models](patterns/automapper-nested-maps-modellen.md) | [verify](patterns/automapper-nested-maps-verify.md) | TAAK-245 |
| `automapper.member-list` | [plan](patterns/automapper-member-list-plan.md) | [models](patterns/automapper-member-list-modellen.md) | [verify](patterns/automapper-member-list-verify.md) | TAAK-246 |
| `automapper.conversions` | [plan](patterns/automapper-conversions-plan.md) | [models](patterns/automapper-conversions-modellen.md) | [verify](patterns/automapper-conversions-verify.md) | TAAK-247 |
| `automapper.reverse-map` | [plan](patterns/automapper-reverse-map-plan.md) | [models](patterns/automapper-reverse-map-modellen.md) | [verify](patterns/automapper-reverse-map-verify.md) | TAAK-249 |
| `automapper.configuration-test` | [plan](patterns/automapper-configuration-test-plan.md) | [models](patterns/automapper-configuration-test-modellen.md) | [verify](patterns/automapper-configuration-test-verify.md) | TAAK-250 |
| `automapper.profile-shapes` | [plan](patterns/automapper-profile-shapes-plan.md) | [models](patterns/automapper-profile-shapes-modellen.md) | [verify](patterns/automapper-profile-shapes-verify.md) | TAAK-248 |
| `automapper.constructor` | [plan](patterns/automapper-constructor-plan.md) | [models](patterns/automapper-constructor-modellen.md) | [verify](patterns/automapper-constructor-verify.md) | TAAK-251 |
| `automapper.value-resolver` | [plan](patterns/automapper-value-resolver-plan.md) | [models](patterns/automapper-value-resolver-modellen.md) | [verify](patterns/automapper-value-resolver-verify.md) | TAAK-254 |
| `automapper.project-to` | [plan](patterns/automapper-project-to-plan.md) | [models](patterns/automapper-project-to-modellen.md) | [verify](patterns/automapper-project-to-verify.md) | TAAK-252 |
| `automapper.convert-using` | [plan](patterns/automapper-convert-using-plan.md) | [models](patterns/automapper-convert-using-modellen.md) | [verify](patterns/automapper-convert-using-verify.md) | TAAK-253 |
| `automapper.member-options` | [plan](patterns/automapper-member-options-plan.md) | [models](patterns/automapper-member-options-modellen.md) | [verify](patterns/automapper-member-options-verify.md) | TAAK-255 |
| `automapper.flattening` | [plan](patterns/automapper-flattening-plan.md) | [models](patterns/automapper-flattening-modellen.md) | [verify](patterns/automapper-flattening-verify.md) | TAAK-256 |
| `automapper.unsupported` | [plan](patterns/automapper-unsupported-plan.md) | [models](patterns/automapper-unsupported-modellen.md) | [verify](patterns/automapper-unsupported-verify.md) | TAAK-257 |
| `automapper.kept-map` | [plan](patterns/automapper-kept-map-plan.md) | [models](patterns/automapper-kept-map-modellen.md) | [verify](patterns/automapper-kept-map-verify.md) | TAAK-272 |
| `automapper.unused-imapper` | [plan](patterns/automapper-unused-imapper-plan.md) | [models](patterns/automapper-unused-imapper-modellen.md) | [verify](patterns/automapper-unused-imapper-verify.md) | TAAK-289 |
| `automapper.mock-set-up` | [plan](patterns/automapper-mock-set-up-plan.md) | [models](patterns/automapper-mock-set-up-modellen.md) | [verify](patterns/automapper-mock-set-up-verify.md) | TAAK-311 |
| `automapper.generic-map` | [plan](patterns/automapper-generic-map-plan.md) | [models](patterns/automapper-generic-map-modellen.md) | [verify](patterns/automapper-generic-map-verify.md) | TAAK-324 |
| `automapper.generic-profile` | [plan](patterns/automapper-generic-profile-plan.md) | [models](patterns/automapper-generic-profile-modellen.md) | [verify](patterns/automapper-generic-profile-verify.md) | TAAK-334 |
| `moq.create` | [plan](patterns/moq-create-plan.md) | [models](patterns/moq-create-modellen.md) | [verify](patterns/moq-create-verify.md) | TAAK-307 |
| `moq.setup-returns` | [plan](patterns/moq-setup-returns-plan.md) | [models](patterns/moq-setup-returns-modellen.md) | [verify](patterns/moq-setup-returns-verify.md) | TAAK-307 |
| `moq.verify` | [plan](patterns/moq-verify-plan.md) | [models](patterns/moq-verify-modellen.md) | [verify](patterns/moq-verify-verify.md) | TAAK-307 |
| `moq.verify-all` | [plan](patterns/moq-verify-all-plan.md) | [models](patterns/moq-verify-all-modellen.md) | [verify](patterns/moq-verify-all-verify.md) | TAAK-307 |
| `moq.throws` | [plan](patterns/moq-throws-plan.md) | [models](patterns/moq-throws-modellen.md) | [verify](patterns/moq-throws-verify.md) | TAAK-307 |
| `moq.properties` | [plan](patterns/moq-properties-plan.md) | [models](patterns/moq-properties-modellen.md) | [verify](patterns/moq-properties-verify.md) | TAAK-307 |
| `moq.returns-lambda` | [plan](patterns/moq-returns-lambda-plan.md) | [models](patterns/moq-returns-lambda-modellen.md) | [verify](patterns/moq-returns-lambda-verify.md) | TAAK-307 |
| `moq.callback` | [plan](patterns/moq-callback-plan.md) | [models](patterns/moq-callback-modellen.md) | [verify](patterns/moq-callback-verify.md) | TAAK-307 |
| `moq.class-mocks` | [plan](patterns/moq-class-mocks-plan.md) | [models](patterns/moq-class-mocks-modellen.md) | [verify](patterns/moq-class-mocks-verify.md) | TAAK-307 |
| `moq.setup-sequence` | [plan](patterns/moq-setup-sequence-plan.md) | [models](patterns/moq-setup-sequence-modellen.md) | [verify](patterns/moq-setup-sequence-verify.md) | TAAK-307 |
| `moq.logger-verify` | [plan](patterns/moq-logger-verify-plan.md) | [models](patterns/moq-logger-verify-modellen.md) | [verify](patterns/moq-logger-verify-verify.md) | TAAK-307 |
| `moq.unsupported` | [plan](patterns/moq-unsupported-plan.md) | [models](patterns/moq-unsupported-modellen.md) | [verify](patterns/moq-unsupported-verify.md) | TAAK-307 |
| `polly.wait-and-retry-async` | [plan](patterns/polly-wait-and-retry-async-plan.md) | [models](patterns/polly-wait-and-retry-async-modellen.md) | [verify](patterns/polly-wait-and-retry-async-verify.md) | TAAK-308 |
| `polly.retry-variants` | [plan](patterns/polly-retry-variants-plan.md) | [models](patterns/polly-retry-variants-modellen.md) | [verify](patterns/polly-retry-variants-verify.md) | TAAK-308 |
| `polly.add-policy-handler` | [plan](patterns/polly-add-policy-handler-plan.md) | [models](patterns/polly-add-policy-handler-modellen.md) | [verify](patterns/polly-add-policy-handler-verify.md) | TAAK-308 |
| `polly.wrap` | [plan](patterns/polly-wrap-plan.md) | [models](patterns/polly-wrap-modellen.md) | [verify](patterns/polly-wrap-verify.md) | TAAK-308 |
| `polly.sync-retry` | [plan](patterns/polly-sync-retry-plan.md) | [models](patterns/polly-sync-retry-modellen.md) | [verify](patterns/polly-sync-retry-verify.md) | TAAK-308 |
| `polly.timeout` | [plan](patterns/polly-timeout-plan.md) | [models](patterns/polly-timeout-modellen.md) | [verify](patterns/polly-timeout-verify.md) | TAAK-308 |
| `polly.no-op` | [plan](patterns/polly-no-op-plan.md) | [models](patterns/polly-no-op-modellen.md) | [verify](patterns/polly-no-op-verify.md) | TAAK-308 |
| `polly.circuit-breaker` | [plan](patterns/polly-circuit-breaker-plan.md) | [models](patterns/polly-circuit-breaker-modellen.md) | [verify](patterns/polly-circuit-breaker-verify.md) | TAAK-308 |
| `polly.unsupported` | [plan](patterns/polly-unsupported-plan.md) | [models](patterns/polly-unsupported-modellen.md) | [verify](patterns/polly-unsupported-verify.md) | TAAK-308 |
| `epplus.license-context` | [plan](patterns/epplus-license-context-plan.md) | [models](patterns/epplus-license-context-modellen.md) | [verify](patterns/epplus-license-context-verify.md) | TAAK-309 |
| `epplus.package` | [plan](patterns/epplus-package-plan.md) | [models](patterns/epplus-package-modellen.md) | [verify](patterns/epplus-package-verify.md) | TAAK-309 |
| `epplus.styles` | [plan](patterns/epplus-styles-plan.md) | [models](patterns/epplus-styles-modellen.md) | [verify](patterns/epplus-styles-verify.md) | TAAK-309 |
| `epplus.layout` | [plan](patterns/epplus-layout-plan.md) | [models](patterns/epplus-layout-modellen.md) | [verify](patterns/epplus-layout-verify.md) | TAAK-309 |
| `epplus.dimension` | [plan](patterns/epplus-dimension-plan.md) | [models](patterns/epplus-dimension-modellen.md) | [verify](patterns/epplus-dimension-verify.md) | TAAK-309 |
| `epplus.load-from` | [plan](patterns/epplus-load-from-plan.md) | [models](patterns/epplus-load-from-modellen.md) | [verify](patterns/epplus-load-from-verify.md) | TAAK-309 |
| `epplus.flow-types` | [plan](patterns/epplus-flow-types-plan.md) | [models](patterns/epplus-flow-types-modellen.md) | [verify](patterns/epplus-flow-types-verify.md) | TAAK-327 |
| `epplus.address` | [plan](patterns/epplus-address-plan.md) | [models](patterns/epplus-address-modellen.md) | [verify](patterns/epplus-address-verify.md) | TAAK-327 |
| `epplus.unsupported` | [plan](patterns/epplus-unsupported-plan.md) | [models](patterns/epplus-unsupported-modellen.md) | [verify](patterns/epplus-unsupported-verify.md) | TAAK-309 |
| `masstransit.registration` | [plan](patterns/masstransit-registration-plan.md) | [models](patterns/masstransit-registration-modellen.md) | [verify](patterns/masstransit-registration-verify.md) | TAAK-310 |
| `masstransit.transport` | [plan](patterns/masstransit-transport-plan.md) | [models](patterns/masstransit-transport-modellen.md) | [verify](patterns/masstransit-transport-verify.md) | TAAK-310 |
| `masstransit.outbox` | [plan](patterns/masstransit-outbox-plan.md) | [models](patterns/masstransit-outbox-modellen.md) | [verify](patterns/masstransit-outbox-verify.md) | TAAK-310 |
| `masstransit.retry` | [plan](patterns/masstransit-retry-plan.md) | [models](patterns/masstransit-retry-modellen.md) | [verify](patterns/masstransit-retry-verify.md) | TAAK-310 |
| `masstransit.test-harness` | [plan](patterns/masstransit-test-harness-plan.md) | [models](patterns/masstransit-test-harness-modellen.md) | [verify](patterns/masstransit-test-harness-verify.md) | TAAK-310 |
| `masstransit.consumer` | [plan](patterns/masstransit-consumer-plan.md) | [models](patterns/masstransit-consumer-modellen.md) | [verify](patterns/masstransit-consumer-verify.md) | TAAK-310 |
| `masstransit.consumer-definition` | [plan](patterns/masstransit-consumer-definition-plan.md) | [models](patterns/masstransit-consumer-definition-modellen.md) | [verify](patterns/masstransit-consumer-definition-verify.md) | TAAK-310 |
| `masstransit.saga` | [plan](patterns/masstransit-saga-plan.md) | [models](patterns/masstransit-saga-modellen.md) | [verify](patterns/masstransit-saga-verify.md) | TAAK-310 |
| `masstransit.request-client` | [plan](patterns/masstransit-request-client-plan.md) | [models](patterns/masstransit-request-client-modellen.md) | [verify](patterns/masstransit-request-client-verify.md) | TAAK-310 |
| `masstransit.publish-send` | [plan](patterns/masstransit-publish-send-plan.md) | [models](patterns/masstransit-publish-send-modellen.md) | [verify](patterns/masstransit-publish-send-verify.md) | TAAK-310 |

## See also

- `claude/docs/INDEX.md` — index for agents
- `claude/docs/patroon-loop.md` — how patterns are made and documented
- `claude/docs/code-standaarden.md` — code style
