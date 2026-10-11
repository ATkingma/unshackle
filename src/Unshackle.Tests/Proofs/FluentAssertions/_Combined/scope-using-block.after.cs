using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(int x, List<int> items, Action act)
    {
        x.ShouldSatisfyAllConditions(
            () => { x.ShouldBe(1); },
            () => { items.ShouldNotBeEmpty(); items.Count.ShouldBe(2); },
            () => { Should.Throw<InvalidOperationException>(act); });
    }
}
