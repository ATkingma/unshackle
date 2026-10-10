using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class Chain
{
    public void Test(List<int> x)
    {
        x.Should().HaveCount(2).And.Contain(1);
    }
}
