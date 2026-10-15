# Draft: r/dotnet launch post

Status: draft for the user to post (TAAK-019 step 5). Post only after the repo is public and `Unshackle 0.1.0` is on NuGet.

---

**Title:** I built a Roslyn tool that migrates FluentAssertions → Shouldly, MediatR → Mediator and AutoMapper → plain mappings

FluentAssertions, MediatR and AutoMapper all moved to commercial licenses. Migrating by hand is tedious, and a regex gets the
edge cases wrong (`.And` chains, `because` args, `Task` vs `ValueTask`, null handling in mappings). So I wrote **Unshackle**, a
`dotnet tool` that rewrites the code with Roslyn and tells you exactly what it could not do safely.

```
dotnet tool install --global Unshackle
unshackle fix MyApp.sln --fix fluentassertions --dry-run
```

How I tested it: 18 open-source solutions the tool had never seen, each built and tested before and after the fix.

- FluentAssertions → Shouldly: 6/6 fully migrated, all tests green
- MediatR → Mediator: 3/6 fully migrated; the other 3 stopped with a clear "fix these by hand first" list
- AutoMapper → hand-written `ToB()` extensions: 1/6 fully migrated; it only rewrites mappings that are provably equivalent
- 0 test regressions

The rule throughout: never guess. If a rewrite is not provably the same, it becomes a *Manual* finding with the reason
(`--report report.md`). It also updates package references and global usings, and refuses to run on a dirty git tree.

Repo (MIT): https://github.com/ATkingma/unshackle — feedback, wrong rewrites and missing patterns are very welcome as issues.

---

## See also

- `claude/tasks/TAAK-019-lancering.md`
- `README.md`
