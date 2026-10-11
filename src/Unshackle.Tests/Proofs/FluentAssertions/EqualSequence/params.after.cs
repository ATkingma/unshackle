using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<long> x, List<string> s)
    {
        x.ShouldBe(new long[] { 1, 2, 3 });
        s.ShouldBe(new string[] { "a", "b" });
    }
}
