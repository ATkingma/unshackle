#nullable enable
using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class NullableBeEmpty
{
    public void Test(List<int>? x)
    {
        x.Should().BeEmpty();
    }
}
