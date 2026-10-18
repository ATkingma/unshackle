---
kingmadoc: 1
feature: "using-removal-edge-cases"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/UsingDirectives.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.Core/Fixer.cs
    - src/Unshackle.Tests/Core/FixerTests.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/_Combined/
---

# Feature: removing an unused old using without breaking the build (TAAK-281, TAAK-283)

## Problem

Blind round 5 found two ways a file's old using made the build fail after `fix`:

- **TAAK-281** (serilogmem): `#if X using A; #else using FluentAssertions; #endif`. The old using is unused, so it is removed with
  `RemoveNodes(KeepNoTrivia)`. Roslyn then also drops the `#else` (and the disabled text of the other branch, which is leading trivia of
  that using) and leaves the `#endif`: CS1028 on every target framework.
- **TAAK-283** (opentws): `using FluentAssertions.Common;` is a sub-namespace. `IFix.RemovedNamespaces` only listed four FluentAssertions
  namespaces, so this using was never treated as old: it stayed, the package reference went, CS0246.

## Requirements

| Id    | Requirement                                                                                                                                  |
| ----- | -------------------------------------------------------------------------------------------------------------------------------------------- |
| REQ-1 | An unused removed using that carries `#if`/`#else`/`#endif` directives or disabled text keeps its leading trivia: the pairs stay whole.     |
| REQ-2 | Every FluentAssertions namespace of the 6/7 packages is a removed namespace: an unused one is removed or swapped, with the others.          |
| REQ-3 | A sub-namespace that is still used (Manual code stays) keeps all old usings and the package, by the existing "one stays, all stay" rule.    |
| REQ-4 | A rewrite never drops `#if`/`#else`/`#region` directives inside the node it replaces: Manual instead (found in serilogmem).                 |
| REQ-5 | A file that a project without the old and without the new library also compiles (linked file) is not migrated: Manual (found in serilogmem). |

## Design

1. `UsingDirectives.UpdateAsync` removes the unused usings in two passes: usings whose leading trivia holds a directive or disabled text with
   `SyntaxRemoveOptions.KeepLeadingTrivia` (the trivia moves to the next token), the rest with `KeepNoTrivia` as before.
2. `FluentAssertionsFix.RemovedNamespaces` lists Collections, Common, Data, Equivalency, Events, Formatting, Numeric, Specialized, Streams,
   Types and Xml as well. `UsedRemovedNamespaces`, `ProjectPackages.KeepsRemovedUsing` and the project-file cascade work from that list, so a
   used sub-namespace needs no new rule.
3. `Fixer.Replace`: when the node has more directives/disabled text inside (not in front of its first token) than the new nodes,
   the finding is Manual and the code stays (`InnerDirectives`). Without this a chain with `#if` between its calls lost the `#if` and kept `#endif`.
4. `Fixer.UnreachableFilesAsync`: a file compiled by one project that references the removed package and by another that references neither
   the removed nor an added package is skipped (an `encoding`-style Manual row); that other project gets no Shouldly from the project-file cascade (CS0246).

Not done: stripping the using together with its branch (the other branch's code may be real code in another configuration), and reporting
Manual for any using in a conditional (the file has nothing left to migrate, the removal is safe once the directives stay paired).

## See also

- [models](using-removal-edge-cases-modellen.md), [verify](using-removal-edge-cases-verify.md), `claude/tasks/archief/TAAK-281-using-in-conditional.md`, `claude/tasks/archief/TAAK-283-using-sub-namespace.md`
