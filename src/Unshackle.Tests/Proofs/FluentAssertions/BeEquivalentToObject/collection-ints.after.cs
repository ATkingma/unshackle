using Shouldly;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(List<int> a, int[] b)
    {
        a.ShouldBe(b, ignoreOrder: true);
    }
}
