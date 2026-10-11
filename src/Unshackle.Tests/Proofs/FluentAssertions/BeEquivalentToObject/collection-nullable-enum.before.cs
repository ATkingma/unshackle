using FluentAssertions;
using System;
using System.Collections.Generic;

namespace Proofs;

public class Case
{
    public void Test(List<DayOfWeek?> a, DayOfWeek?[] b)
    {
        a.Should().BeEquivalentTo(b);
    }
}
