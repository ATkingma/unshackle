using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Because
{
    public void Test(List<int> list, string text)
    {
        list.Should().BeEmpty("fresh order");
        text.Should().NotBeEmpty("must be filled");
    }
}
