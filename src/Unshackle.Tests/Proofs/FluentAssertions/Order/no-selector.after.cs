using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.ShouldBeInOrder(SortDirection.Ascending);
        x.ShouldBeInOrder(SortDirection.Descending);
    }
}
