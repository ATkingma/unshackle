using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldHaveSingleItem("one");
        x.ShouldContain(i => i > 2, 1, "one");
    }
}
