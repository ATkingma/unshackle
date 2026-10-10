using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class ListBecause
{
    public void Test(List<int> x)
    {
        x.Count.ShouldBe(2, "two items expected");
    }
}
