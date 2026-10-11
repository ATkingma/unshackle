using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.Should().HaveCount(2).And.OnlyContain(i => i > 0);
    }
}
