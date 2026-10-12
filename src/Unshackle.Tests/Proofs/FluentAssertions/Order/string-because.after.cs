using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(string[] x)
    {
        x.ShouldBeInOrder(SortDirection.Ascending, System.StringComparer.Ordinal, "names");
    }
}
