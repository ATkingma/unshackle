using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Proofs;

public class Case
{
    public void Test(List<int> a, int[] b, IEnumerable<int> c)
    {
        a.Should().HaveCountGreaterThan(1);
        b.Should().HaveCountGreaterThanOrEqualTo(2, "at least two");
        c.Should().HaveCountLessThan(3);
        a.Should().HaveCountLessThanOrEqualTo(4);
    }
}
