using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class AndChainCase
{
    public void Test(List<int> x)
    {
        x.Should().NotBeEmpty().And.Contain(2);
    }
}
