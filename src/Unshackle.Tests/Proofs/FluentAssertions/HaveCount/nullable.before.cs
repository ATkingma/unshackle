#nullable enable
using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class NullableHaveCount
{
    public void Test(List<int>? x)
    {
        x.Should().HaveCount(2);
    }
}
