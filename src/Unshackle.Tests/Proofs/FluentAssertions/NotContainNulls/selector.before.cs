using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Item { public string? Name { get; set; } }

public class Case
{
    public void Test(List<Item> x)
    {
        x.Should().NotContainNulls(i => i.Name);
    }
}
