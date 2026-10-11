using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Enumerable
{
    public void Test(IEnumerable<int> items, int[] array)
    {
        items.Should().BeEmpty();
        array.Should().NotBeEmpty();
    }
}
