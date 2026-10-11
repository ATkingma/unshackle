using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<int, int> x, Dictionary<int, int> y)
    {
        x.Should().Equal(y);
    }
}
