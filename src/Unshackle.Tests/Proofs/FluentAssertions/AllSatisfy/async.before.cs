using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.Should().AllSatisfy(async i => await System.Threading.Tasks.Task.CompletedTask);
    }

    private static void Check(int i)
    {
    }
}
