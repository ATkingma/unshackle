using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int[] x)
    {
        x.ShouldNotBeEmpty();
        x.ShouldAllBe(i => i > 0);
    }
}
