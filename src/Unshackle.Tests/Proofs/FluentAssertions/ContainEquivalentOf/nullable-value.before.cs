using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<DateTimeOffset> x, List<int?> y, DateTimeOffset at)
    {
        x.Should().ContainEquivalentOf(at);
        y.Should().ContainEquivalentOf((int?)null);
    }
}
