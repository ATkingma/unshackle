using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class NotBeEmptyList
{
    public void Test(List<int> list)
    {
        list.Should().NotBeEmpty();
    }
}
