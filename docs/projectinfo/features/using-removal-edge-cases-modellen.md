# Models: removing an unused old using (TAAK-281, TAAK-283)

Facts from `src/Unshackle.Core/UsingDirectives.cs` (`UpdateAsync`) and `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs`.

## Figure 1: decision tree (one old using of the file)

```d2
direction: down
q1: "namespace in fix.RemovedNamespaces?"
q2: "a name of it (or of a twin-less type) still bound,\nor inactive code may use it?"
q3: "leading trivia has directive / disabled text?"
keep: "stays; all removed usings stay; package stays"
swap: "swapped for the added using (first ones)"
k1: "RemoveNodes(KeepLeadingTrivia)"
k2: "RemoveNodes(KeepNoTrivia)"
q1 -> keep: "no (not ours)"
q1 -> q2: yes
q2 -> keep: yes
q2 -> swap: "no, and an added namespace is missing"
q2 -> q3: "no, nothing to add"
q3 -> k1: yes
q3 -> k2: no
```

## Figure 2: before and after of the conditional

```d2
direction: right
before: "using System;\n#if X\nusing System.Text; (disabled)\n#else\nusing FluentAssertions;\n#endif\nnamespace P;"
bad: "KeepNoTrivia: #else and the disabled\nusing vanish, #endif stays (CS1028)"
good: "KeepLeadingTrivia: using System;\n#if X\nusing System.Text;\n#else\n#endif\nnamespace P;"
before -> bad
before -> good
```

| Element              | Role                                                                                                   |
| -------------------- | ------------------------------------------------------------------------------------------------------ |
| `RemovedNamespaces`  | all FluentAssertions namespaces; a using of one that nothing binds to is old and unused               |
| `UsedRemovedNamespaces` | one used removed namespace puts them all in `used`: nothing is swapped or removed then            |
| `conditional` loop   | the two removal passes in `UpdateAsync`                                                                |

## See also

- [plan](using-removal-edge-cases-plan.md)
