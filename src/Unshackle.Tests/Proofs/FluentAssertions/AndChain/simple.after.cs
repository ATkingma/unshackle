using System;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(List<int> items, string name, object obj)
    {
        items.ShouldNotBeEmpty();
        items.Count.ShouldBe(2);
        name.ShouldNotBeNull();
        name.ShouldBe("Ann");
    }
}
