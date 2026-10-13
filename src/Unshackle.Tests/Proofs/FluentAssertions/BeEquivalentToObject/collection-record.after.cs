using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public sealed record Item(int Id, string? Name);

public class Case
{
    public void Test(List<Item> actual, Item[] expected, IEnumerable<Item?> maybe)
    {
        actual.ShouldBe(expected, ignoreOrder: true);
        actual.ShouldBe(new[] { new Item(2, null), new Item(1, "a"), new Item(2, null) }, ignoreOrder: true, "any order");
        maybe.ShouldBe(new Item?[] { null, new Item(1, "a") }, ignoreOrder: true);
    }
}
