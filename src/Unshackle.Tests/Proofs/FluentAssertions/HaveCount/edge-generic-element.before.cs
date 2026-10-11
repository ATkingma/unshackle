using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public void Test<T>(List<T> items)
    {
        items.Should().HaveCount(1);
    }
}
