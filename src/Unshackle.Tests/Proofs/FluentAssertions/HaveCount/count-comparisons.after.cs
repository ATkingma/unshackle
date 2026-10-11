using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proofs;

public class Case
{
    public void Test(List<int> a, int[] b, IEnumerable<int> c)
    {
        a.Count.ShouldBeGreaterThan(1);
        b.Length.ShouldBeGreaterThanOrEqualTo(2, "at least two");
        c.Count().ShouldBeLessThan(3);
        a.Count.ShouldBeLessThanOrEqualTo(4);
    }
}
