using FluentAssertions.Execution;
using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, int[] y)
    {
        using var scope = new AssertionScope();
        x.Should().Equal(y);
    }
}
