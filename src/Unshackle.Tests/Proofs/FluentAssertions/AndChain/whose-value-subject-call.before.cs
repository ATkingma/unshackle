using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(int unused)
    {
        Get().Should().ContainKey("a").WhoseValue.Should().Be(1);
    }

    private static Dictionary<string, int> Get() => new() { ["a"] = 1 };
}
