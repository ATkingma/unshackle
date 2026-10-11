using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test<T>(List<T> items)
    {
        items.Count.ShouldBe(1);
    }
}
