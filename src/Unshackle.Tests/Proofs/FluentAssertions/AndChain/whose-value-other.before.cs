using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, string> x)
    {
        x.Should().ContainKey("a").WhoseValue.Should().StartWith("b");
    }
}
