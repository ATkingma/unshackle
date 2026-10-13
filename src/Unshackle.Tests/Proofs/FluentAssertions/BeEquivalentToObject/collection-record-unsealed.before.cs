using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public record Item(int Id);

public class Case
{
    public void Test(List<Item> actual, Item[] expected)
    {
        actual.Should().BeEquivalentTo(expected);
    }
}
