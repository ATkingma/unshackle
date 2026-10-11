using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldNotBeEmpty();
        foreach (var i in x)
        {
            Check(i);
            Check(i + 1);
        }
    }

    private static void Check(int i)
    {
    }
}
