using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        x.Should().OnlyContain(i => i > 0).And.HaveCount(2);
    }
}
