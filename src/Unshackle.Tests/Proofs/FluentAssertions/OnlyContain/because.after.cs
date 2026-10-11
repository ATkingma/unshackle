using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldNotBeEmpty("all are positive");
        x.ShouldAllBe(i => i > 0, "all are positive");
    }
}
