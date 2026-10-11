using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<int> items)
    {
        items.ShouldNotBeEmpty();
        foreach (var x in items)
        {
            Check(x);
        }
    }

    private static void Check(int value)
    {
    }
}
