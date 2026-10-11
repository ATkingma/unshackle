using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<int> x, int[] y)
    {
        x.Should().HaveCount(2);
        y.Should().Contain(3);
        x.Should().AllSatisfy(i => i.Should().Be(1));
    }
}
