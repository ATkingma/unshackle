using System;
using System.Linq;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(IEnumerable<int> x, int[] y)
    {
        x.Count().ShouldBe(2);
        y.ShouldContain(3);
        x.ShouldNotBeEmpty();
        foreach (var i in x)
        {
            i.ShouldBe(1);
        }
    }
}
