using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<string> x, List<string> y)
    {
        x.ShouldNotBeNull().ShouldNotBe<System.Collections.Generic.IEnumerable<string>>(y);
    }
}
