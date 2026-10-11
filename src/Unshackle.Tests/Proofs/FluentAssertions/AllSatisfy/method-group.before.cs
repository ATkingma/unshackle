using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.Should().AllSatisfy(Check);
    }

    private static void Check(int i)
    {
    }
}
