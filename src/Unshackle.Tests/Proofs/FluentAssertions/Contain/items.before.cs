using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class ItemsCase
{
    public void Test(List<int> x)
    {
        x.Should().Contain(new[] { 1, 2 });
    }
}
