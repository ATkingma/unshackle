using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldNotBeEmpty();
        foreach (int i in x)
        {
            Check(i);
        }
    }

    private static void Check(int i)
    {
    }
}
