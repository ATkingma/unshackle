using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<DateTimeOffset> x, List<int?> y, DateTimeOffset at)
    {
        x.ShouldContain(at);
        y.ShouldContain((int?)null);
    }
}
