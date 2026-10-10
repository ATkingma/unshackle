using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class BecauseArgs
{
    public void Test(List<int> x, int y)
    {
        x.Should().HaveCount(2, "x {0}", y);
    }
}
