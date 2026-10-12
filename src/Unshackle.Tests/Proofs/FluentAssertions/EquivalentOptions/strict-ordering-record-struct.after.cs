using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public record struct Pair(int Id, string Name);

public class Case
{
    public void Test(List<Pair> a, Pair[] b)
    {
        a.ShouldBe(b);
    }
}
