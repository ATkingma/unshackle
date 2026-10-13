using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public struct Pair
{
    public int A;
    public string? B { get; set; }
}

public readonly record struct Stamp(int X, DateTime At);

public class Case
{
    public void Test(List<Pair> pairs, Pair[] expectedPairs, IReadOnlyList<Stamp> stamps, Stamp[] expectedStamps)
    {
        pairs.ShouldBe(expectedPairs, ignoreOrder: true);
        stamps.ShouldBe(expectedStamps, ignoreOrder: true);
    }
}
