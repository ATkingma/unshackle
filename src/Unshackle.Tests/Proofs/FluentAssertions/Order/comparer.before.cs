using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.Should().BeInAscendingOrder(Comparer<int>.Default);
    }
}
