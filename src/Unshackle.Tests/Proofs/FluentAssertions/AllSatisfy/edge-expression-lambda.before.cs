using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<int> items)
    {
        items.Should().AllSatisfy(x => Check(x));
    }

    private static void Check(int value)
    {
    }
}
