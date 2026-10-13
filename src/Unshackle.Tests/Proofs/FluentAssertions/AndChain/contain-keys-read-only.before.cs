using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(IReadOnlyDictionary<string, int> x)
    {
        x.Should().ContainKeys("a", "b");
    }
}
