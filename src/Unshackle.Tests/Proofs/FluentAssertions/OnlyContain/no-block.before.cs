using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(List<int> x)
    {
        if (x != null)
            x.Should().OnlyContain(i => i > 0);
    }
}
