using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        if (x.Count > 0) x.Should().AllSatisfy(i => Check(i));
    }

    private static void Check(int i)
    {
    }
}
