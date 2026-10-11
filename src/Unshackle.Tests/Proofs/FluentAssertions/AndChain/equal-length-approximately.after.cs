using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, string s, double d)
    {
        x.ShouldNotBeEmpty();
        x.ShouldBe(new int[] { 1, 2 });
        s.ShouldNotBeNull();
        s.Length.ShouldBe(3);
        d.ShouldBeGreaterThan(0);
        d.ShouldBe(1.5, 0.1);
    }
}
