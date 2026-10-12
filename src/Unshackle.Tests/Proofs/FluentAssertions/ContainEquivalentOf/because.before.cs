using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.Should().ContainEquivalentOf(2, "it was added");
        x.Should().NotContainEquivalentOf(5, "it was {0}", "removed");
    }
}
