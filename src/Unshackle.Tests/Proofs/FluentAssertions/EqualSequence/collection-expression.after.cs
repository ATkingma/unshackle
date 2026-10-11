using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string> s, List<int> x)
    {
        s.ShouldBe(new string[] { "a", "b" });
        x.ShouldBe(new int[] { 1 });
    }
}
