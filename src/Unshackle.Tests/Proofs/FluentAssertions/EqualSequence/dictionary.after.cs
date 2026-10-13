using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<int, int> x, Dictionary<int, int> y)
    {
        x.ShouldBe(y, ignoreOrder: true);
    }
}
