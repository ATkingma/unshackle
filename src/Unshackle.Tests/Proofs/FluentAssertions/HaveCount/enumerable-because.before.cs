using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class EnumerableBecause
{
    public void Test(IEnumerable<int> x)
    {
        x.Should().HaveCount(2, "two items expected");
    }
}
