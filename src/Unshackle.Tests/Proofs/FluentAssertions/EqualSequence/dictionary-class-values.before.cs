using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Item
{
    public int Id { get; set; }
}

public class Case
{
    public void Test(Dictionary<string, Item> x, Dictionary<string, Item> y)
    {
        x.Should().Equal(y);
    }
}
