using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, int y)
    {
        y.Should().Be(1);
        x.Should().AllSatisfy(i => Check(i), "they are valid");
        x.Should().NotBeEmpty().And.AllSatisfy(i => Check(i));
        y.Should().Be(2);
    }

    private static void Check(int i)
    {
    }
}
