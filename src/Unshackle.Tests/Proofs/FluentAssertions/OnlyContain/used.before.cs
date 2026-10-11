using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        var r = x.Should().OnlyContain(i => i > 0);
    }
}
