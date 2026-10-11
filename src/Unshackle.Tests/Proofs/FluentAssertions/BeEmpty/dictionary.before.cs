using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Dictionary
{
    public void Test(Dictionary<string, int> map)
    {
        map.Should().BeEmpty();
        map.Should().NotBeEmpty();
    }
}
