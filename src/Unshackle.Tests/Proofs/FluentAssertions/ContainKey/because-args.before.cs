using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Dictionary<string, int> x)
    {
        x.Should().ContainKey("a", "because {0}", 1);
    }
}
