# Verification: specflow-namespace

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/specflow-namespace-plan.md`](specflow-namespace-plan.md) |
| **Status**    | Verified                                                          |
| **Generated** | 2026-10-27 by KingmaDoc                                           |

> Verified by an AI agent (TAAK-340 first slice) against the code on branch `taak-340`.

## Requirements

Code: `src/Unshackle.Core/Fixes/SpecFlow/Patterns/NamespacePattern.cs`, registration in `SpecFlowFix.cs`.
Proofs: `src/Unshackle.Tests/Proofs/SpecFlow/Namespace/` (`ProofTests`; an after proof runs the whole fix, `ProofFix.AfterRunsWholeFix`, and compiles against Reqnroll 3.3.4 only).

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | steps, assist, bodi, qualified |
| REQ-2 | Met | app-config |
| REQ-3 | Met | SpecFlowFixTests.Patterns_IdsUseSpecFlowPrefix_NamespacePatternLast |
| REQ-4 | Met | Namespace/assist, RenamedType/output-helper; `NamespacePattern.Droppable` binds every use with the semantic model (council 21 S12) |
| REQ-5 | Met | SpecFlowNamesTests (Autofac plugin types against both real assemblies), RenamedType/autofac-bindings |

## Deviations

None.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed |
| Tests | `dotnet test` | all green |

## Council round 18 (S7, S8)

A swapped namespace using is dropped (the result has no nodes) when nothing in the file binds through it afterwards: every name, `foreach`
enumerator, `await` awaiter and collection-initializer `Add` is resolved, and a symbol of the old namespace counts only when its new
namespace (after `RenamedTypes`) is the swapped one. Global, alias and static usings, a using with comments or directives in front, and
files with a query expression or a deconstruction keep it, and so does the only using of its block (dropping it would leave a stray blank line). Proofs: `Namespace/assist` (`using Reqnroll.Assist;` dropped) and
`RenamedType/output-helper` (the only using, kept). S8: namespace `SpecFlow.Autofac` maps to `Reqnroll.Autofac`.

## Council round 21 (S11, S12)

S12: accepted as built; the exclusions are listed literally under REQ-4 of the plan. Checked in code: `Droppable` drops only when no
symbol bound by the semantic model lives in the swapped namespace; the syntactic checks only add exclusions, they never cause a drop.
S11: SpecFlow.Autofac 3.9.74 and Reqnroll.Autofac 3.3.4 are in `Directory.Packages.props` (licence check: both BSD-3-Clause, their
build files only copy the plugin dll, no licence or sponsor check). The type proof found that the plugin sub-namespace is renamed too
(`SpecFlow.Autofac.SpecFlowPlugin` → `Reqnroll.Autofac.ReqnrollPlugin`), now in `SpecFlowNames.NewNamespace`.

## See also

- [specflow-namespace-plan.md](specflow-namespace-plan.md)
- [specflow-namespace-modellen.md](specflow-namespace-modellen.md)
