using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class List
{
    public void Test(List<int> x)
    {
        x.Should().HaveCount(2);
    }
}
