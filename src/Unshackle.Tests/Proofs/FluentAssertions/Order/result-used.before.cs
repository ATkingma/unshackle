using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        var c = x.Should().BeInAscendingOrder().And;
    }
}
