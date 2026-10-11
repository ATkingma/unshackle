using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class ListCase
{
    public void Test(List<int> x)
    {
        x.Should().Contain(2);
    }
}
