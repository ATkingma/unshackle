using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x, System.Linq.Expressions.Expression<Func<int, bool>> p)
    {
        var one = x.Should().ContainSingle(p).Which;
    }
}
