using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<string> x)
    {
        x.ShouldBeInOrder(SortDirection.Descending, System.StringComparer.Ordinal);
    }
}
