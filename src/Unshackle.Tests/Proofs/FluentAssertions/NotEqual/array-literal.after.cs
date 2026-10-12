using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldNotBeNull().ShouldNotBe<System.Collections.Generic.IEnumerable<int>>(new[] { 1, 2 });
    }
}
