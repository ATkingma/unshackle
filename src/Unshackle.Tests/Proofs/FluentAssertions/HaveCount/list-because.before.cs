using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class ListBecause
{
    public void Test(List<int> x)
    {
        x.Should().HaveCount(2, "two items expected");
    }
}
