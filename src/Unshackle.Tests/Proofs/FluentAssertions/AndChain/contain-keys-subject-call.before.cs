using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(int unused)
    {
        Get().Should().ContainKeys("a", "b");
    }

    private static Dictionary<string, int> Get() => new() { ["a"] = 1, ["b"] = 2 };
}
