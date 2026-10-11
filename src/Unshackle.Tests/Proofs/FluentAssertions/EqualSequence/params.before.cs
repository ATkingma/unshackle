using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<long> x, List<string> s)
    {
        x.Should().Equal(1, 2, 3);
        s.Should().Equal("a", "b");
    }
}
