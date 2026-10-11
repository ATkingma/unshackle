using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class BecauseVariableCase
{
    public void Test(List<int> x, string why)
    {
        x.Should().Contain(2, why, "added");
    }
}
