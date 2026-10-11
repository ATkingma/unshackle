using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class BeEmptyList
{
    public void Test(List<int> list)
    {
        list.Should().BeEmpty();
    }
}
