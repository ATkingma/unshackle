using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(List<int> x)
    {
        var result = x.Should().Contain(2);
    }
}
