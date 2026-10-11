#nullable enable
using FluentAssertions;
using System.Collections.Generic;

namespace Proofs;

public class NullableOnlyContain
{
    public void Test(List<int>? x)
    {
        x.Should().OnlyContain(i => i > 0);
    }
}
