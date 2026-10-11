using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test<T>(List<T> items, T item)
    {
        items.Should().Contain(item);
    }
}
