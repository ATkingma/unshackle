using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class List
{
    public void Test(List<int> x)
    {
        x.Count.ShouldBe(2);
    }
}
