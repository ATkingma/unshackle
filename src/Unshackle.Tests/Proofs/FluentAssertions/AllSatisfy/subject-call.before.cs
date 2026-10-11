using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Func<List<int>> f)
    {
        f().Should().AllSatisfy(i => Check(i));
    }

    private static void Check(int i)
    {
    }
}
