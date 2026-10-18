# Models: AutoMapper mocked IMapper (TAAK-279)

Facts from `src/Unshackle.Core/Fixes/AutoMapper/Patterns/MapOwners.cs`, `CreateMapPattern.cs` and `KeptMapPattern.cs`.
Builds on [automapper-partial-migration-modellen.md](automapper-partial-migration-modellen.md).

## Figure 1: class diagram

```d2
MapOwners: {shape: class; "-_pairs / _destinations / _all": "kept on AutoMapper"; "-_shared": "pair → reason (CreateMap stays, ToB() generated)"; "-_underTest": "type keys"; "+Reason(source, destination)": "string?"; "+Shared(source, destination)": "string? (null when kept)"; "+KeptCallAsync()": "kept, or call in a class under test"; "~ClassesUnderTest(setUp, model)": "List<INamedTypeSymbol>"; "-CloseUnderTest(sources)": "types the class hands IMapper to"}
CreateMapPattern: {shape: class; "-Rewrite(…, owners, …)": "shared statement: methods + statement stays"}
KeptMapPattern: {shape: class; "-Kept()": "Reason ?? Shared"}
CallPatterns: "MapCallPattern / MapIntoPattern / ProjectToPattern"
CreateMapPattern -> MapOwners: Reason, Shared
KeptMapPattern -> MapOwners: Reason, Shared
CallPatterns -> MapOwners: KeptCallAsync
```

| Element              | Role                                                                                                                                        |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `_shared`            | pairs a class under test keeps calling through `IMapper`; `Reason` is null for them, so `NestedMaps` and `GeneratedAsync` see their `ToB()` |
| `_underTest`         | the classes a test constructs with the mock, their base types, and the types they hand an `IMapper` to                                      |
| `ClassesUnderTest`   | mock variable of the set-up (plus variables assigned from it) → `new C(…)` in the same file with it as an `IMapper` argument                |
| `Shared` vs `Reason` | kept wins: a shared pair that another reason keeps has no `ToB()`                                                                           |

## Figure 2: sequence (plan with a stub)

```d2
shape: sequence_diagram
Owners: MapOwners
Test: test file
Calls: call patterns
Owners -> Test: "ClassesUnderTest(set-up): mock variable, aliases, new C(mock)"
Owners -> Owners: "_underTest += C and its bases; CloseUnderTest (IMapper arguments)"
Owners -> Owners: "fixpoint loop: set-up with classes → ShareCall(pair); without → KeepCall (as before)"
Owners -> Owners: "Close: a shared node shares its reverse and every reached map that is not kept"
Calls -> Owners: "KeptCallAsync(call)"
Owners -> Calls: "kept pair → Manual; shared pair in a class under test → Manual; else null (rewrite to ToB())"
```

## Figure 3: decision tree (one Map call of pair A → B)

```d2
direction: down
q1: "mock set-up itself?"
q2: "A → B kept (Reason)?"
q3: "A → B shared AND call inside a class under test?"
m1: "Manual: set-up stays"
m2: "Manual: call keeps IMapper (kept reason)"
m3: "Manual: call keeps IMapper (class under test)"
f: "rewrite to a.ToB()"
q1 -> m1: yes
q1 -> q2: no
q2 -> m2: yes
q2 -> q3: no
q3 -> m3: yes
q3 -> f: no
```

## Figure 4: decision tree (a stubbed pair)

```d2
direction: down
s1: "test file constructs a class with the mock (or a variable assigned from it)?"
s2: "types known at compile time?"
s3: "ToB() can be generated and no other reason keeps it?"
k: "kept on AutoMapper everywhere (as before)"
sh: "shared: CreateMap stays, ToB() generated, other classes migrate"
s1 -> k: no
s1 -> s2: yes
s2 -> k: no
s2 -> s3: yes
s3 -> k: no
s3 -> sh: yes
```

## See also

- [plan](automapper-mocked-imapper-plan.md), [verify](automapper-mocked-imapper-verify.md)
