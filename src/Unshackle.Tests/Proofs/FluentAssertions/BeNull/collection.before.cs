using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Collection
{
    public void Test(List<string>? list, int[]? array)
    {
        list.Should().NotBeNull();
        array.Should().BeNull();
    }
}
