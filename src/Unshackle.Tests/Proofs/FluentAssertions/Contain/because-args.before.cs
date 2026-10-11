using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(List<int> x)
    {
        x.Should().Contain(2, "it was {0}", "added");
    }
}
