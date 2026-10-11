using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, string s, double d)
    {
        x.Should().NotBeEmpty().And.Equal(1, 2);
        s.Should().NotBeNull().And.HaveLength(3);
        d.Should().BeGreaterThan(0).And.BeApproximately(1.5, 0.1);
    }
}
