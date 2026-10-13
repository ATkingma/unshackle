using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.Should().ContainKey("a", "it is there").WhoseValue.Should().Be(1, "it is one");
    }
}
