using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public sealed record Item(int Id, string? Name);

public class Case
{
    public void Test(List<Item> actual, Item[] expected, IEnumerable<Item?> maybe)
    {
        actual.Should().BeEquivalentTo(expected);
        actual.Should().BeEquivalentTo(new[] { new Item(2, null), new Item(1, "a"), new Item(2, null) }, "any order");
        maybe.Should().BeEquivalentTo(new Item?[] { null, new Item(1, "a") });
    }
}
