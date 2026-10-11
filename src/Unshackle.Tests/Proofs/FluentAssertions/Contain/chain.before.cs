using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class ChainCase
{
    public void Test(List<int> x)
    {
        x.Should().Contain(2).And.HaveCount(3);
    }
}
