using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, List<string> y)
    {
        x.Should().Equal(y, (a, b) => a.ToString() == b);
    }
}
