#nullable enable
using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class NullableContainKey
{
    public void Test(Dictionary<string, int>? x)
    {
        x.Should().ContainKey("a");
    }
}
