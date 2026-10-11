#nullable enable
using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class NullableOnlyContain
{
    public void Test(List<int>? x)
    {
        x.ShouldNotBeEmpty();
        x.ShouldAllBe(i => i > 0);
    }
}
