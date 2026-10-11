using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Func<List<int>> f)
    {
        var subject = f();
        subject.ShouldNotBeEmpty();
        foreach (var i in subject)
        {
            Check(i);
        }
    }

    private static void Check(int i)
    {
    }
}
